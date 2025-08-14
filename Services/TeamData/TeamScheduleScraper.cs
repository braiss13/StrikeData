using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace StrikeData.Services.TeamData
{
    // --- Modelos de datos ---

    /// <summary>Una fila del calendario (número de juego, fecha, rival, marcador, decisión, récord acumulado).</summary>
    public class ScheduleEntry
    {
        public int GameNumber { get; set; }
        public DateTime Date { get; set; }
        public string Opponent { get; set; } = string.Empty;
        public string Score { get; set; } = string.Empty;
        public string Decision { get; set; } = string.Empty;
        public string Record { get; set; } = string.Empty;
    }

    /// <summary>División mensual: mes, número de juegos, victorias, derrotas y porcentaje de victorias.</summary>
    public class MonthlySplit
    {
        public string Month { get; set; } = string.Empty;
        public int Games { get; set; }
        public int Won { get; set; }
        public int Lost { get; set; }
        public float WinPercentage { get; set; }
    }

    /// <summary>Enfrentamiento contra un rival: rival, juegos totales, victorias, derrotas y porcentaje de victorias.</summary>
    public class TeamSplit
    {
        public string Opponent { get; set; } = string.Empty;
        public int Games { get; set; }
        public int Won { get; set; }
        public int Lost { get; set; }
        public float WinPercentage { get; set; }
    }

    /// <summary>Contiene todas las secciones extraídas de la página de calendario de un equipo.</summary>
    public class TeamScheduleResult
    {
        public List<ScheduleEntry> Schedule { get; set; } = new();
        public List<MonthlySplit> MonthlySplits { get; set; } = new();
        public List<TeamSplit> TeamSplits { get; set; } = new();
    }

    /// <summary>
    /// Servicio de scraping para Baseball-Almanac. Descarga la página de calendario de un equipo y año concretos,
    /// y extrae el calendario completo, los splits mensuales y los splits por rival.
    /// </summary>
    public class TeamScheduleScraper
    {
        private readonly HttpClient _httpClient;

        public TeamScheduleScraper(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// Descarga y analiza la página de calendario de un equipo (por abreviatura) en un año concreto.
        /// </summary>
        public async Task<TeamScheduleResult> GetTeamScheduleAndSplitsAsync(string teamCode, int year)
        {

            if (string.IsNullOrWhiteSpace(teamCode))
                throw new ArgumentException("Team code must be specified", nameof(teamCode));

            var url = $"https://www.baseball-almanac.com/teamstats/schedule.php?y={year}&t={teamCode.ToUpperInvariant()}";

            // Configurar la solicitud para imitar a un navegador real
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:100.0) Gecko/20100101 Firefox/100.0");
            request.Headers.Referrer = new Uri("https://www.baseball-almanac.com/teammenu.shtml");

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var html = await response.Content.ReadAsStringAsync();
            // Comprobación básica: si el HTML es demasiado corto, probablemente sea un error 404/403
            if (html.Length < 1000)
                throw new InvalidOperationException($"Unexpected response length ({html.Length} characters). The page may not exist.");

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var result = new TeamScheduleResult();
            ParseScheduleTable(doc, result);
            ParseMonthlySplits(doc, result);
            ParseTeamSplits(doc, result);
            return result;
        }

        // Extrae la tabla de calendario principal (Game, Date, Opponent, Score, Decision, Record)
        private static void ParseScheduleTable(HtmlDocument doc, TeamScheduleResult result)
        {
            // Busca la primera tabla que contenga las palabras "Game", "Opponent" y "Record"
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

                if (!headerSkipped)
                {
                    headerSkipped = true; // saltar la cabecera
                    continue;
                }

                try
                {
                    var entry = new ScheduleEntry();
                    entry.GameNumber = int.TryParse(cells[0].InnerText.Trim(), out var num) ? num : 0;

                    var dateText = cells[1].InnerText.Trim();
                    if (DateTime.TryParseExact(dateText, "MM-dd-yyyy", CultureInfo.InvariantCulture,
                                               DateTimeStyles.None, out var dt))
                    {
                        entry.Date = dt;
                    }
                    else
                    {
                        DateTime.TryParse(dateText, out dt);
                        entry.Date = dt;
                    }

                    entry.Opponent = HtmlEntity.DeEntitize(cells[2].InnerText.Trim());
                    entry.Score = cells[3].InnerText.Trim();
                    entry.Decision = cells[4].InnerText.Trim();
                    entry.Record = cells[5].InnerText.Trim();

                    result.Schedule.Add(entry);
                }
                catch { /* ignorar filas mal formateadas */ }
            }
        }

        /// <summary>
        /// Extrae los splits mensuales de una página de Baseball Almanac.
        /// Intenta primero parsear una tabla; si no existe, recurre a texto libre.
        /// </summary>
        private static void ParseMonthlySplits(HtmlDocument doc, TeamScheduleResult result)
        {
            // Localiza el encabezado "Monthly Splits" sin distinguir mayúsculas/minúsculas.
            var headerNode = doc.DocumentNode.SelectSingleNode(
                "//*[contains(translate(text(), 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'monthly splits')]"
            );

            Console.WriteLine("Monthly header found: " + (headerNode != null));

            if (headerNode == null)
                return;

            // 1) Intentar parsear una tabla inmediatamente después del encabezado
            var table = headerNode.SelectSingleNode("following::table[1]");
            Console.WriteLine("Monthly table found: " + (table != null));

            if (table != null)
            {
                var rows = table.SelectNodes(".//tr");
                if (rows != null)
                {
                    bool headerSkipped = false;
                    foreach (var row in rows)
                    {
                        var cells = row.SelectNodes("th|td");
                        if (cells == null || cells.Count < 4)
                            continue;
                        // Omitir cabecera
                        if (!headerSkipped)
                        {
                            headerSkipped = true;
                            continue;
                        }
                        try
                        {
                            var label = cells[0].InnerText.Trim();
                            string monthName = label;
                            int gamesCount = 0;
                            int parenStart = label.IndexOf('(');
                            int parenEnd = label.IndexOf(')');
                            if (parenStart >= 0 && parenEnd > parenStart)
                            {
                                monthName = label.Substring(0, parenStart).Trim();
                                var gamesStr = label.Substring(parenStart + 1, parenEnd - parenStart - 1);
                                int.TryParse(gamesStr, out gamesCount);
                            }
                            int won = int.TryParse(cells[1].InnerText.Trim(), out var w) ? w : 0;
                            int lost = int.TryParse(cells[2].InnerText.Trim(), out var l) ? l : 0;
                            float wp = float.TryParse(cells[3].InnerText.Trim(), NumberStyles.Float,
                                                      CultureInfo.InvariantCulture, out var wpVal) ? wpVal : 0f;

                            result.MonthlySplits.Add(new MonthlySplit
                            {
                                Month = monthName,
                                Games = gamesCount,
                                Won = won,
                                Lost = lost,
                                WinPercentage = wp
                            });
                        }
                        catch
                        {
                            continue;
                        }
                    }
                    if (result.MonthlySplits.Count > 0)
                        return;
                }
            }

            // 2) Si no hay tabla, reunir el texto entre "Monthly Splits" y el siguiente encabezado relevante
            var sb = new StringBuilder();
            var node = headerNode.NextSibling;
            while (node != null)
            {
                var text = node.InnerText?.Trim();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    var lower = text.ToLowerInvariant();
                    // Cortar en cuanto aparece la siguiente sección
                    if (lower.Contains("team vs team splits") || lower.Contains("score related"))
                        break;

                    sb.AppendLine(HtmlEntity.DeEntitize(text));
                }
                node = node.NextSibling;
            }
            var sectionText = sb.ToString();
            Console.WriteLine("Monthly section text:\n" + sectionText);

            if (string.IsNullOrWhiteSpace(sectionText))
            {
                // Buscamos "April (26) 17 9 0.654", etc., en todo el texto de la página.
                var plainTextFallback = HtmlEntity.DeEntitize(doc.DocumentNode.InnerText);
                var fallbackMatches = new Regex(
                    @"(?<month>January|February|March|April|May|June|July|August|September|October|November|December)\s*\((?<games>\d+)\)\s*(?<won>\d+)\s*(?<lost>\d+)\s*(?<wp>[0-9\.]+)",
                    RegexOptions.Multiline | RegexOptions.IgnoreCase
                ).Matches(plainTextFallback);

                foreach (Match m in fallbackMatches)
                {
                    var month = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(m.Groups["month"].Value.ToLower());
                    int games = int.Parse(m.Groups["games"].Value);
                    int won = int.Parse(m.Groups["won"].Value);
                    int lost = int.Parse(m.Groups["lost"].Value);
                    float wp = float.Parse(m.Groups["wp"].Value, CultureInfo.InvariantCulture);

                    result.MonthlySplits.Add(new MonthlySplit
                    {
                        Month = month,
                        Games = games,
                        Won = won,
                        Lost = lost,
                        WinPercentage = wp
                    });
                }
                return;
            }


            // Expresión regular para líneas de FastFacts tipo "April (26) 17 9 0.654"
            var pattern = new System.Text.RegularExpressions.Regex(
                @"(?<month>[A-Za-z]+)\s*\((?<games>\d+)\)\s*(?<won>\d+)\s*(?<lost>\d+)\s*(?<wp>[0-9\.]+)",
                System.Text.RegularExpressions.RegexOptions.Multiline
            );
            var matches = pattern.Matches(sectionText);
            foreach (System.Text.RegularExpressions.Match m in matches)
            {
                var month = m.Groups["month"].Value;
                int games = int.TryParse(m.Groups["games"].Value, out var g) ? g : 0;
                int won = int.TryParse(m.Groups["won"].Value, out var w) ? w : 0;
                int lost = int.TryParse(m.Groups["lost"].Value, out var l) ? l : 0;
                float wp = float.TryParse(m.Groups["wp"].Value, NumberStyles.Float,
                                           CultureInfo.InvariantCulture, out var wpVal) ? wpVal : 0f;

                result.MonthlySplits.Add(new MonthlySplit
                {
                    Month = month,
                    Games = games,
                    Won = won,
                    Lost = lost,
                    WinPercentage = wp
                });
            }
        }

        /// <summary>
        /// Extrae los splits contra rivales (Team vs Team) de una página de Baseball Almanac.
        /// Intenta primero parsear una tabla; si no existe, recurre a texto libre.
        /// </summary>
        private static void ParseTeamSplits(HtmlDocument doc, TeamScheduleResult result)
        {
            var headerNode = doc.DocumentNode.SelectSingleNode(
                "//*[contains(translate(text(), 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'team vs team splits')]"
            );
            if (headerNode == null)
                return;

            // 1) Intentar leer una tabla
            var table = headerNode.SelectSingleNode("following::table[1]");
            if (table != null)
            {
                var rows = table.SelectNodes(".//tr");
                if (rows != null)
                {
                    bool headerSkipped = false;
                    foreach (var row in rows)
                    {
                        var cells = row.SelectNodes("th|td");
                        if (cells == null || cells.Count < 4)
                            continue;
                        if (!headerSkipped)
                        {
                            headerSkipped = true;
                            continue;
                        }
                        try
                        {
                            var label = cells[0].InnerText.Trim();
                            var opponent = label;
                            int games = 0;
                            int parenStart = label.IndexOf('(');
                            int parenEnd = label.IndexOf(')');
                            if (parenStart >= 0 && parenEnd > parenStart)
                            {
                                opponent = label.Substring(0, parenStart).Trim();
                                var gamesStr = label.Substring(parenStart + 1, parenEnd - parenStart - 1);
                                int.TryParse(gamesStr, out games);
                            }
                            int won = int.TryParse(cells[1].InnerText.Trim(), out var w) ? w : 0;
                            int lost = int.TryParse(cells[2].InnerText.Trim(), out var l) ? l : 0;
                            float wp = float.TryParse(cells[3].InnerText.Trim(), NumberStyles.Float,
                                                       CultureInfo.InvariantCulture, out var wpVal) ? wpVal : 0f;

                            result.TeamSplits.Add(new TeamSplit
                            {
                                Opponent = opponent,
                                Games = games,
                                Won = won,
                                Lost = lost,
                                WinPercentage = wp
                            });
                        }
                        catch
                        {
                            continue;
                        }
                    }
                    if (result.TeamSplits.Count > 0)
                        return;
                }
            }

            // 2) Si no hay tabla, reunir texto hasta el siguiente encabezado significativo
            var sb = new StringBuilder();
            var node = headerNode.NextSibling;
            while (node != null)
            {
                var text = node.InnerText?.Trim();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    var lower = text.ToLowerInvariant();
                    if (lower.Contains("score related") || lower.Contains("during the regular"))
                        break;

                    sb.AppendLine(HtmlEntity.DeEntitize(text));
                }
                node = node.NextSibling;
            }
            var sectionText = sb.ToString();
            if (string.IsNullOrWhiteSpace(sectionText))
            {
                var plainTextFallback = HtmlEntity.DeEntitize(doc.DocumentNode.InnerText);
                var fallbackRegex = new Regex(
                    @"(?<opp>[^\(\n]+)\s*\((?<games>\d+)\)\s*(?<won>\d+)\s*(?<lost>\d+)\s*(?<wp>[0-9\.]+)",
                    RegexOptions.Multiline
                );

                var monthNames = new[] { "january", "february", "march", "april", "may", "june", "july", "august", "september", "october", "november", "december" };
                foreach (Match m in fallbackRegex.Matches(plainTextFallback))
                {
                    var opponentCandidate = m.Groups["opp"].Value.Trim();
                    if (monthNames.Contains(opponentCandidate.ToLower()))
                        continue; // descartar líneas de Monthly Splits

                    int games = int.Parse(m.Groups["games"].Value);
                    int won = int.Parse(m.Groups["won"].Value);
                    int lost = int.Parse(m.Groups["lost"].Value);
                    float wp = float.Parse(m.Groups["wp"].Value, CultureInfo.InvariantCulture);

                    result.TeamSplits.Add(new TeamSplit
                    {
                        Opponent = opponentCandidate,
                        Games = games,
                        Won = won,
                        Lost = lost,
                        WinPercentage = wp
                    });
                }
                return;
            }


            // Expresión regular para líneas tipo "New York Yankees (13) 8 5 0.615"
            var pattern2 = new System.Text.RegularExpressions.Regex(
                @"(?<opponent>[^\(\n]+)\s*\((?<games>\d+)\)\s*(?<won>\d+)\s*(?<lost>\d+)\s*(?<wp>[0-9\.]+)",
                System.Text.RegularExpressions.RegexOptions.Multiline
            );
            var matches2 = pattern2.Matches(sectionText);
            foreach (System.Text.RegularExpressions.Match m in matches2)
            {
                var opponent = m.Groups["opponent"].Value.Trim();
                int games = int.TryParse(m.Groups["games"].Value, out var g) ? g : 0;
                int won = int.TryParse(m.Groups["won"].Value, out var w) ? w : 0;
                int lost = int.TryParse(m.Groups["lost"].Value, out var l) ? l : 0;
                float wp = float.TryParse(m.Groups["wp"].Value, NumberStyles.Float,
                                              CultureInfo.InvariantCulture, out var wpVal) ? wpVal : 0f;

                result.TeamSplits.Add(new TeamSplit
                {
                    Opponent = opponent,
                    Games = games,
                    Won = won,
                    Lost = lost,
                    WinPercentage = wp
                });
            }
        }

    }
}
