using HtmlAgilityPack;
using StrikeData.Data;
using StrikeData.Models;
using StrikeData.Models.Enums;
using StrikeData.Services.Normalization;
using StrikeData.Services.StaticMaps; 
using StrikeData.Data.Extensions;

namespace StrikeData.Services.TeamData.Importers
{
    /*
        Imports "win trends" team statistics from TeamRankings.
        Persists data into TeamStats under the "WinTrends" category, using the Team perspective.
    */
    public class WinTrendsImporter
    {
        private readonly AppDbContext _context;
        private readonly HttpClient _httpClient;

        public WinTrendsImporter(AppDbContext context)
        {
            _context = context;
            _httpClient = new HttpClient();
        }

        // Entry point: iterates the WinTrends map and imports each stat from TeamRankings.
        public async Task ImportAllStatsAsyncWT()
        {
            foreach (var stat in TeamRankingsMaps.WinTrends)
                await ImportWinTrendsTeamStatsTR(stat.Key, stat.Value);
        }

        /*
            Scrapes one WinTrends table from TeamRankings and upserts:
            - StatType (category = WinTrends)
            - Team entities (normalized name)
            - TeamStats for that StatType and Team (Perspective = Team), setting WinLossRecord and WinPct
        */
        public async Task ImportWinTrendsTeamStatsTR(string statTypeName, string url)
        {
            var response = await _httpClient.GetStringAsync(url);
            var doc = new HtmlDocument();
            doc.LoadHtml(response);

            // Main data table for this trend
            var table = doc.DocumentNode.SelectSingleNode("//table[contains(@class, 'datatable')]");
            if (table == null) return;

            var rows = table.SelectNodes(".//tbody/tr");
            if (rows == null) return;

            // 1) Ensure StatType exists in the "WinTrends" category
            var statType = _context.StatTypes
                .FirstOrDefault(s => s.Name == statTypeName && s.StatCategory.Name == "WinTrends");
            if (statType == null)
            {
                var categoryId = await _context.EnsureCategoryIdAsync("WinTrends");
                statType = new StatType { Name = statTypeName, StatCategoryId = categoryId };
                _context.StatTypes.Add(statType);
                await _context.SaveChangesAsync();
            }

            // 2) Preload Teams into a dictionary keyed by normalized name (case-insensitive)
            var allTeams = _context.Teams.ToList();
            var teamsByNormName = allTeams
                .GroupBy(t => NormalizeName(t.Name))
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            // 3) Preload existing TeamStats for this StatType and Perspective=Team, keyed by TeamId
            var existingStats = _context.TeamStats
                .Where(ts => ts.StatTypeId == statType.Id && ts.Perspective == StatPerspective.Team)
                .ToList()
                .ToDictionary(ts => ts.TeamId, ts => ts);

            // Iterate table rows and upsert team stats
            foreach (var row in rows)
            {
                var cells = row.SelectNodes("td");
                if (cells == null || cells.Count < 4) continue; 

                // Normalize team name to match DB representation
                var rawTeam = cells[0].InnerText;
                var normName = NormalizeName(rawTeam);
                if (string.IsNullOrWhiteSpace(normName)) continue;

                // Ensure Team exists (using normalized name)
                if (!teamsByNormName.TryGetValue(normName, out var team))
                {
                    team = new Team { Name = normName };
                    _context.Teams.Add(team);
                    await _context.SaveChangesAsync();

                    // Cache the newly inserted team to avoid re-insert in this run
                    teamsByNormName[normName] = team;
                }

                // Fetch or create TeamStat for this team/statType/perspective
                if (!existingStats.TryGetValue(team.Id, out var stat))
                {
                    stat = new TeamStat
                    {
                        TeamId = team.Id,
                        StatTypeId = statType.Id,
                        Perspective = StatPerspective.Team
                    };
                    _context.TeamStats.Add(stat);
                    existingStats[team.Id] = stat;
                }

                // W-L record is presented as plain text in the table
                stat.WinLossRecord = Utilities.CleanText(cells[1].InnerText);

                // Win% may include a '%' sign; parse as float (null if parsing fails)
                var winPctText = Utilities.CleanText(cells[2].InnerText).Replace("%", "").Trim();
                stat.WinPct = Utilities.Parse(winPctText);
            }

            await _context.SaveChangesAsync();
        }

        /*
        /// Normalizes a team name using the shared helpers:
        /// - Cleans HTML/whitespace
        /// - Maps aliases to official DB names
        */
        private static string NormalizeName(string? raw)
        {
            var cleaned = Utilities.CleanText(raw ?? "");
            return TeamNameNormalizer.Normalize(cleaned);
        }

    }
}
