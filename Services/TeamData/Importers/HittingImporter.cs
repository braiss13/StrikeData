using HtmlAgilityPack;
using StrikeData.Data;
using StrikeData.Models;
using System.Globalization;
using Newtonsoft.Json.Linq;
using StrikeData.Services.Normalization;
using StrikeData.Data.Extensions;
using StrikeData.Services.StaticMaps;

namespace StrikeData.Services.TeamData.Importers
{
    /// <summary>
    /// Imports team-level HITTING statistics from two sources:
    /// 1) MLB Stats API (season totals + games count)
    /// 2) TeamRankings (per-game splits such as current season, last N, home/away)
    /// Results are persisted into StatType/TeamStat under the "Hitting" category.
    /// </summary>
    public class HittingImporter
    {
        // Data access for persistence and an HttpClient for outbound fetches.
        private readonly AppDbContext _context;
        private readonly HttpClient _httpClient;

        #region MLB

        public HittingImporter(AppDbContext context)
        {
            _context = context;
            _httpClient = new HttpClient();
        }

        /// <summary>
        /// Orchestrates the import of all team-level hitting stats:
        /// 1) Loads MLB season totals (and Games) per team
        /// 2) Loads TeamRankings per-game splits for each hitting metric
        /// </summary>
        public async Task ImportAllStatsAsyncH()
        {
            // Step 1: totals and games from MLB (authoritative for season aggregates)
            await ImportHittingTeamStatsMLB();

            // Step 2: enrich with TeamRankings per-game splits for each metric key
            foreach (var stat in TeamRankingsMaps.Hitting)
            {
                await ImportHittingTeamStatsTR(stat.Key, stat.Value);
            }
        }

        /// <summary>
        /// Imports MLB season totals for hitting (one record per team).
        /// MLB provides team "Games" and aggregated totals for core batting stats.
        /// </summary>
        private async Task ImportHittingTeamStatsMLB()
        {
            // Fetch raw array from MLB (each element corresponds to one team)
            var statsArray = await FetchTeamHittingStatsMLB();

            // Ensure the "Hitting" category exists and retrieve its id
            var hittingCategoryId = await _context.EnsureCategoryIdAsync("Hitting");

            // Maps MLB API field names (long) to our canonical abbreviations
            var statMappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "gamesPlayed", "G" },
                { "atBats", "AB" },
                { "runs", "R" },
                { "hits", "H" },
                { "homeRuns", "HR" },
                { "doubles", "2B" },
                { "triples", "3B" },
                { "rbi", "RBI" },
                { "baseOnBalls", "BB" },
                { "strikeOuts", "SO" },
                { "stolenBases", "SB" },
                { "groundIntoDoublePlay", "GIDP" },
                { "caughtStealing", "CS" },
                { "sacBunts", "SAC" },
                { "sacFlies", "SF" },
                { "totalBases", "TB" },
                { "hitByPitch", "HBP" },
                { "atBatsPerHomeRun", "AB/HR" }
            };

            // Iterate team results and upsert into Teams/TeamStats
            foreach (var statToken in statsArray)
            {
                if (statToken is not JObject teamStat)
                    continue;

                // Team name is the join key across sources; normalize to our official names
                var teamNameRaw = teamStat["teamName"]?.ToString();
                if (string.IsNullOrWhiteSpace(teamNameRaw))
                {
                    Console.WriteLine("⚠️ Team name not found or empty.");
                    continue;
                }

                var teamName = TeamNameNormalizer.Normalize(teamNameRaw);

                // Ensure Team row exists (create on first appearance)
                var team = _context.Teams.FirstOrDefault(t => t.Name == teamName);
                if (team == null)
                {
                    team = new Team { Name = teamName };
                    _context.Teams.Add(team);
                    await _context.SaveChangesAsync();
                }

                // Persist each mapped stat
                foreach (var mapping in statMappings)
                {
                    string apiField = mapping.Key;   // field from MLB JSON
                    string shortName = mapping.Value; // canonical abbreviation

                    if (!teamStat.TryGetValue(apiField, out var token))
                        continue;

                    string rawValue = token?.ToString();
                    if (string.IsNullOrWhiteSpace(rawValue))
                        continue;

                    // Special case: "G" is stored on Team (not TeamStat)
                    if (shortName == "G")
                    {
                        if (int.TryParse(rawValue, out int games))
                        {
                            team.Games = games;
                        }
                        continue;
                    }

                    // Parse numeric stat (float, invariant culture)
                    if (!float.TryParse(rawValue, NumberStyles.Float, CultureInfo.InvariantCulture, out float statValue))
                    {
                        Console.WriteLine($"⚠️ Invalid value for {shortName} in {teamName}: '{rawValue}'");
                        continue;
                    }

                    // Ensure StatType exists within the Hitting category
                    var statType = _context.StatTypes.FirstOrDefault(s => s.Name == shortName);
                    if (statType == null)
                    {
                        // Create in the Hitting category the first time we see it
                        statType = new StatType { Name = shortName };
                        statType = new StatType { Name = shortName, StatCategoryId = hittingCategoryId };
                        _context.StatTypes.Add(statType);
                        await _context.SaveChangesAsync();
                    }

                    // Upsert TeamStat (one row per TeamId/StatTypeId)
                    var stat = _context.TeamStats.FirstOrDefault(ts => ts.TeamId == team.Id && ts.StatTypeId == statType.Id);
                    if (stat == null)
                    {
                        stat = new TeamStat { TeamId = team.Id, StatTypeId = statType.Id };
                        _context.TeamStats.Add(stat);
                    }

                    // MLB provides the season aggregate -> store in Total
                    stat.Total = statValue;
                }
            }

            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Ensures the "Hitting" StatCategory exists and returns its Id.
        /// </summary>
        private async Task<int> GetHittingCategoryIdAsync()
        {
            var category = _context.StatCategories.FirstOrDefault(c => c.Name == "Hitting");
            if (category == null)
            {
                category = new StatCategory { Name = "Hitting" };
                _context.StatCategories.Add(category);
                await _context.SaveChangesAsync();
            }
            return category.Id;
        }

        /// <summary>
        /// Calls MLB Stats API (internal stitch endpoint used by MLB sites) to obtain
        /// the team-level hitting stats for the 2025 regular season.
        /// Returns the "stats" array as a JArray for further processing.
        /// </summary>
        private async Task<JArray> FetchTeamHittingStatsMLB()
        {
            // Network-captured endpoint used by MLB pages to fetch season team stats
            var url = "https://bdfed.stitch.mlbinfra.com/bdfed/stats/team?stitch_env=prod&sportId=1&gameType=R&group=hitting&stats=season&season=2025&limit=30&offset=0";

            var response = await _httpClient.GetStringAsync(url);
            var json = JObject.Parse(response);
            var stats = (JArray)json["stats"];

            if (stats == null || !stats.Any())
            {
                Console.WriteLine("❌ No team stats found from MLB endpoint.");
                return [];
            }

            return stats;
        }

        #endregion

        // ================================================================
        // ========== SECTION: TEAM RANKINGS STATISTICS ===================
        // ================================================================

        /// <summary>
        /// Imports a single TeamRankings hitting metric (identified by <paramref statTypeName>)
        /// from the provided <paramref url>. TeamRankings supplies per-game splits:
        /// CurrentSeason, Last3Games, LastGame, Home, Away, PrevSeason.
        /// </summary>
        public async Task ImportHittingTeamStatsTR(string statTypeName, string url)
        {
            // Download the HTML page and load it into a DOM for parsing
            var response = await _httpClient.GetStringAsync(url);
            var doc = new HtmlDocument();
            doc.LoadHtml(response);

            // TeamRankings renders a single datatable with the metric and splits
            var table = doc.DocumentNode.SelectSingleNode("//table[contains(@class, 'datatable')]");

            // Header row (unused beyond structural checks) and data rows (skip header)
            var header = table.SelectSingleNode(".//thead/tr");
            var rows = table.SelectNodes(".//tr").Skip(1);

            // Ensure StatType exists (under Hitting category)
            var statType = _context.StatTypes.FirstOrDefault(s => s.Name == statTypeName);
            if (statType == null)
            {
                var categoryId = await _context.EnsureCategoryIdAsync("Hitting");

                statType = new StatType
                {
                    Name = statTypeName,
                    StatCategoryId = categoryId
                };

                _context.StatTypes.Add(statType);
                await _context.SaveChangesAsync();
            }

            // Parse each row and upsert TeamStat with per-game split values
            foreach (var row in rows)
            {
                // Extract columns for: Position | Team | Current | Last3 | Last1 | Home | Away | Previous
                var cells = row.SelectNodes("td");

                // Defensive guard: skip non-data rows (e.g., separators or malformed rows)
                if (cells == null || cells.Count < 8) continue;

                // Team name normalization (align TR label with our official DB name)
                string rawTeamName = cells[1].InnerText.Trim();
                string teamName = TeamNameNormalizer.Normalize(rawTeamName);

                // Ensure Team exists
                var team = _context.Teams.FirstOrDefault(t => t.Name == teamName);
                if (team == null)
                {
                    team = new Team { Name = teamName };
                    _context.Teams.Add(team);
                    await _context.SaveChangesAsync();
                }

                // Upsert TeamStat (one row per TeamId/StatTypeId)
                var stat = _context.TeamStats.FirstOrDefault(ts => ts.TeamId == team.Id && ts.StatTypeId == statType.Id);
                if (stat == null)
                {
                    stat = new TeamStat
                    {
                        TeamId = team.Id,
                        StatTypeId = statType.Id
                    };
                    _context.TeamStats.Add(stat);
                }

                // Populate per-game splits parsed from the table cells
                stat.CurrentSeason = Utilities.Parse(cells[2].InnerText);
                stat.Last3Games    = Utilities.Parse(cells[3].InnerText);
                stat.LastGame      = Utilities.Parse(cells[4].InnerText);
                stat.Home          = Utilities.Parse(cells[5].InnerText);
                stat.Away          = Utilities.Parse(cells[6].InnerText);
                stat.PrevSeason    = Utilities.Parse(cells[7].InnerText);

                // For some TR-only metrics, compute a season TOTAL from per-game × games
                CalculateTotal(statTypeName, team, stat);
            }

            // Persist all changes to the database
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Computes TeamStats.Total for TR-only metrics by multiplying
        /// the per-game CurrentSeason value by the number of Games stored on Team.
        /// Only applies to specific metrics that are not delivered as totals by MLB.
        /// </summary>
        private static void CalculateTotal(string statTypeName, Team team, TeamStat stat)
        {
            // Only compute TOTALS for these TR metrics (others already come as MLB totals)
            if (statTypeName == "S" || statTypeName == "SBA" || statTypeName == "LOB" || statTypeName == "TLOB" || statTypeName == "RLSP")
            {
                if (team.Games < 1)
                {
                    Console.WriteLine($"⚠️ Team {team.Name} has no games recorded. Total cannot be computed.");
                }
                else if (stat.CurrentSeason.HasValue)
                {
                    // Use double for rounding API, then cast back to float (our DB type)
                    float currentSeasonValue = stat.CurrentSeason ?? 0;
                    double rawTotal = (double)(team.Games * currentSeasonValue);

                    // Two decimals for season-level totals derived from per-game averages
                    stat.Total = (float)Math.Round(rawTotal, 2);
                }
                else
                {
                    Console.WriteLine($"⚠️ CurrentSeason is null for team {team.Name} in '{statTypeName}'. Total cannot be computed.");
                }
            }
        }
    }
}
