using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
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

        // Extrae la tabla de splits mensuales (Monthly Splits)
        private static void ParseMonthlySplits(HtmlDocument doc, TeamScheduleResult result)
        {
            var header = doc.DocumentNode.SelectSingleNode("//*[contains(translate(text(), 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'monthly splits')]");

            if (header == null)
            {
                Console.WriteLine("No se encontró la sección de Monthly Splits.");
                return;
            }

            var table = header.SelectSingleNode("following::table[1]");
            if (table == null) return;
            var rows = table.SelectNodes(".//tr");
            if (rows == null) return;

            bool headerSkipped = false;
            foreach (var row in rows)
            {
                var cells = row.SelectNodes("th|td");
                if (cells == null || cells.Count < 4) continue;

                if (!headerSkipped)
                {
                    headerSkipped = true;
                    continue;
                }

                try
                {
                    var label = cells[0].InnerText.Trim();
                    var monthName = label;
                    int gamesCount = 0;
                    var parenStart = label.IndexOf('(');
                    var parenEnd = label.IndexOf(')');
                    if (parenStart >= 0 && parenEnd > parenStart)
                    {
                        monthName = label.Substring(0, parenStart).Trim();
                        var gamesStr = label.Substring(parenStart + 1, parenEnd - parenStart - 1);
                        int.TryParse(gamesStr, out gamesCount);
                    }

                    var won = int.TryParse(cells[1].InnerText.Trim(), out var w) ? w : 0;
                    var lost = int.TryParse(cells[2].InnerText.Trim(), out var l) ? l : 0;
                    var wp = float.TryParse(cells[3].InnerText.Trim(), NumberStyles.Float,
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
                catch { }
            }
        }

        // Extrae la tabla de splits por rival (Team vs Team Splits)
        private static void ParseTeamSplits(HtmlDocument doc, TeamScheduleResult result)
        {
            var header = doc.DocumentNode
                .SelectSingleNode("//*[contains(text(), 'Team vs Team Splits')]");
            if (header == null) return;

            var table = header.SelectSingleNode("following::table[1]");
            if (table == null) return;

            var rows = table.SelectNodes(".//tr");
            if (rows == null) return;

            bool headerSkipped = false;
            foreach (var row in rows)
            {
                var cells = row.SelectNodes("th|td");
                if (cells == null || cells.Count < 4) continue;

                if (!headerSkipped)
                {
                    headerSkipped = true;
                    continue;
                }

                try
                {
                    var label = cells[0].InnerText.Trim();
                    var opponentName = label;
                    int games = 0;
                    var parenStart = label.IndexOf('(');
                    var parenEnd = label.IndexOf(')');
                    if (parenStart >= 0 && parenEnd > parenStart)
                    {
                        opponentName = label.Substring(0, parenStart).Trim();
                        var gamesStr = label.Substring(parenStart + 1, parenEnd - parenStart - 1);
                        int.TryParse(gamesStr, out games);
                    }

                    var won = int.TryParse(cells[1].InnerText.Trim(), out var w) ? w : 0;
                    var lost = int.TryParse(cells[2].InnerText.Trim(), out var l) ? l : 0;
                    var wp = float.TryParse(cells[3].InnerText.Trim(), NumberStyles.Float,
                                            CultureInfo.InvariantCulture, out var wpVal) ? wpVal : 0f;

                    result.TeamSplits.Add(new TeamSplit
                    {
                        Opponent = opponentName,
                        Games = games,
                        Won = won,
                        Lost = lost,
                        WinPercentage = wp
                    });
                }
                catch { }
            }
        }
    }
}
