using HtmlAgilityPack;
using StrikeData.Data;
using StrikeData.Models;
using StrikeData.Models.Enums;
using StrikeData.Services.Normalization; 
using StrikeData.Services.StaticMaps;
using StrikeData.Data.Extensions;

namespace StrikeData.Services.TeamData.Importers
{
    /// <summary>
    /// Imports "Curious Facts" team metrics from TeamRankings.
    /// Each metric can be displayed from the team's own perspective or from opponents (prefixed with 'O').
    /// Persists values into TeamStats associated to the "CuriousFacts" category.
    /// </summary>
    public class CuriousFactsImporter
    {
        private readonly AppDbContext _context;
        private readonly HttpClient _httpClient;

        public CuriousFactsImporter(AppDbContext context)
        {
            _context = context;
            _httpClient = new HttpClient();
        }

        /// <summary>
        /// Main entry point. Iterates <see cref="TeamRankingsMaps.CuriousFacts"/> and imports each metric.
        /// </summary>
        public async Task ImportAllStatsAsyncCF()
        {
            foreach (var stat in TeamRankingsMaps.CuriousFacts)
            {
                await ImportCuriousTeamStatsTR(stat.Key, stat.Value);
            }
        }

        /// <summary>
        /// Scrapes a single TeamRankings "curious fact" table and upserts rows into TeamStats.
        /// The <paramref statTypeKey> may start with 'O' to indicate the "Opponent" perspective
        /// </summary>
        /// <param statTypeKey> Abbreviation from the map (e.g., "YRFI", "OYRFI", "F5IR/G").</param>
        /// <param url> TeamRankings URL for that metric.</param>
        public async Task ImportCuriousTeamStatsTR(string statTypeKey, string url)
        {
            // Download HTML and load it into an HtmlAgilityPack document
            var response = await _httpClient.GetStringAsync(url);
            var doc = new HtmlDocument();
            doc.LoadHtml(response);

            // Locate the main datatable (TeamRankings layout)
            var table = doc.DocumentNode.SelectSingleNode("//table[contains(@class, 'datatable')]");
            if (table == null) return;

            // Restrict to <tbody> rows to ignore the header and extraneous rows
            var rows = table.SelectNodes(".//tbody/tr");
            if (rows == null) return;

            // Perspective and base abbreviation:
            // - If the key starts with 'O', treat as Opponent perspective and strip the prefix for the StatType name.
            var perspective = statTypeKey.StartsWith("O", StringComparison.OrdinalIgnoreCase)
                ? StatPerspective.Opponent
                : StatPerspective.Team;

            var baseKey = perspective == StatPerspective.Opponent
                ? statTypeKey.Substring(1)
                : statTypeKey;

            // Ensure StatType is present under "CuriousFacts" and use the base name (without the optional 'O')
            var statType = _context.StatTypes.FirstOrDefault(s => s.Name == baseKey);
            if (statType == null)
            {
                // Centralized "get or create" for StatCategory; returns a stable Id
                var categoryId = await _context.EnsureCategoryIdAsync("CuriousFacts"); 

                statType = new StatType
                {
                    Name = baseKey,
                    StatCategoryId = categoryId
                };

                _context.StatTypes.Add(statType);
                await _context.SaveChangesAsync();
            }

            // Iterate table rows and upsert a TeamStat per team/perspective
            foreach (var row in rows)
            {
                var cells = row.SelectNodes("td");
                // Expected columns (TeamRankings): Position, Team, Current, Last 3, Last 1, Home, Away, Prev Season
                if (cells == null || cells.Count < 7) continue;

                // Normalize team name to the canonical DB form (defensive against aliases/abbreviations)
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

                // Find or create TeamStat for (Team, StatType, Perspective)
                var stat = _context.TeamStats.FirstOrDefault(ts =>
                    ts.TeamId == team.Id &&
                    ts.StatTypeId == statType.Id &&
                    ts.Perspective == perspective);

                if (stat == null)
                {
                    stat = new TeamStat
                    {
                        TeamId = team.Id,
                        StatTypeId = statType.Id,
                        Perspective = perspective
                    };
                    _context.TeamStats.Add(stat);
                }

                // Assign values parsed from the row; percentages (e.g., YRFI) are stored as numeric values (percent sign stripped)
                stat.CurrentSeason = ParseCell(cells, 2);
                stat.Last3Games    = ParseCell(cells, 3);
                stat.LastGame      = ParseCell(cells, 4);
                stat.Home          = ParseCell(cells, 5);
                stat.Away          = ParseCell(cells, 6);
                stat.PrevSeason    = cells.Count > 7 ? ParseCell(cells, 7) : null;
            }

            // Flush all pending inserts/updates for this metric
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Legacy helper retained for compatibility; current importer ensures the category
        /// </summary>
        private async Task<int> GetCuriousFactsCategoryIdAsync()
        {
            var category = _context.StatCategories.FirstOrDefault(c => c.Name == "CuriousFacts");
            if (category == null)
            {
                category = new StatCategory { Name = "CuriousFacts" };
                _context.StatCategories.Add(category);
                await _context.SaveChangesAsync();
            }
            return category.Id;
        }

        // --- local parsing helper ---

        /// <summary>
        /// Extracts a numeric value from a TD cell:
        /// - Trims whitespace
        /// - Removes percent signs (if present)
        /// - HTML-deentitizes
        /// - Parses using <see cref="Utilities.Parse(string)"/>
        /// Returns null if the cell is empty or cannot be parsed.
        /// </summary>
        private static float? ParseCell(IList<HtmlNode> cells, int index)
        {
            if (index >= cells.Count) return null;

            var txt = Utilities.CleanText(cells[index].InnerText);
            if (string.IsNullOrWhiteSpace(txt)) return null;

            // Drop percent symbols (YRFI/NRFI etc.) and normalize content
            txt = txt.Replace("%", "").Trim();
            txt = HtmlEntity.DeEntitize(txt);

            return Utilities.Parse(txt);
        }
    }
}
