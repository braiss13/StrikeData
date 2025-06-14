using HtmlAgilityPack;
using StrikeData.Data;
using StrikeData.Models;
using System.Globalization;
using System.Net.Http;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace StrikeData.Services
{
    public class TeamDataImporter
    {
        private readonly AppDbContext _context;
        private readonly HttpClient _httpClient;

        public TeamDataImporter(AppDbContext context)
        {
            _context = context;
            _httpClient = new HttpClient();
        }

        /*
        public async Task ImportWinTrendsAsync()
        {

            var url = "https://www.teamrankings.com/mlb/trends/win_trends";
            var rows = await ScrapeTable(url);

            foreach (var row in rows)
            {
                if (row.Count < 3) continue;

                string name = row[0];
                string record = row[1];
                if (!float.TryParse(row[2].TrimEnd('%'), out float winPct))
                    continue;

                var team = _context.Teams.FirstOrDefault(t => t.Name == name);
                if (team == null)
                {
                    team = new Team { Name = name, SeasonYear = 2025 };
                    _context.Teams.Add(team);
                }

                team.OverallRecord = record;
                team.WinPercentage = winPct / 100f;
            }

            await _context.SaveChangesAsync();
            
        }
        */

        // Método principal -> Contiene las llamadas a los dos métodos de scrapping
        public async Task ImportAllStatsAsync()
        {

            // 2. Luego, el scraping de TeamRankings donde se obtienen los promedios por partido de cada aspecto -> SE CREA UN DICCIONARIO
            var stats = new Dictionary<string, string>
            {
                { "R", "https://www.teamrankings.com/mlb/stat/runs-per-game" },
                { "AB", "https://www.teamrankings.com/mlb/stat/at-bats-per-game" },
                { "H", "https://www.teamrankings.com/mlb/stat/hits-per-game" },
                { "HR", "https://www.teamrankings.com/mlb/stat/home-runs-per-game" },
                { "S", "https://www.teamrankings.com/mlb/stat/singles-per-game" },
                { "2B", "https://www.teamrankings.com/mlb/stat/doubles-per-game" },
                { "3B", "https://www.teamrankings.com/mlb/stat/triples-per-game" },
                { "RBI", "https://www.teamrankings.com/mlb/stat/rbis-per-game" },
                { "BB", "https://www.teamrankings.com/mlb/stat/walks-per-game" },
                { "SO", "https://www.teamrankings.com/mlb/stat/strikeouts-per-game" },
                { "SB", "https://www.teamrankings.com/mlb/stat/stolen-bases-per-game" },
                { "CS", "https://www.teamrankings.com/mlb/stat/caught-stealing-per-game" },
                { "SH", "https://www.teamrankings.com/mlb/stat/sacrifice-hits-per-game" },
                { "SF", "https://www.teamrankings.com/mlb/stat/sacrifice-flys-per-game" },
                { "HP", "https://www.teamrankings.com/mlb/stat/hit-by-pitch-per-game" },
                { "GIDP", "https://www.teamrankings.com/mlb/stat/grounded-into-double-plays-per-game" },
                { "TB", "https://www.teamrankings.com/mlb/stat/total-bases-per-game" },
                { "AVG", "https://www.teamrankings.com/mlb/stat/batting-average" },
                { "SLG", "https://www.teamrankings.com/mlb/stat/slugging-pct" },
                { "OBP", "https://www.teamrankings.com/mlb/stat/on-base-pct" },
                { "OPS", "https://www.teamrankings.com/mlb/stat/on-base-plus-slugging-pct" }
            };

            // Va recorriendo el diccionario y llama al método para cada estadística por separado, indicando la estadística y la URL a consultar
            foreach (var stat in stats)
            {
                await ImportStatAsync(stat.Key, stat.Value);
            }

            // 1. Primero, scrapea la página de la MLB para obtener TOTAL y GAMES
            await ImportStatsFromMLBAsync();
        }

        // Este método se encarga de obtener las estadísticas necesarias de la MLB
        public async Task ImportStatsFromMLBAsync()
        {
            // Se carga el documento HTML desde la URL de la MLB utilizando la librería HtmlAgilityPack
            var url = "https://www.mlb.com/stats/team";
            var web = new HtmlWeb();
            var doc = web.Load(url);

            // Busca la etiqueta <table> de estadísticas dentro del DOM de la MLB, emplea la clase bui-table
            /* Esto lo hace con XPath: //table[contains(@class, 'bui-table')]:
               - table busca cualquier tabla.
               - [contains(@class, 'bui-table')] filtra por las que contienen esa clase.

            Resultado: nodo de la tabla con estadísticas principales de los equipos. */

            var table = doc.DocumentNode.SelectSingleNode("//table[contains(@class, 'bui-table')]");

            if (table == null)
            {
                Console.WriteLine("❌ No se encontró la tabla de estadísticas.");
                return;
            }

            // Extrae los encabezados de la tabla (primera fila) para mapear los nombres de las columnas a estadísticas.
            /* 
             - .//thead/tr navega al <thead> y a su única fila (<tr>).
             - .SelectNodes("th") extrae las celdas de encabezado (<th>). */

            var headerCells = table.SelectSingleNode(".//thead/tr").SelectNodes("th");

            if (headerCells == null)
            {
                Console.WriteLine("❌ No se encontraron encabezados.");
                return;
            }

            var headers = new List<string>();

            // Para cada encabezado, intenta leer el texto desde la etiqueta <span> (si existe), o directamente desde el <th> 
            for (int i = 0; i < headerCells.Count; i++)
            {
                var th = headerCells[i];
                var headerText = CleanHeader(th);
                headers.Add(headerText);
                Console.WriteLine($"Header {i}: {headerText}");

            }

            // Se buscan todas las filas <tr> dentro del body
            var rows = table.SelectNodes(".//tbody/tr");

            if (rows == null)
            {
                Console.WriteLine("❌ No se encontraron filas de datos.");
                return;
            }

            // Para cada fila...
            foreach (var row in rows)
            {
                // Aquí se intenta obtener el nombre limpio del <span> dentro del <a>. Si no hay <span>, se usa solo la primera línea del texto del <a>, lo cual elimina la repetición de los nombres.
                string teamName = row.SelectSingleNode(".//a/span")?.InnerText.Trim().Replace(" at ", " ") ?? row.SelectSingleNode(".//a")?.InnerText.Split('\n').First().Trim() ?? "UNKNOWN";

                if (teamName == "UNKNOWN")
                {
                    Console.WriteLine("⚠️ No se pudo extraer el nombre del equipo.");
                    continue;
                }

                // Se extraen las celdas para cada fila (sería el <td>)
                var cells = row.SelectNodes("td");

                if (cells == null || cells.Count < 3)
                {
                    Console.WriteLine($"⚠️ No se encontraron celdas válidas para {teamName}");
                    continue;
                }

                // Se valida si el equipo ya existe en la BD, caso contrario se crea y se guarda
                var team = _context.Teams.FirstOrDefault(t => t.Name == teamName);

                if (team == null)
                {
                    team = new Team { Name = teamName };
                    _context.Teams.Add(team);
                    await _context.SaveChangesAsync();
                }

                // Se busca la columna con el índice de "Games"
                var gamesIndex = headers.FindIndex(h => h == "G");

                if (gamesIndex >= 0 && gamesIndex < cells.Count - 1)
                {
                    var gamesRaw = cells[gamesIndex].InnerText.Trim();

                    // Se intenta convertir el valor a entero y asignarlo al equpo
                    if (int.TryParse(gamesRaw.Replace(",", ""), out int g))
                    {
                        team.Games = g;
                    }
                }

                // Lista de aspectos para los que se debe guardar el campo Total
                var allowedTotals = new HashSet<string>
                {
                    "AB", "R", "H", "HR", "2B", "3B", "RBI", "BB", "SO", "SB", "CS", "SF"
                };

                // Para cada estadística del encabezado (Runs, At bat...)
                for (int colIndex = 0; colIndex < headers.Count; colIndex++)
                {
                    string statTypeName = headers[colIndex];

                    if (statTypeName == "G") continue;

                    if (!allowedTotals.Contains(statTypeName)) continue;

                    if (colIndex >= cells.Count) continue;

                    var valueRaw = cells[colIndex].InnerText.Trim();

                    float? total = float.TryParse(valueRaw.Replace(",", ""), NumberStyles.Float, CultureInfo.InvariantCulture, out float val) ? val : null;

                    var statType = _context.StatTypes.FirstOrDefault(s => s.Name == statTypeName) ?? new StatType { Name = statTypeName };

                    if (statType.Id == 0)
                    {
                        _context.StatTypes.Add(statType);
                        await _context.SaveChangesAsync();
                    }

                    var stat = _context.TeamStats.FirstOrDefault(ts => ts.TeamId == team.Id && ts.StatTypeId == statType.Id);
                    if (stat == null)
                    {
                        stat = new TeamStat { TeamId = team.Id, StatTypeId = statType.Id };
                        _context.TeamStats.Add(stat);
                    }

                    stat.Total = total;
                }
            }

            // Guarda todo al final para evitar múltiples escrituras en la BD
            await _context.SaveChangesAsync();
        }

        public async Task ImportStatAsync(string statTypeName, string url)
        {

            // Descarga la página html y crea un documento con todo el contenido
            var response = await _httpClient.GetStringAsync(url);
            var doc = new HtmlDocument();
            doc.LoadHtml(response);

            // Busca la etiqueta <table> dentro del doc
            var table = doc.DocumentNode.SelectSingleNode("//table[contains(@class, 'datatable')]");

            // Obtiene la primera fila de la tabla que sería el encabezado
            var header = table.SelectSingleNode(".//thead/tr");

            // Obtiene las filas pero saltando el primero que sería el encabezado
            var rows = table.SelectNodes(".//tr").Skip(1);

            // Se busca si el tipo de estadística ya está en la BD, sino se crea
            var statType = _context.StatTypes.FirstOrDefault(s => s.Name == statTypeName);
            if (statType == null)
            {
                statType = new StatType { Name = statTypeName };
                _context.StatTypes.Add(statType);
                await _context.SaveChangesAsync();
            }

            foreach (var row in rows)
            {

                // Se obtiene cada celda de cada fila (sería el <td>)
                var cells = row.SelectNodes("td");

                // Como la tabla tiene Posición, Nombre Equipo y estadísticas (Current, Last 3...) si es menor a 8 se salta.
                if (cells == null || cells.Count < 8) continue;

                // Se obtiene el nombre del equipo y se normaliza para evitar abreviaciones o inconsistencias
                string rawTeamName = cells[1].InnerText.Trim();
                string teamName = TeamNameNormalizer.Normalize(rawTeamName);

                // Se valida si el equipo ya existe en la BD, caso contrario se crea y se guarda
                var team = _context.Teams.FirstOrDefault(t => t.Name == teamName);
                if (team == null)
                {
                    team = new Team { Name = teamName };
                    _context.Teams.Add(team);
                    await _context.SaveChangesAsync();
                }

                // Se busca si está el TeamStat en la BD, sino se crea
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

                // Se guarda cada valor en la propiedad correspondiente de TeamStat
                stat.CurrentSeason = Parse(cells[2].InnerText);
                stat.Last3Games = Parse(cells[3].InnerText);
                stat.LastGame = Parse(cells[4].InnerText);
                stat.Home = Parse(cells[5].InnerText);
                stat.Away = Parse(cells[6].InnerText);
                stat.PrevSeason = Parse(cells[7].InnerText);
            }

            // Guarda todo al final para evitar múltiples escrituras en la BD
            await _context.SaveChangesAsync();
        }

        // Método creado para conviertir el String a float (usado para parsear los datos al final)
        private static float? Parse(string input)
        {
            return float.TryParse(input, NumberStyles.Float, CultureInfo.InvariantCulture, out float val) ? val : null;
        }

        // Método para limpiar los encabezados de la tabla (headerText), eliminando nodos problemáticos y decorativos
        private string CleanHeader(HtmlNode th)
        {
            // Eliminamos nodos problemáticos como svg, iconos, etc.
            foreach (var node in th.SelectNodes(".//svg|.//i|.//icon") ?? Enumerable.Empty<HtmlNode>())
            {
                node.Remove();
            }

            // Extraemos el texto limpio
            var rawText = th.InnerText?.Trim();

            if (string.IsNullOrEmpty(rawText))
                return string.Empty;

            // Eliminamos duplicaciones (como TEAMTEAM)
            // Si un texto tiene exactamente el mismo contenido duplicado, lo cortamos a la mitad.
            if (rawText.Length % 2 == 0)
            {
                var halfLength = rawText.Length / 2;
                var firstHalf = rawText.Substring(0, halfLength);
                var secondHalf = rawText.Substring(halfLength);

                if (firstHalf == secondHalf)
                    return firstHalf;
            }

            // Eliminamos textos decorativos
            rawText = rawText.Replace("caret-up", "").Replace("caret-down", "").Trim();

            return rawText;
        }

    }
}
