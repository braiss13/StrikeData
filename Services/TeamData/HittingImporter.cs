using HtmlAgilityPack;
using StrikeData.Data;
using StrikeData.Models;
using System.Globalization;
using Newtonsoft.Json.Linq;
using Microsoft.EntityFrameworkCore;

namespace StrikeData.Services.TeamData
{
    public class HittingImporter
    {
        // Este importador contiene los métodos para importar las estadísticas relativas a equipos en cuanto a bateo
        private readonly AppDbContext _context;
        private readonly HttpClient _httpClient;

        public HittingImporter(AppDbContext context)
        {
            _context = context;
            _httpClient = new HttpClient();
        }

        private async Task<int> GetHittingCategoryIdAsync()
        {
            var category = _context.StatCategories.FirstOrDefault(c => c.Name == "Hitting");
            if (category == null)
            {
                category = new StatCategory { Name = "Hitting" };
                _context.StatCategories.Add(category);
                await _context.SaveChangesAsync();
            }
            return category.Id;
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

        // Método principal -> Contiene las llamadas a los dos métodos de obtención de datos (MLB y TeamRankings)
        public async Task ImportAllStatsAsync()
        {
            // 1. Primero scrapear la MLB para tener el número de Games por equipo y el valor total para cada tipo de estadística
            await ImportTeamStatsMLB();

            // 2. Luego, el scraping de TeamRankings con promedios por partido
            var stats = new Dictionary<string, string>
            {
                { "AB", "https://www.teamrankings.com/mlb/stat/at-bats-per-game" },
                { "R", "https://www.teamrankings.com/mlb/stat/runs-per-game" },
                { "H", "https://www.teamrankings.com/mlb/stat/hits-per-game" },
                { "HR", "https://www.teamrankings.com/mlb/stat/home-runs-per-game" },
                { "S", "https://www.teamrankings.com/mlb/stat/singles-per-game" },
                { "2B", "https://www.teamrankings.com/mlb/stat/doubles-per-game" },
                { "3B", "https://www.teamrankings.com/mlb/stat/triples-per-game" },
                { "RBI", "https://www.teamrankings.com/mlb/stat/rbis-per-game" },
                { "BB", "https://www.teamrankings.com/mlb/stat/walks-per-game" },
                { "SO", "https://www.teamrankings.com/mlb/stat/strikeouts-per-game" },
                { "SB", "https://www.teamrankings.com/mlb/stat/stolen-bases-per-game" },
                { "SBA", "https://www.teamrankings.com/mlb/stat/stolen-bases-attempted-per-game" },
                { "CS", "https://www.teamrankings.com/mlb/stat/caught-stealing-per-game" },
                { "SAC", "https://www.teamrankings.com/mlb/stat/sacrifice-hits-per-game" },
                { "SF", "https://www.teamrankings.com/mlb/stat/sacrifice-flys-per-game" },
                { "LOB", "https://www.teamrankings.com/mlb/stat/left-on-base-per-game" },
                { "TLOB", "https://www.teamrankings.com/mlb/stat/team-left-on-base-per-game" },
                { "HBP", "https://www.teamrankings.com/mlb/stat/hit-by-pitch-per-game" },
                { "GIDP", "https://www.teamrankings.com/mlb/stat/grounded-into-double-plays-per-game" },
                { "RLSP", "https://www.teamrankings.com/mlb/stat/runners-left-in-scoring-position-per-game" },
                { "TB", "https://www.teamrankings.com/mlb/stat/total-bases-per-game" },
                { "AVG", "https://www.teamrankings.com/mlb/stat/batting-average" },
                { "SLG", "https://www.teamrankings.com/mlb/stat/slugging-pct" },
                { "OBP", "https://www.teamrankings.com/mlb/stat/on-base-pct" },
                { "OPS", "https://www.teamrankings.com/mlb/stat/on-base-plus-slugging-pct" },
                { "AB/HR", "https://www.teamrankings.com/mlb/stat/at-bats-per-home-run" }
            };

            foreach (var stat in stats)
            {
                await ImportTeamStatsTR(stat.Key, stat.Value);
            }
        }

        // Método que realiza scrapping para obtener estadísticas de TeamRankings
        public async Task ImportTeamStatsTR(string statTypeName, string url)
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

                // Obtener o crear la categoría Hitting y recuperar su Id
                int categoryId = await GetHittingCategoryIdAsync();

                // Crear el nuevo tipo de estadística asociándolo a esa categoría
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

                CalculateTotal(statTypeName, team, stat);

            }

            // Guarda todo al final para evitar múltiples escrituras en la BD
            await _context.SaveChangesAsync();
        }

        private static void CalculateTotal(string statTypeName, Team team, TeamStat stat)
        {
            // Para los campos que no están en la página de la MLB, se calcula el "Total" como Games * CurrentSeason
            if (statTypeName == "S" || statTypeName == "SBA" || statTypeName == "LOB" || statTypeName == "TLOB" || statTypeName == "RLSP")
            {
                if (team.Games < 1)
                {
                    Console.WriteLine($"⚠️ El equipo {team.Name} no tiene juegos registrados. La operación será inválida.");
                }
                else if (stat.CurrentSeason.HasValue)
                {
                    // Aquí se obtiene el valor de currentSeason como float
                    float currentSeasonValue = stat.CurrentSeason ?? 0;

                    // Como la librería Math.Round no acepta float, se convierte a double el valor final (en .net no hay sobrecarga de Math.Round para float)
                    double rawTotal = (double)(team.Games * currentSeasonValue);

                    // Por último, se redondea a 2 decimales y se asigna al Total en formato float, puesto que es el que hay en la BD
                    stat.Total = (float)Math.Round(rawTotal, 2);
                }
                else
                {
                    Console.WriteLine($"⚠️ El valor de CurrentSeason es null para el equipo {team.Name} en la estadística 'S' (Singles). La operación será inválida.");
                }

            }

        }

        // Método creado para convertir el String a float (empleado para parsear los datos al final)
        private static float? Parse(string input)
        {
            return float.TryParse(input, NumberStyles.Float, CultureInfo.InvariantCulture, out float val) ? val : null;
        }

        // Método que importa las estadísticas de la página oficial de la MLB y las trata para guardarlas en la BD
        private async Task ImportTeamStatsMLB()
        {
            var statsArray = await FetchTeamStatsMLB();
            var hittingCategoryId = await GetHittingCategoryIdAsync();

            // Mapeo de nombres de la API (extendidos) a abreviaturas deseadas
            var statMappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "gamesPlayed", "G" },
                { "atBats", "AB" },
                { "runs", "R" },
                { "hits", "H" },
                { "homeRuns", "HR" },
                { "doubles", "2B" },
                { "triples", "3B" },
                { "rbi", "RBI" },
                { "baseOnBalls", "BB" },
                { "strikeOuts", "SO" },
                { "stolenBases", "SB" },
                { "groundIntoDoublePlay", "GIDP" },
                { "caughtStealing", "CS" },
                { "sacBunts", "SAC" },
                { "sacFlies", "SF" },
                { "totalBases", "TB" },
                { "hitByPitch", "HBP" },
                { "atBatsPerHomeRun", "AB/HR" }

            };

            foreach (var statToken in statsArray)
            {
                if (statToken is not JObject teamStat)
                    continue;

                var teamNameRaw = teamStat["teamName"]?.ToString();
                if (string.IsNullOrWhiteSpace(teamNameRaw))
                {
                    Console.WriteLine("⚠️ Nombre de equipo no encontrado o vacío.");
                    continue;
                }

                var teamName = TeamNameNormalizer.Normalize(teamNameRaw);

                var team = _context.Teams.FirstOrDefault(t => t.Name == teamName);
                if (team == null)
                {
                    team = new Team { Name = teamName };
                    _context.Teams.Add(team);
                    await _context.SaveChangesAsync();
                }

                foreach (var mapping in statMappings)
                {
                    string apiField = mapping.Key;
                    string shortName = mapping.Value;

                    if (!teamStat.TryGetValue(apiField, out var token))
                        continue;

                    string rawValue = token?.ToString();

                    if (string.IsNullOrWhiteSpace(rawValue))
                        continue;

                    if (shortName == "G")
                    {
                        if (int.TryParse(rawValue, out int games))
                        {
                            team.Games = games;
                        }
                        continue;
                    }

                    if (!float.TryParse(rawValue, NumberStyles.Float, CultureInfo.InvariantCulture, out float statValue))
                    {
                        Console.WriteLine($"⚠️ Valor inválido para {shortName} en {teamName}: '{rawValue}'");
                        continue;
                    }

                    // Obtener o crear el tipo de estadística
                    var statType = _context.StatTypes.FirstOrDefault(s => s.Name == shortName);
                    if (statType == null)
                    {
                        statType = new StatType { Name = shortName };
                        statType = new StatType { Name = shortName, StatCategoryId = hittingCategoryId };
                        _context.StatTypes.Add(statType);
                        await _context.SaveChangesAsync();
                    }

                    // Obtener o crear la estadística del equipo
                    var stat = _context.TeamStats.FirstOrDefault(ts => ts.TeamId == team.Id && ts.StatTypeId == statType.Id);
                    if (stat == null)
                    {
                        stat = new TeamStat { TeamId = team.Id, StatTypeId = statType.Id };
                        _context.TeamStats.Add(stat);
                    }

                    stat.Total = statValue;
                }
            }

            await _context.SaveChangesAsync();
        }

        // Método que realiza la llamada a la API de la MLB para obtener las estadísticas 
        private async Task<JArray> FetchTeamStatsMLB()
        {

            // Esta URL es la que utiliza la página oficial de la MLB para obtener las estadísticas de los equipos (obtenida al hacer un análisis de red)
            var url = "https://bdfed.stitch.mlbinfra.com/bdfed/stats/team?stitch_env=prod&sportId=1&gameType=R&group=hitting&stats=season&season=2025&limit=30&offset=0";

            var response = await _httpClient.GetStringAsync(url);
            var json = JObject.Parse(response);
            var stats = (JArray)json["stats"];

            if (stats == null || !stats.Any())
            {
                Console.WriteLine("❌ No se encontraron estadísticas.");
                return [];
            }

            return stats;
        }
        
    }
}
