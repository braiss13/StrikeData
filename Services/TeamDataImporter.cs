using HtmlAgilityPack;
using StrikeData.Data;
using StrikeData.Models;
using System.Globalization;
using Newtonsoft.Json.Linq;

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
                { "CS", "https://www.teamrankings.com/mlb/stat/caught-stealing-per-game" },
                { "SAC", "https://www.teamrankings.com/mlb/stat/sacrifice-hits-per-game" },
                { "SF", "https://www.teamrankings.com/mlb/stat/sacrifice-flys-per-game" },
                { "HBP", "https://www.teamrankings.com/mlb/stat/hit-by-pitch-per-game" },
                { "GIDP", "https://www.teamrankings.com/mlb/stat/grounded-into-double-plays-per-game" },
                { "TB", "https://www.teamrankings.com/mlb/stat/total-bases-per-game" },
                { "AVG", "https://www.teamrankings.com/mlb/stat/batting-average" },
                { "SLG", "https://www.teamrankings.com/mlb/stat/slugging-pct" },
                { "OBP", "https://www.teamrankings.com/mlb/stat/on-base-pct" },
                { "OPS", "https://www.teamrankings.com/mlb/stat/on-base-plus-slugging-pct" }
            };

            //1. TeamRanking -> Va recorriendo el diccionario y llama al método para cada estadística por separado, indicando la estadística y la URL a consultar
            foreach (var stat in stats)
            {
                await ImportStatAsync(stat.Key, stat.Value);
            }

            // 2. Una vez lo tengamos, se scrapea la página de la MLB para obtener TOTAL y GAMES
            await ImportStatsFromMLB();
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

        private async Task ImportStatsFromMLB()
        {
            var statsArray = await FetchExpandedTeamStatsAsync();

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
                { "caughtStealing", "CS" },
                { "sacBunts", "SAC" },
                { "sacFlies", "SF" },
                { "totalBases", "TB" },
                { "hitByPitch", "HBP" },
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

        private async Task<JArray> FetchExpandedTeamStatsAsync()
        {

            var url = "https://bdfed.stitch.mlbinfra.com/bdfed/stats/team?stitch_env=prod&sportId=1&gameType=R&group=hitting&stats=season&season=2025&limit=30&offset=0";

            var response = await _httpClient.GetStringAsync(url);
            var json = JObject.Parse(response);
            var stats = (JArray)json["stats"];

            Console.WriteLine($"📊 Estadísticas obtenidas: {stats} ");

            if (stats == null || !stats.Any())
            {
                Console.WriteLine("❌ No se encontraron estadísticas.");
                return new JArray();
            }

            return stats;
        }


    }
}
