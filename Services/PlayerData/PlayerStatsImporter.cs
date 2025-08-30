using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using StrikeData.Data;
using StrikeData.Models;
using StrikeData.Services.StaticMaps;

namespace StrikeData.Services.PlayerData
{
    /*
        Orchestrates the import of season-level player stats (pitching + hitting)
        from MLB APIs. It makes sure StatCategory and PlayerStatType exist and upserts values into PlayerStats.
    */
    public class PlayerStatsImporter
    {
        private readonly AppDbContext _context;
        private readonly HttpClient _httpClient;

        public PlayerStatsImporter(AppDbContext context)
        {
            _context = context;

            _httpClient = new HttpClient();
        }

        /*
            Entry point used from Index:
            1) Ensures Players exist (40-man rosters)
            2) Imports season stats for both groups (pitching + hitting)
        */
        public async Task ImportAllPlayersAndStatsAsync(int season = 2025)
        {
            // Step 1: Make sure Players exist (roster import creates/updates them).
            var rosterImporter = new PlayerRosterImporter(_context);
            await rosterImporter.ImportAllPlayersAsync();

            // Step 2: Import both stat groups for the provided season.
            await ImportSeasonStatsAsync(season);
        }

        /*
            Imports both groups for the given season:
            - Pitching: group=pitching (filtered to pitchers only)
            - Hitting:  group=hitting (filtered to non-pitchers)
            The API returns a bulk "stats" array that traverse and persist.
        */
        public async Task ImportSeasonStatsAsync(int season = 2025)
        {
            // API endpoints are documented internally by MLB; these URLs fetch aggregates (season scope) for all players.
            // Keep a large "limit" to cover the full population in one pass.

            // ==== PITCHING ====
            var pitchingUrl =
                $"https://bdfed.stitch.mlbinfra.com/bdfed/stats/player?stitch_env=prod&sportId=1&gameType=R&group=pitching&stats=season&season={season}&playerPool=ALL&sortStat=so&order=desc&limit=1000&offset=0";

            await ImportStatsGroupAsync(
                url: pitchingUrl,
                categoryName: "Pitching",
                // MetricName -> JSON field name expected in each item of "stats"
                statFieldMap: PlayerMaps.StatJsonFields.Pitching,
                // Safety filter (avoid mixing hitter rows here)
                mustBePitcher: true
            );

            // ==== HITTING ====
            var hittingUrl =
                $"https://bdfed.stitch.mlbinfra.com/bdfed/stats/player?stitch_env=prod&sportId=1&gameType=R&group=hitting&stats=season&season={season}&playerPool=ALL&sortStat=runs&order=desc&limit=1000&offset=0";

            await ImportStatsGroupAsync(
                url: hittingUrl,
                categoryName: "Hitting",
                statFieldMap: PlayerMaps.StatJsonFields.Hitting,
                // Exclude pitchers from hitting (keeps the view consistent)
                mustBePitcher: false
            );
        }

        /*
            Imports one group (pitching or hitting):
            1) Ensure StatCategory and PlayerStatType records exist
            2) Preload Players and PlayerStats for efficient upserts
            3) Download and parse the bulk JSON payload
            4) For each player item, filter by role (if requested) and upsert totals
        */
        private async Task ImportStatsGroupAsync(
            string url,
            string categoryName,
            IReadOnlyDictionary<string, string> statFieldMap,
            bool mustBePitcher)
        {
            // 1) Ensure StatCategory exists (single row per category) 
            var category = await _context.StatCategories
                .FirstOrDefaultAsync(c => c.Name == categoryName);

            if (category == null)
            {
                category = new StatCategory { Name = categoryName };
                _context.StatCategories.Add(category);
                await _context.SaveChangesAsync();
            }

            // 2) Ensure PlayerStatType for every metric in this category 
            // Preload all types for the category to avoid N+1 queries.
            var existingTypes = await _context.PlayerStatTypes
                .Where(pst => pst.StatCategoryId == category.Id)
                .ToListAsync();

            var typesByName = existingTypes.ToDictionary(
                p => p.Name,
                p => p,
                StringComparer.OrdinalIgnoreCase);

            foreach (var metricName in statFieldMap.Keys)
            {
                if (!typesByName.ContainsKey(metricName))
                {
                    var pst = new PlayerStatType
                    {
                        Name = metricName,
                        StatCategoryId = category.Id
                    };
                    _context.PlayerStatTypes.Add(pst);
                    typesByName[metricName] = pst;
                }
            }
            await _context.SaveChangesAsync();

            // 3) Preload Players by MLB_Player_Id to map API ids to local ids 
            var playersWithId = await _context.Players
                .Where(p => p.MLB_Player_Id != null)
                .ToListAsync();

            var playersByMlbId = playersWithId.ToDictionary(
                p => p.MLB_Player_Id!.Value,
                p => p);

            // 4) Preload existing PlayerStats for this category (for upsert) 
            var typeIds = typesByName.Values.Select(t => t.Id).ToList();

            var existingStats = await _context.PlayerStats
                .Where(ps => typeIds.Contains(ps.PlayerStatTypeId))
                .ToListAsync();

            var statsByKey = existingStats.ToDictionary(
                ps => (ps.PlayerId, ps.PlayerStatTypeId),
                ps => ps);

            // 5) Download and parse JSON payload ("stats" array expected) 
            var json = await _httpClient.GetStringAsync(url);
            using var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("stats", out var statsArr) ||
                statsArr.ValueKind != JsonValueKind.Array)
            {
                // Gracefully skip if the payload is not in the expected format
                return;
            }

            // Iterate each player item
            foreach (var item in statsArr.EnumerateArray())
            {
                // API's playerId can fit in Int32 or come as Int64 (defensive cast below)
                if (!item.TryGetProperty("playerId", out var pidEl)) continue;

                int playerIdFromApi;
                if (!pidEl.TryGetInt32(out playerIdFromApi))
                {
                    if (pidEl.ValueKind == JsonValueKind.Number && pidEl.TryGetInt64(out var pid64))
                        playerIdFromApi = unchecked((int)pid64);
                    else
                        continue; // cannot parse id -> skip row
                }

                // Join with our Players table using stored MLB_Player_Id
                if (!playersByMlbId.TryGetValue(playerIdFromApi, out var player))
                    continue; // player not in our DB (roster import might not have added it yet)

                /*
                    Optional role filter to keep groups consistent:
                    - Pitching group -> only pitchers
                    - Hitting group  -> exclude pitchers
                */
                var isPitcher = string.Equals(player.Position, "P", StringComparison.OrdinalIgnoreCase);
                if (mustBePitcher && !isPitcher) continue;
                if (!mustBePitcher && isPitcher) continue;

                // Map and persist each metric from the JSON item
                foreach (var kv in statFieldMap)
                {
                    var metricName = kv.Key;    // e.g., "AVG", "HR", "SO"
                    var jsonField  = kv.Value;  // expected JSON field name

                    // Some APIs rename specific fields occasionally; handle known aliases
                    if (!item.TryGetProperty(jsonField, out var valEl))
                    {
                        // Example: "IP" may appear as "inningsPitched"
                        if (metricName == "IP" && item.TryGetProperty("inningsPitched", out var alt))
                            valEl = alt;
                        else
                            continue; // field not found -> skip this metric
                    }

                    // Normalize to float? (many stats are numeric; some may come as strings like "12.3%" or "0.950")
                    float? value = null;
                    switch (valEl.ValueKind)
                    {
                        case JsonValueKind.Number:
                            if (valEl.TryGetDouble(out var d))
                                value = (float)d;
                            break;

                        case JsonValueKind.String:
                            var s = valEl.GetString();
                            if (!string.IsNullOrWhiteSpace(s))
                            {
                                // Remove a percentage sign if present and parse invariant
                                s = s.Replace("%", "").Trim();
                                if (float.TryParse(
                                        s,
                                        System.Globalization.NumberStyles.Float,
                                        System.Globalization.CultureInfo.InvariantCulture,
                                        out var f))
                                {
                                    value = f;
                                }
                            }
                            break;

                    }

                    // If the stat type wasn't resolved something is off with the map; guard and continue
                    if (!typesByName.TryGetValue(metricName, out var pstType))
                        continue;

                    // Upsert into PlayerStats using (PlayerId, PlayerStatTypeId) as logical key
                    var key = (player.Id, pstType.Id);
                    if (!statsByKey.TryGetValue(key, out var ps))
                    {
                        ps = new PlayerStat
                        {
                            PlayerId = player.Id,
                            PlayerStatTypeId = pstType.Id,
                            Total = value
                        };
                        _context.PlayerStats.Add(ps);
                        statsByKey[key] = ps; // keep in-memory index in sync
                    }
                    else
                    {
                        ps.Total = value;
                        _context.PlayerStats.Update(ps);
                    }
                }
            }

            // Single commit at the end of the batch. 
            await _context.SaveChangesAsync();
        }
    }
}
