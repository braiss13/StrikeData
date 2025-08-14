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
            ParseFastFacts(doc, result); // ← analiza Monthly Splits y Team Splits de FastFacts
            return result;
        }

        // Extrae la tabla de calendario principal (Game, Date, Opponent, Score, Decision, Record)
        private static void ParseScheduleTable(HtmlDocument doc, TeamScheduleResult result)
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

                // 1) El número de juego DEBE ser numérico (evita gameNumber=0 y filas “demo”)
                if (!int.TryParse(cells[0].InnerText.Trim(), out var gameNum)) continue;

                // 2) La fecha debe parsear; si no, descarta la fila
                var dateText = cells[1].InnerText.Trim();
                DateTime date;
                if (!DateTime.TryParseExact(dateText, "MM-dd-yyyy", CultureInfo.InvariantCulture,
                                            DateTimeStyles.None, out date))
                {
                    if (!DateTime.TryParse(dateText, out date)) continue;
                }

                // 3) Oponente válido (evita la demo “Opponent”)
                var opponent = HtmlEntity.DeEntitize(cells[2].InnerText.Trim());
                if (string.Equals(opponent, "Opponent", StringComparison.OrdinalIgnoreCase)) continue;

                var score = cells[3].InnerText.Trim();
                var decision = cells[4].InnerText.Trim();
                var record = cells[5].InnerText.Trim();

                result.Schedule.Add(new ScheduleEntry
                {
                    GameNumber = gameNum,
                    Date = date,
                    Opponent = opponent,   // OJO: aquí guardamos tal cual (“vs Baltimore…”, “at …”)
                    Score = score,
                    Decision = decision,
                    Record = record
                });
            }
        }

        /// <summary>
        /// Analiza la sección Fast Facts. Cada tabla de esta sección puede contener
        /// Monthly Splits, Team vs Team Splits o Score Related Splits. Sólo se procesan
        /// las filas con un número de partidos entre paréntesis; las filas con “:”
        /// (Shutouts, 1-Run Games, etc.) se descartan.
        /// </summary>
        public static void ParseFastFacts(HtmlDocument doc, TeamScheduleResult result)
        {
            var fastFactsDiv = doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'fast-facts')]");
            if (fastFactsDiv == null) return;

            var tables = fastFactsDiv.SelectNodes(".//table[contains(@class, 'fastfacttable')]");
            if (tables == null) return;

            var monthNames = new HashSet<string>(new[] {
                "january","february","march","april","may","june",
                "july","august","september","october","november","december"
            });

            // Etiquetas que NO queremos en TeamSplits (Score Related Splits)
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
                    if (!float.TryParse(cells[3].InnerText.Trim(), NumberStyles.Float,
                                        CultureInfo.InvariantCulture, out var wp)) continue;

                    // Coherencia básica (no bloquea ongoing si quieres permitirlo)
                    if (games > 0 && wins + losses != games)
                    {
                        // Si prefieres ser estricto, usa: continue;
                    }

                    // Clasificación: mes -> Monthly; si no -> Team
                    if (monthNames.Contains(baseKey))
                    {
                        result.MonthlySplits.Add(new MonthlySplit
                        {
                            Month = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(baseName.ToLowerInvariant()),
                            Games = games,
                            Won = wins,
                            Lost = losses,
                            WinPercentage = wp
                        });
                    }
                    else
                    {
                        result.TeamSplits.Add(new TeamSplit
                        {
                            Opponent = baseName,
                            Games = games,
                            Won = wins,
                            Lost = losses,
                            WinPercentage = wp
                        });
                    }
                }
            }
        }



    }
}
