using HtmlAgilityPack;
using StrikeData.Data;
using StrikeData.Models;
using System.Globalization;
using Newtonsoft.Json.Linq;
using StrikeData.Services.Normalization;
using StrikeData.Services.StaticMaps;
using StrikeData.Data.Extensions;

namespace StrikeData.Services.TeamData.Importers
{
    /// <summary>
    /// Imports team-level PITCHING statistics from two sources:
    /// 1) MLB Stats API (season totals for core pitching metrics)
    /// 2) TeamRankings (per-game "Current Season" values for selected metrics)
    /// Data is written into StatType/TeamStat under the "Pitching" category.
    /// </summary>
    public class PitchingImporter
    {
        // EF Core DbContext for persistence and a dedicated HttpClient for network I/O.
        private readonly AppDbContext _context;
        private readonly HttpClient _httpClient;

        public PitchingImporter(AppDbContext context)
        {
            _context = context;
            _httpClient = new HttpClient();
        }

        /// <summary>
        /// Entry point for loading all pitching stats:
        /// 1) Pull MLB season totals
        /// 2) Pull TeamRankings metrics (per-game, current season)
        /// </summary>
        public async Task ImportAllStatsAsyncP()
        {
            // Step 1: MLB provides authoritative season totals per team
            await ImportTeamPitchingStatsMLB();

            // Step 2: Enrich with TeamRankings "Current Season" per-game metrics
            foreach (var stat in TeamRankingsMaps.Pitching)
            {
                await ImportPitchingTeamStatTR(stat.Key, stat.Value);
            }
        }

        /// <summary>
        /// Imports MLB team pitching stats and stores them as season totals.
        /// Each JSON record corresponds to one team; values are mapped to our StatType abbreviations.
        /// </summary>
        private async Task ImportTeamPitchingStatsMLB()
        {
            var statsArray = await FetchTeamPitchingStatsMLB();
            var pitchingCategoryId = await _context.EnsureCategoryIdAsync("Pitching");


            // Maps MLB JSON fields to our canonical abbreviations (StatType.Name).
            var statMappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "era", "ERA" },
                { "shutouts", "SHO" },
                { "completeGames", "CG" },
                { "saves", "SV" },
                { "saveOpportunities", "SVO" },
                { "inningsPitched", "IP" },
                { "hits", "H" },
                { "runs", "R" },
                { "homeRuns", "HR" },
                { "wins", "W" },
                { "strikeOuts", "SO" },
                { "whip", "WHIP" },
                { "avg", "AVG" },
                { "battersFaced", "TBF" },
                { "numberOfPitches", "NP" },
                { "pitchesPerInning", "P/IP" },
                { "gamesFinished", "GF" },
                { "holds", "HLD" },
                { "intentionalWalks", "IBB" },
                { "wildPitches", "WP" },
                { "strikeoutWalkRatio", "K/BB" }
            };

            foreach (var statToken in statsArray)
            {
                // Each element must be a structured object with team fields.
                if (statToken is not JObject teamStat)
                    continue;

                // Team name is the join key across sources; normalize to our canonical names.
                var teamNameRaw = teamStat["teamName"]?.ToString();
                if (string.IsNullOrWhiteSpace(teamNameRaw))
                {
                    Console.WriteLine("⚠️ Team name not found or empty.");
                    continue;
                }
                var teamName = TeamNameNormalizer.Normalize(teamNameRaw);

                // Ensure Team exists in the database.
                var team = _context.Teams.FirstOrDefault(t => t.Name == teamName);
                if (team == null)
                {
                    team = new Team { Name = teamName };
                    _context.Teams.Add(team);
                    await _context.SaveChangesAsync();
                }

                // Upsert a TeamStat row for each mapped metric.
                foreach (var mapping in statMappings)
                {
                    var apiField = mapping.Key;
                    var shortName = mapping.Value;

                    // Skip if the expected field is missing in MLB payload.
                    if (!teamStat.TryGetValue(apiField, out var token))
                        continue;

                    var rawValue = token?.ToString();
                    if (string.IsNullOrWhiteSpace(rawValue))
                        continue;

                    // Parse using invariant culture to avoid locale-dependent issues.
                    if (!float.TryParse(rawValue, NumberStyles.Float, CultureInfo.InvariantCulture, out float statValue))
                    {
                        Console.WriteLine($"⚠️ Invalid value for {shortName} in {teamName}: '{rawValue}'");
                        continue;
                    }

                    // Ensure StatType exists under the Pitching category.
                    var statType = _context.StatTypes.FirstOrDefault(s => s.Name == shortName && s.StatCategoryId == pitchingCategoryId);
                    if (statType == null)
                    {
                        statType = new StatType { Name = shortName, StatCategoryId = pitchingCategoryId };
                        _context.StatTypes.Add(statType);
                        await _context.SaveChangesAsync();
                    }

                    // One TeamStat per (Team, StatType).
                    var stat = _context.TeamStats.FirstOrDefault(ts => ts.TeamId == team.Id && ts.StatTypeId == statType.Id);
                    if (stat == null)
                    {
                        stat = new TeamStat { TeamId = team.Id, StatTypeId = statType.Id };
                        _context.TeamStats.Add(stat);
                    }

                    // MLB season aggregate is stored in Total.
                    stat.Total = statValue;
                }

                // Persist team-by-team so a partial import still leaves data saved if a later team fails.
                await _context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Returns the Id of the "Pitching" category, creating it if necessary.
        /// </summary>
        private async Task<int> GetPitchingCategoryIdAsync()
        {
            var category = _context.StatCategories.FirstOrDefault(c => c.Name == "Pitching");
            if (category == null)
            {
                category = new StatCategory { Name = "Pitching" };
                _context.StatCategories.Add(category);
                await _context.SaveChangesAsync();
            }
            return category.Id;
        }

        /// <summary>
        /// Calls the MLB endpoint used by their public site to retrieve team pitching stats
        /// for the 2025 regular season and returns the "stats" array.
        /// </summary>
        private async Task<JArray> FetchTeamPitchingStatsMLB()
        {
            var url = "https://bdfed.stitch.mlbinfra.com/bdfed/stats/team?&env=prod&gameType=R&group=pitching&order=desc&sortStat=strikeouts&stats=season&season=2025&limit=30&offset=0";
            var response = await _httpClient.GetStringAsync(url);
            var json = JObject.Parse(response);
            var stats = (JArray)json["stats"];
            if (stats == null || !stats.Any())
            {
                Console.WriteLine("⚠️ No pitching stats found from MLB.");
                return new JArray();
            }
            return stats;
        }

        /// <summary>
        /// Imports one pitching metric from TeamRankings.
        /// Only the "2025" column is read and stored as TeamStat.CurrentSeason.
        /// </summary>
        private async Task ImportPitchingTeamStatTR(string statTypeName, string url)
        {
            var response = await _httpClient.GetStringAsync(url);
            var doc = new HtmlDocument();
            doc.LoadHtml(response);

            // TeamRankings renders a single data table with yearly columns (e.g., "2025").
            var table = doc.DocumentNode.SelectSingleNode("//table[contains(@class,'datatable')]");
            if (table == null) return;

            // Locate the "2025" column in the header.
            var headerRow = table.SelectSingleNode(".//thead/tr");
            var headerCells = headerRow.SelectNodes(".//th|.//td");
            int currentSeasonIndex = -1;
            for (int i = 0; i < headerCells.Count; i++)
            {
                var colName = headerCells[i].InnerText.Trim();
                if (colName == "2025")
                {
                    currentSeasonIndex = i;
                    break;
                }
            }
            if (currentSeasonIndex == -1)
            {
                Console.WriteLine("⚠️ Column 2025 not found in TeamRankings table.");
                return;
            }

            // Ensure the StatType exists under the Pitching category.
            var categoryId = await _context.EnsureCategoryIdAsync("Pitching");
            var statType = _context.StatTypes.FirstOrDefault(s => s.Name == statTypeName && s.StatCategoryId == categoryId);
            if (statType == null)
            {
                statType = new StatType { Name = statTypeName, StatCategoryId = categoryId };
                _context.StatTypes.Add(statType);
                await _context.SaveChangesAsync();
            }

            // Iterate team rows and write the "2025" value into CurrentSeason.
            var rows = table.SelectNodes(".//tr").Skip(1);
            foreach (var row in rows)
            {
                var cells = row.SelectNodes("./td").ToList();
                if (cells.Count <= currentSeasonIndex) continue;

                // Team name is in the second column (index 1).
                var rawTeamName = cells[1].InnerText.Trim();
                if (string.IsNullOrWhiteSpace(rawTeamName)) continue;
                var teamName = TeamNameNormalizer.Normalize(rawTeamName);

                // Ensure Team exists.
                var team = _context.Teams.FirstOrDefault(t => t.Name == teamName);
                if (team == null)
                {
                    team = new Team { Name = teamName };
                    _context.Teams.Add(team);
                    await _context.SaveChangesAsync();
                }

                // Parse "2025" cell as float (invariant culture).
                var cellText = cells[currentSeasonIndex].InnerText.Trim();
                if (string.IsNullOrWhiteSpace(cellText)) continue;

                if (!float.TryParse(cellText, NumberStyles.Float, CultureInfo.InvariantCulture, out float currentSeason))
                {
                    Console.WriteLine($"⚠️ Invalid value for {statTypeName} in {teamName}: '{cellText}'");
                    continue;
                }

                // Upsert TeamStat and store CurrentSeason (per-game value).
                var stat = _context.TeamStats.FirstOrDefault(ts => ts.TeamId == team.Id && ts.StatTypeId == statType.Id);
                if (stat == null)
                {
                    stat = new TeamStat { TeamId = team.Id, StatTypeId = statType.Id };
                    _context.TeamStats.Add(stat);
                }

                stat.CurrentSeason = currentSeason;
            }

            await _context.SaveChangesAsync();
        }
    }
}
