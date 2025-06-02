using HtmlAgilityPack;
using StrikeData.Data;
using StrikeData.Models;
using System.Globalization;
using System.Net.Http;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;

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

        public async Task ImportAllStatsAsync()
        {

            // 1. Primero, scrapea la página de la MLB para obtener TOTAL y GAMES
            await ImportStatsFromMLBAsync();

            // 2. Luego, el scraping de TeamRankings donde se obtienen los promedios por partido de cada aspecto
            var stats = new Dictionary<string, string>
            {
                { "Runs", "https://www.teamrankings.com/mlb/stat/runs-per-game" },
                { "At Bat", "https://www.teamrankings.com/mlb/stat/at-bats-per-game" },
                { "Hits", "https://www.teamrankings.com/mlb/stat/hits-per-game" },
                { "Home Runs", "https://www.teamrankings.com/mlb/stat/home-runs-per-game" },
                { "Singles", "https://www.teamrankings.com/mlb/stat/singles-per-game" },
                { "Doubles", "https://www.teamrankings.com/mlb/stat/doubles-per-game" },
                { "Triples", "https://www.teamrankings.com/mlb/stat/triples-per-game" },
                { "RBIs", "https://www.teamrankings.com/mlb/stat/rbis-per-game" },
                { "Walks / Base on Ball", "https://www.teamrankings.com/mlb/stat/walks-per-game" },
                { "Strikeouts (SO)", "https://www.teamrankings.com/mlb/stat/strikeouts-per-game" },
                { "Stolen Bases (SB)", "https://www.teamrankings.com/mlb/stat/stolen-bases-per-game" },
                { "Caught Stealing", "https://www.teamrankings.com/mlb/stat/caught-stealing-per-game" },
                { "Sacrifice Hits", "https://www.teamrankings.com/mlb/stat/sacrifice-hits-per-game" },
                { "Sacrifice Flys", "https://www.teamrankings.com/mlb/stat/sacrifice-flys-per-game" },
                { "Hit by Pitch", "https://www.teamrankings.com/mlb/stat/hit-by-pitch-per-game" },
                { "Grounded into Double Plays", "https://www.teamrankings.com/mlb/stat/grounded-into-double-plays-per-game" },
                { "Total Bases", "https://www.teamrankings.com/mlb/stat/total-bases-per-game" },
                { "Batting Average (en %)", "https://www.teamrankings.com/mlb/stat/batting-average" },
                { "Slugging (en %)", "https://www.teamrankings.com/mlb/stat/slugging-pct" },
                { "On Base (en %)", "https://www.teamrankings.com/mlb/stat/on-base-pct" },
                { "On Base plus Slugging (en %)", "https://www.teamrankings.com/mlb/stat/on-base-plus-slugging-pct" }
            };

            foreach (var stat in stats)
            {
                await ImportStatAsync(stat.Key, stat.Value);
            }
        }

        public async Task ImportStatsFromMLBAsync()
        {
            var url = "https://www.mlb.com/stats/team";
            var web = new HtmlWeb();
            var doc = web.Load(url);

            var table = doc.DocumentNode.SelectSingleNode("//table[contains(@class, 'bui-table')]");
            if (table == null)
            {
                Console.WriteLine("❌ No se encontró la tabla de estadísticas.");
                return;
            }

            var headerCells = table.SelectSingleNode(".//thead/tr").SelectNodes("th");
            if (headerCells == null)
            {
                Console.WriteLine("❌ No se encontraron encabezados.");
                return;
            }

            var columnIndexToStatName = new Dictionary<int, string>();
            for (int i = 0; i < headerCells.Count; i++)
            {
                var th = headerCells[i];
                var span = th.SelectSingleNode(".//span");
                string headerText = (span != null ? span.InnerText : th.InnerText).Trim();

                switch (headerText)
                {
                    case "G": columnIndexToStatName[i] = "Games"; break;
                    case "AB": columnIndexToStatName[i] = "At Bat"; break;
                    case "R": columnIndexToStatName[i] = "Runs"; break;
                    case "H": columnIndexToStatName[i] = "Hits"; break;
                    case "HR": columnIndexToStatName[i] = "Home Runs"; break;
                    case "2B": columnIndexToStatName[i] = "Doubles"; break;
                    case "3B": columnIndexToStatName[i] = "Triples"; break;
                    case "RBI": columnIndexToStatName[i] = "RBIs"; break;
                    case "BB": columnIndexToStatName[i] = "Walks / Base on Ball"; break;
                    case "SO": columnIndexToStatName[i] = "Strikeouts (SO)"; break;
                    case "SB": columnIndexToStatName[i] = "Stolen Bases (SB)"; break;
                    case "CS": columnIndexToStatName[i] = "Caught Stealing"; break;
                    case "SF": columnIndexToStatName[i] = "Sacrifice Flys"; break;
                    /* case "HBP": columnIndexToStatName[i] = "Hit by pitch"; break;
                    case "GIDP": columnIndexToStatName[i] = "Grounded into Double Plays"; break;
                    case "TB": columnIndexToStatName[i] = "Total Bases"; break; */
                }
            }

            var rows = table.SelectNodes(".//tbody/tr");
            if (rows == null)
            {
                Console.WriteLine("❌ No se encontraron filas de datos.");
                return;
            }

            foreach (var row in rows)
            {
                var teamLink = row.SelectSingleNode(".//a");
                string teamName = teamLink?.InnerText.Trim() ?? "UNKNOWN";
                if (teamName == "UNKNOWN")
                {
                    Console.WriteLine("⚠️ No se pudo extraer el nombre del equipo.");
                    continue;
                }

                var cells = row.SelectNodes("td");
                if (cells == null || cells.Count < 3)
                {
                    Console.WriteLine($"⚠️ No se encontraron celdas válidas para {teamName}");
                    continue;
                }

                var team = _context.Teams.FirstOrDefault(t => t.Name == teamName);
                if (team == null)
                {
                    team = new Team { Name = teamName };
                    _context.Teams.Add(team);
                    await _context.SaveChangesAsync();
                }

                var gamesIndex = columnIndexToStatName.FirstOrDefault(kv => kv.Value == "Games").Key;
                if (gamesIndex >= 0 && gamesIndex < cells.Count)
                {
                    var gamesRaw = cells[gamesIndex+1].InnerText.Trim();
                    Console.WriteLine($"📊 Procesando equipo: {teamName} | Games (raw): {gamesRaw}");

                    if (int.TryParse(gamesRaw.Replace(",", ""), out int g))
                    {
                        team.Games = g;
                        Console.WriteLine($"✅ Games parseado: {g}");
                    }
                    else
                    {
                        Console.WriteLine($"⚠️ Error al parsear games para {teamName}");
                    }
                }

                foreach (var kv in columnIndexToStatName)
                {
                    int colIndex = kv.Key;
                    string statTypeName = kv.Value;

                    if (statTypeName == "Games") continue;
                    if (colIndex >= cells.Count) continue;

                    var valueRaw = cells[colIndex].InnerText.Trim();
                    float? total = float.TryParse(valueRaw.Replace(",", ""), NumberStyles.Float, CultureInfo.InvariantCulture, out float val) ? val : null;

                    var statType = _context.StatTypes.FirstOrDefault(s => s.Name == statTypeName);
                    if (statType == null)
                    {
                        statType = new StatType { Name = statTypeName };
                        _context.StatTypes.Add(statType);
                        await _context.SaveChangesAsync();
                    }

                    var stat = _context.TeamStats.FirstOrDefault(ts => ts.TeamId == team.Id && ts.StatTypeId == statType.Id);
                    if (stat == null)
                    {
                        stat = new TeamStat
                        {
                            TeamId = team.Id,
                            StatTypeId = statType.Id,
                            CurrentSeason = DateTime.Now.Year
                        };
                        _context.TeamStats.Add(stat);
                    }

                    stat.Total = total;
                }
            }

            await _context.SaveChangesAsync();
        }

        public async Task ImportStatAsync(string statTypeName, string url)
        {
            var response = await _httpClient.GetStringAsync(url);
            var doc = new HtmlDocument();
            doc.LoadHtml(response);

            var table = doc.DocumentNode.SelectSingleNode("//table[contains(@class, 'datatable')]");

            var header = table.SelectSingleNode(".//thead/tr");

            var rows = table.SelectNodes(".//tr").Skip(1); // skip header

            var statType = _context.StatTypes.FirstOrDefault(s => s.Name == statTypeName);
            if (statType == null)
            {
                statType = new StatType { Name = statTypeName };
                _context.StatTypes.Add(statType);
                await _context.SaveChangesAsync();
            }

            foreach (var row in rows)
            {
                var cells = row.SelectNodes("td");
                if (cells == null || cells.Count < 8) continue;

                // Se obtiene el nombre del equipo
                string rawTeamName = cells[1].InnerText.Trim();
                string teamName = TeamNameNormalizer.Normalize(rawTeamName);

                var team = _context.Teams.FirstOrDefault(t => t.Name == teamName);
                if (team == null)
                {
                    team = new Team { Name = teamName };
                    _context.Teams.Add(team);
                    await _context.SaveChangesAsync();
                }

                float? Parse(string input) =>
                    float.TryParse(input, NumberStyles.Float, CultureInfo.InvariantCulture, out float val) ? val : null;

                var stat = _context.TeamStats.FirstOrDefault(ts =>
                    ts.TeamId == team.Id &&
                    ts.StatTypeId == statType.Id);

                if (stat == null)
                {
                    stat = new TeamStat
                    {
                        TeamId = team.Id,
                        StatTypeId = statType.Id
                    };
                    _context.TeamStats.Add(stat);
                }

                stat.CurrentSeason = Parse(cells[2].InnerText);
                stat.Last3Games = Parse(cells[3].InnerText);
                stat.LastGame = Parse(cells[4].InnerText);
                stat.Home = Parse(cells[5].InnerText);
                stat.Away = Parse(cells[6].InnerText);
                stat.PrevSeason = Parse(cells[7].InnerText);
            }

            await _context.SaveChangesAsync();
        }
    }
}
