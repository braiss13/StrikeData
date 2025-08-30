using Microsoft.EntityFrameworkCore;
using StrikeData.Data;
using StrikeData.Models;
using StrikeData.Services.StaticMaps;

namespace StrikeData.Services.PlayerData
{
    /// <summary>
    /// Imports per-player fielding metrics from a web scraper and stores them
    /// under the "Fielding" StatCategory in PlayerStats.
    /// </summary>
    public class PlayerFieldingImporter
    {
        private readonly AppDbContext _context;
        private readonly PlayerFieldingScraper _scraper;

        public PlayerFieldingImporter(AppDbContext context, PlayerFieldingScraper scraper)
        {
            _context = context;
            _scraper = scraper;
        }

        /// <summary>
        /// Imports fielding stats for all teams in the given season.
        /// Ensures StatCategory and PlayerStatTypes exist, matches rows to Players,
        /// and upserts PlayerStats.
        /// </summary>
        public async Task ImportAllTeamsPlayerFieldingAsync(int season)
        {
            // Ensure the "Fielding" category exists
            var fieldingCat = await _context.StatCategories.FirstOrDefaultAsync(c => c.Name == "Fielding")
                              ?? new StatCategory { Name = "Fielding" };
            if (fieldingCat.Id == 0)
            {
                _context.StatCategories.Add(fieldingCat);
                await _context.SaveChangesAsync();
            }

            // Ensure the set of PlayerStatTypes used for fielding
            var existingTypes = await _context.PlayerStatTypes
                .Where(pst => pst.StatCategoryId == fieldingCat.Id)
                .ToListAsync();

            var byName = existingTypes.ToDictionary(t => t.Name, t => t, StringComparer.OrdinalIgnoreCase);
            int created = 0;

            // PlayerMaps.FieldingMetrics defines the expected column abbreviations
            foreach (var m in PlayerMaps.FieldingMetrics)
            {
                if (!byName.ContainsKey(m))
                {
                    var t = new PlayerStatType { Name = m, StatCategoryId = fieldingCat.Id };
                    _context.PlayerStatTypes.Add(t);
                    byName[m] = t;
                    created++;
                }
            }
            if (created > 0) await _context.SaveChangesAsync();

            // Preload players (with Team navigation) for name/position matching
            var allPlayers = await _context.Players
                .Include(p => p.Team)
                .AsNoTracking()
                .ToListAsync();

            // Build a per-team dictionary keyed by normalized "Name|POS" variants
            // to improve match rate against scraped rows.
            static string Key(string name, string? pos)
                => $"{Utilities.NormalizePlayerName(name)}|{(pos ?? "").ToUpperInvariant()}";

            var playersByTeam = allPlayers
                .GroupBy(p => p.TeamId)
                .ToDictionary(
                    g => g.Key,
                    g =>
                    {
                        var dict = new Dictionary<string, Player>(StringComparer.OrdinalIgnoreCase);

                        foreach (var p in g)
                        {
                            var posU = (p.Position ?? "").ToUpperInvariant();

                            // Variant 1: "FirstName LastName"
                            var k1 = Key(p.Name, posU);
                            if (!dict.ContainsKey(k1)) dict[k1] = p;

                            // Variant 2: "LastName, FirstName"
                            var parts = p.Name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length >= 2)
                            {
                                var last = parts[^1];
                                var first = string.Join(' ', parts.Take(parts.Length - 1));
                                var k2 = Key($"{last}, {first}", posU);
                                if (!dict.ContainsKey(k2)) dict[k2] = p;
                            }
                        }
                        return dict;
                    });

            // Iterate teams by external code -> official team name mapping
            foreach (var kv in TeamCodeMap.CodeToName)
            {
                var code = kv.Key;
                var teamName = kv.Value;

                // Resolve local Team row by official name
                var team = await _context.Teams.FirstOrDefaultAsync(t => t.Name == teamName);
                if (team == null) continue;

                // Scrape fielding rows for this team and season
                var rows = await _scraper.GetTeamFieldingRowsAsync(code, season);

                // Player matching dictionary for this team
                if (!playersByTeam.TryGetValue(team.Id, out var byPlayerKey))
                {
                    Console.WriteLine($"[FieldingImporter][INFO] There aren't players for TeamId={team.Id}. Skipping.");
                    continue;
                }

                // Prevent duplicates within the same execution before SaveChanges
                var seen = new HashSet<(int playerId, int typeId)>();

                int matchedPlayers = 0, upserts = 0;

                foreach (var row in rows)
                {
                    // Match by normalized Name+POS, as the scraped table is position-specific
                    var k = Key(row.Name, row.Pos);
                    if (!byPlayerKey.TryGetValue(k, out var player))
                        continue; // position mismatch or name not found -> row is ignored

                    matchedPlayers++;

                    // Persist all metrics present in this row
                    foreach (var kvp in row.Values)
                    {
                        if (!byName.TryGetValue(kvp.Key, out var statType)) continue;

                        var logicalKey = (player.Id, statType.Id);
                        if (seen.Contains(logicalKey)) continue; // already processed in this batch
                        seen.Add(logicalKey);

                        var existing = await _context.PlayerStats
                            .FirstOrDefaultAsync(ps =>
                                ps.PlayerId == player.Id &&
                                ps.PlayerStatTypeId == statType.Id);

                        if (existing == null)
                        {
                            _context.PlayerStats.Add(new PlayerStat
                            {
                                PlayerId = player.Id,
                                PlayerStatTypeId = statType.Id,
                                Total = kvp.Value
                            });
                        }
                        else
                        {
                            existing.Total = kvp.Value;
                        }
                        upserts++;
                    }
                }

                // Commit team-level changes to keep batches manageable
                await _context.SaveChangesAsync();
            }
        }
    }
}
