using System.Globalization;
using HtmlAgilityPack;
using StrikeData.Models.Scraping;

namespace StrikeData.Services.TeamData.Scrapers
{
    /// <summary>
    /// Baseball Almanac scraper for a team's schedule page:
    /// - Loads the target HTML,
    /// - Extracts the main schedule table (per-game rows),
    /// - Extracts "Fast Facts" splits (monthly and opponent).
    /// Returns DTOs only (EF entities are managed by the importer).
    /// </summary>
    public class TeamScheduleScraper : BaseballAlmanacScraperBase
    {
        public TeamScheduleScraper(HttpClient httpClient) : base(httpClient) { }

        /// <summary>
        /// Downloads and parses the team schedule and split summaries for a given year.
        /// </summary>
        public async Task<TeamScheduleResultDto> GetTeamScheduleAndSplitsAsync(string teamCode, int year)
        {
            if (string.IsNullOrWhiteSpace(teamCode))
                throw new ArgumentException("Team code must be specified", nameof(teamCode));

            var url = $"https://www.baseball-almanac.com/teamstats/schedule.php?y={year}&t={teamCode.ToUpperInvariant()}";
            var doc = await LoadDocumentAsync(url);

            var result = new TeamScheduleResultDto();
            ParseScheduleTable(doc, result);
            ParseFastFacts(doc, result);
            return result;
        }

        /// <summary>
        /// Extracts the main schedule table with columns:
        /// Game, Date, Opponent, Score, Decision, Record.
        /// </summary>
        private static void ParseScheduleTable(HtmlDocument doc, TeamScheduleResultDto result)
        {
            // Locate the schedule table by checking known header labels.
            var table = doc.DocumentNode
                .SelectNodes("//table")?
                .FirstOrDefault(t =>
                    t.InnerText.Contains("Game") &&
                    t.InnerText.Contains("Opponent") &&
                    t.InnerText.Contains("Record"));

            if (table == null) return;

            var rows = table.SelectNodes(".//tr");
            if (rows == null) return;

            bool headerSkipped = false;
            foreach (var row in rows)
            {
                var cells = row.SelectNodes("th|td");
                if (cells == null || cells.Count < 6) continue;

                // Skip the header row exactly once.
                if (!headerSkipped) { headerSkipped = true; continue; }

                // 1) Valid game number
                if (!int.TryParse(cells[0].InnerText.Trim(), out var gameNum)) continue;

                // 2) Date parsing: prefer fixed "MM-dd-yyyy"; fallback to a looser parse.
                var dateText = cells[1].InnerText.Trim();
                if (!DateTime.TryParseExact(dateText, "MM-dd-yyyy", CultureInfo.InvariantCulture,
                                            DateTimeStyles.None, out var date))
                {
                    if (!DateTime.TryParse(dateText, out date)) continue;
                }

                // 3) Opponent text (keep as-is; importer will normalize name and home/away flag).
                var opponent = HtmlEntity.DeEntitize(cells[2].InnerText.Trim());
                if (string.Equals(opponent, "Opponent", StringComparison.OrdinalIgnoreCase)) continue;

                var score = cells[3].InnerText.Trim();
                var decision = cells[4].InnerText.Trim();
                var record = cells[5].InnerText.Trim();

                result.Schedule.Add(new ScheduleEntryDto
                {
                    GameNumber = gameNum,
                    Date = date,
                    Opponent = opponent, // includes "vs"/"at" prefix in HTML
                    Score = score,
                    Decision = decision,
                    Record = record
                });
            }
        }

        /// <summary>
        /// Parses the "Fast Facts" area and separates:
        /// - Monthly Splits (rows named after months),
        /// - Team vs Team Splits (remaining rows).
        /// Excludes score-related summaries (Shutouts, 1-Run Games, Blowouts).
        /// </summary>
        private static void ParseFastFacts(HtmlDocument doc, TeamScheduleResultDto result)
        {
            var fastFactsDiv = doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'fast-facts')]");
            if (fastFactsDiv == null) return;

            var tables = fastFactsDiv.SelectNodes(".//table[contains(@class, 'fastfacttable')]");
            if (tables == null) return;

            // Lowercase month names to detect monthly split tables robustly.
            var monthNames = new HashSet<string>(new[] {
                "january","february","march","april","may","june",
                "july","august","september","october","november","december"
            });

            // Known non-opponent, score-related summaries to ignore.
            var scoreRelated = new HashSet<string>(new[] {
                "shutouts",
                "1-run games",
                "one-run games",
                "blowouts"
            });

            foreach (var table in tables)
            {
                var rows = table.SelectNodes(".//tr");
                if (rows == null || rows.Count < 2) continue;

                foreach (var row in rows.Skip(1)) // skip header
                {
                    var cells = row.SelectNodes("td");
                    if (cells == null || cells.Count < 4) continue;

                    var rawLabel = (cells[0].InnerText ?? string.Empty).Trim();

                    var p1 = rawLabel.IndexOf('(');
                    var p2 = rawLabel.IndexOf(')');
                    if (p1 < 0 || p2 <= p1) continue;

                    // Base label without "(N)".
                    var baseName = rawLabel.Substring(0, p1).Trim();

                    // Normalize for classification (lowercase, collapse whitespace).
                    var baseKey = System.Text.RegularExpressions.Regex
                        .Replace(baseName.ToLowerInvariant(), @"\s+", " ")
                        .Trim();

                    // Hard exclusions and simple guards to avoid score-related buckets.
                    if (scoreRelated.Contains(baseKey)) continue;
                    if (baseKey.EndsWith(" games")) continue;

                    // Parse numeric values: Games, Won, Lost, Win%.
                    var gamesStr = rawLabel.Substring(p1 + 1, p2 - p1 - 1);
                    if (!int.TryParse(gamesStr, out var games)) continue;
                    if (!int.TryParse(cells[1].InnerText.Trim(), out var wins)) continue;
                    if (!int.TryParse(cells[2].InnerText.Trim(), out var losses)) continue;

                    if (!decimal.TryParse(cells[3].InnerText.Trim(), NumberStyles.Float,
                                          CultureInfo.InvariantCulture, out var wpDec))
                    {
                        wpDec = 0m;
                    }

                    // Classify row as Monthly or Opponent based on month name match.
                    if (monthNames.Contains(baseKey))
                    {
                        result.MonthlySplits.Add(new MonthlySplitDto
                        {
                            Month = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(baseName.ToLowerInvariant()),
                            Games = games,
                            Won = wins,
                            Lost = losses,
                            WinPercentage = wpDec
                        });
                    }
                    else
                    {
                        result.TeamSplits.Add(new TeamSplitDto
                        {
                            Opponent = baseName,
                            Games = games,
                            Won = wins,
                            Lost = losses,
                            WinPercentage = wpDec
                        });
                    }
                }
            }
        }
    }
}
