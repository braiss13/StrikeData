using HtmlAgilityPack;
using StrikeData.Data;
using StrikeData.Models;
using StrikeData.Services.Normalization;
using StrikeData.Services.StaticMaps;
using StrikeData.Data.Extensions;

namespace StrikeData.Services.TeamData.Importers
{
    /// <summary>
    /// Imports TEAM-level FIELDING statistics from TeamRankings and saves them
    /// into StatType/TeamStat under the "Fielding" category.
    /// TeamRankings provides per-game splits (Current season, Last 3, etc.).
    /// </summary>
    public class FieldingImporter
    {
        // EF Core DbContext for persistence and a dedicated HttpClient for network I/O.
        private readonly AppDbContext _context;
        private readonly HttpClient _httpClient;

        public FieldingImporter(AppDbContext context)
        {
            _context = context;
            _httpClient = new HttpClient();
        }

        /// <summary>
        /// Entry point for Fielding: iterates over TeamRankings fielding endpoints
        /// and imports each metric's per-game values for all teams.
        /// </summary>
        public async Task ImportAllStatsAsyncF()
        {
            foreach (var stat in TeamRankingsMaps.Fielding)
            {
                // stat.Key => abbreviation; stat.Value => TeamRankings URL
                await ImportHittingTeamStatsTR(stat.Key, stat.Value);
            }
        }

        /// <summary>
        /// Scrapes a single TeamRankings fielding page and stores the split values
        /// (CurrentSeason, Last3Games, LastGame, Home, Away, PrevSeason) into TeamStat.
        /// </summary>
        /// <param statTypeName> Abbreviation (e.g., "DP") used as StatType.Name.</param>
        /// <param url> TeamRankings URL for the given fielding metric.</param>
        public async Task ImportHittingTeamStatsTR(string statTypeName, string url)
        {
            // Download the HTML and load it into an HtmlAgilityPack document.
            var response = await _httpClient.GetStringAsync(url);
            var doc = new HtmlDocument();
            doc.LoadHtml(response);

            // TeamRankings renders a single <table class="datatable"> with yearly columns.
            var table = doc.DocumentNode.SelectSingleNode("//table[contains(@class, 'datatable')]");
            // Header keeps the column labels, rows contain one team per row.
            var header = table.SelectSingleNode(".//thead/tr");

            // Skip the header row: we only iterate data rows (one per team).
            var rows = table.SelectNodes(".//tr").Skip(1);

            // Ensure the StatType exists under the "Fielding" category.
            var statType = _context.StatTypes.FirstOrDefault(s => s.Name == statTypeName);
            if (statType == null)
            {
                // Ensure the category exists and get its Id.
                var categoryId = await _context.EnsureCategoryIdAsync("Fielding");

                // Create the StatType bound to "Fielding" to keep the glossary and UI consistent.
                statType = new StatType
                {
                    Name = statTypeName,
                    StatCategoryId = categoryId
                };

                _context.StatTypes.Add(statType);
                await _context.SaveChangesAsync();
            }

            foreach (var row in rows)
            {
                // Each row is a team. The table includes: Rank, Team Name, 2025 (Current), Last3, Last1, Home, Away, Prev
                var cells = row.SelectNodes("td");

                // Guard against unexpected table shapes. We expect at least 8 cells.
                if (cells == null || cells.Count < 8) continue;

                // Team name comes as a display label; normalize to our canonical DB name.
                string rawTeamName = cells[1].InnerText.Trim();
                string teamName = TeamNameNormalizer.Normalize(rawTeamName);

                // Ensure Team exists in the DB so TeamStat can reference it.
                var team = _context.Teams.FirstOrDefault(t => t.Name == teamName);
                if (team == null)
                {
                    team = new Team { Name = teamName };
                    _context.Teams.Add(team);
                    await _context.SaveChangesAsync();
                }

                // One TeamStat row per (Team, StatType). Create it if missing.
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

                // Parse all splits from the row using the shared invariant parser.
                // Indexes map to TeamRankings table columns:
                // 2 => Current Season (year column), 3 => Last 3, 4 => Last 1, 5 => Home, 6 => Away, 7 => Previous season
                stat.CurrentSeason = Utilities.Parse(cells[2].InnerText);
                stat.Last3Games    = Utilities.Parse(cells[3].InnerText);
                stat.LastGame      = Utilities.Parse(cells[4].InnerText);
                stat.Home          = Utilities.Parse(cells[5].InnerText);
                stat.Away          = Utilities.Parse(cells[6].InnerText);
                stat.PrevSeason    = Utilities.Parse(cells[7].InnerText);
            }

            // Persist all changes in a single batch to reduce DB round trips.
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Returns the Id of the "Fielding" StatCategory, creating it if necessary.
        /// </summary>
        private async Task<int> GetFieldingCategoryIdAsync()
        {
            var category = _context.StatCategories.FirstOrDefault(c => c.Name == "Fielding");
            if (category == null)
            {
                category = new StatCategory { Name = "Fielding" };
                _context.StatCategories.Add(category);
                await _context.SaveChangesAsync();
            }
            return category.Id;
        }
    }
}
