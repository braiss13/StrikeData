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

            var statColumnMap = new Dictionary<string, string>
            {
                { "R", "Runs" },
                { "AB", "At Bat" },
                { "H", "Hits" },
                { "HR", "Home Runs" },
                { "2B", "Doubles" },
                { "3B", "Triples" },
                { "RBI", "RBIs" },
                { "BB", "Walks / Base on Ball" },
                { "SO", "Strikeouts (SO)" },
                { "SB", "Stolen Bases (SB)" },
                { "CS", "Caught Stealing" },
                { "SF", "Sacrifice Flys" },
                { "HBP", "Hit by pitch" },
                { "GIDP", "Grounded into Double Plays" },
                { "TB", "Total Bases" }
            };

            var table = doc.DocumentNode.SelectSingleNode("//table[contains(@class, 'bui-table')]");
            if (table == null) return;

            // Mapear los encabezados a índices
            var headerCells = table.SelectSingleNode(".//thead/tr").SelectNodes("th");
            var headerMap = new Dictionary<string, int>();

            for (int i = 0; i < headerCells.Count; i++)
            {
                string colName = headerCells[i].InnerText.Trim();
                if (colName == "Team" || colName == "G" || statColumnMap.ContainsKey(colName))
                {
                    headerMap[colName] = i;
                }
            }

            // Validar existencia de columnas clave
            if (!headerMap.ContainsKey("Team") || !headerMap.ContainsKey("G")) return;

            var rows = table.SelectNodes(".//tbody/tr");

            foreach (var row in rows)
            {
                var cells = row.SelectNodes("td");
                if (cells == null || cells.Count <= headerMap["G"]) continue;

                string teamName = cells[headerMap["Team"]].InnerText.Trim();
                string gamesRaw = cells[headerMap["G"]].InnerText.Trim();

                // Normalizar nombre si fuera necesario (opcional)
                teamName = TeamNameNormalizer.Normalize(teamName); // si has creado esta clase

                var team = _context.Teams.FirstOrDefault(t => t.Name == teamName);
                if (team == null)
                {
                    team = new Team { Name = teamName };
                    _context.Teams.Add(team);
                    await _context.SaveChangesAsync();
                }

                if (int.TryParse(gamesRaw.Replace(",", ""), out int parsedGames))
                {
                    team.Games = parsedGames;
                    Console.WriteLine($"✅ {teamName} => Games: {parsedGames}");
                }
                else
                {
                    Console.WriteLine($"❌ Error al parsear Games para {teamName}. Raw: {gamesRaw}");
                }

                // Guardar Total stats
                foreach (var statKvp in statColumnMap)
                {
                    if (!headerMap.ContainsKey(statKvp.Key)) continue;

                    int statIndex = headerMap[statKvp.Key];
                    if (statIndex >= cells.Count) continue;

                    string statRaw = cells[statIndex].InnerText.Trim();
                    float? statValue = float.TryParse(statRaw.Replace(",", ""), NumberStyles.Float, CultureInfo.InvariantCulture, out float val) ? val : null;

                    var statTypeName = statKvp.Value;

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

                    stat.Total = statValue;
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
