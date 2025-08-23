using System.Globalization;
using HtmlAgilityPack;
using StrikeData.Models.Scraping;
using StrikeData.Services.Common;

namespace StrikeData.Services.TeamData
{
    /// <summary>
    /// Scraper de Baseball-Almanac para calendario y splits de equipo.
    /// Hereda utilidades comunes de BaseballAlmanacScraperBase.
    /// </summary>
    public class TeamScheduleScraper : BaseballAlmanacScraperBase
    {
        public TeamScheduleScraper(HttpClient httpClient) : base(httpClient) { }

        /// <summary>
        /// Descarga y analiza la página de calendario de un equipo (por abreviatura) en un año concreto.
        /// Devuelve DTOs de scraping (no entidades EF).
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

        /// Extrae la tabla de calendario principal (Game, Date, Opponent, Score, Decision, Record).
        private static void ParseScheduleTable(HtmlDocument doc, TeamScheduleResultDto result)
        {
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

                // Saltar cabecera
                if (!headerSkipped) { headerSkipped = true; continue; }

                // 1) Número de juego válido
                if (!int.TryParse(cells[0].InnerText.Trim(), out var gameNum)) continue;

                // 2) Fecha válida
                var dateText = cells[1].InnerText.Trim();
                if (!DateTime.TryParseExact(dateText, "MM-dd-yyyy", CultureInfo.InvariantCulture,
                                            DateTimeStyles.None, out var date))
                {
                    if (!DateTime.TryParse(dateText, out date)) continue;
                }

                // 3) Oponente válido (evitar “Opponent” demo)
                var opponent = HtmlEntity.DeEntitize(cells[2].InnerText.Trim());
                if (string.Equals(opponent, "Opponent", StringComparison.OrdinalIgnoreCase)) continue;

                var score = cells[3].InnerText.Trim();
                var decision = cells[4].InnerText.Trim();
                var record = cells[5].InnerText.Trim();

                result.Schedule.Add(new ScheduleEntryDto
                {
                    GameNumber = gameNum,
                    Date = date,
                    Opponent = opponent, // ya incluye “vs/at” en el HTML
                    Score = score,
                    Decision = decision,
                    Record = record
                });
            }
        }

        // Analiza la sección Fast Facts (tablas con clase fastfacttable) y separa:
        // - Monthly Splits (meses)
        // - Team vs Team Splits (rivales)
        // Excluye Score Related Splits (Shutouts, 1-Run Games, Blowouts).
        private static void ParseFastFacts(HtmlDocument doc, TeamScheduleResultDto result)
        {
            var fastFactsDiv = doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'fast-facts')]");
            if (fastFactsDiv == null) return;

            var tables = fastFactsDiv.SelectNodes(".//table[contains(@class, 'fastfacttable')]");
            if (tables == null) return;

            var monthNames = new HashSet<string>(new[] {
                "january","february","march","april","may","june",
                "july","august","september","october","november","december"
            });

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

                foreach (var row in rows.Skip(1)) // saltar cabecera
                {
                    var cells = row.SelectNodes("td");
                    if (cells == null || cells.Count < 4) continue;

                    var rawLabel = (cells[0].InnerText ?? string.Empty).Trim();

                    // Debe haber paréntesis con nº de juegos
                    var p1 = rawLabel.IndexOf('(');
                    var p2 = rawLabel.IndexOf(')');
                    if (p1 < 0 || p2 <= p1) continue;

                    // Nombre base (sin "(N)")
                    var baseName = rawLabel.Substring(0, p1).Trim();

                    // Normalizamos para comparar
                    var baseKey = System.Text.RegularExpressions.Regex
                        .Replace(baseName.ToLowerInvariant(), @"\s+", " ")
                        .Trim();

                    // DESCARTE explícito de Score Related
                    if (scoreRelated.Contains(baseKey)) continue;
                    // Red de seguridad: cualquier etiqueta “algo Games” no es un rival
                    if (baseKey.EndsWith(" games")) continue;

                    // Parse numérico
                    var gamesStr = rawLabel.Substring(p1 + 1, p2 - p1 - 1);
                    if (!int.TryParse(gamesStr, out var games)) continue;
                    if (!int.TryParse(cells[1].InnerText.Trim(), out var wins)) continue;
                    if (!int.TryParse(cells[2].InnerText.Trim(), out var losses)) continue;

                    if (!decimal.TryParse(cells[3].InnerText.Trim(), NumberStyles.Float,
                                          CultureInfo.InvariantCulture, out var wpDec))
                    {
                        wpDec = 0m;
                    }

                    // Clasificación: mes -> Monthly; si no -> Team
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
