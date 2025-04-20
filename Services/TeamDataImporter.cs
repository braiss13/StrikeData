using HtmlAgilityPack;
using StrikeData.Data;
using StrikeData.Models;
using System.Globalization;
using System.Net.Http;
using System.Threading.Tasks;
using System.Linq;

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

        public async Task ImportWinTrendsAsync()
        {
            /*
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
            */
        }

          public async Task ImportAllStatsAsync()
        {
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

        public async Task ImportStatAsync(string statTypeName, string url)
        {
            var rows = await ScrapeTable(url);

            var statType = _context.StatTypes.FirstOrDefault(s => s.Name == statTypeName);
            if (statType == null)
            {
                statType = new StatType { Name = statTypeName };
                _context.StatTypes.Add(statType);
                await _context.SaveChangesAsync();
            }

            foreach (var row in rows)
            {
                if (row.Count < 8) continue;

                string teamName = row[1];
                var team = _context.Teams.FirstOrDefault(t => t.Name == teamName);
                if (team == null)
                {
                    team = new Team { Name = teamName };
                    _context.Teams.Add(team);
                    await _context.SaveChangesAsync();
                }

                float? ParseFloat(string val)
                {
                    return float.TryParse(val, NumberStyles.Float, CultureInfo.InvariantCulture, out float result)
                        ? result : null;
                }

                var existing = _context.TeamStats.FirstOrDefault(ts =>
                    ts.TeamId == team.Id &&
                    ts.StatTypeId == statType.Id &&
                    ts.CurrentSeason == 2025);

                if (existing == null)
                {
                    existing = new TeamStat
                    {
                        TeamId = team.Id,
                        StatTypeId = statType.Id,
                        CurrentSeason = 2025
                    };
                    _context.TeamStats.Add(existing);
                }

                existing.Last3Games = ParseFloat(row[3]);
                existing.LastGame = ParseFloat(row[4]);
                existing.Home = ParseFloat(row[5]);
                existing.Away = ParseFloat(row[6]);
                existing.PrevSeason = ParseFloat(row[7]);
            }

            await _context.SaveChangesAsync();
        }

        private async Task<List<List<string>>> ScrapeTable(string url)
        {
            var response = await _httpClient.GetStringAsync(url);
            var doc = new HtmlDocument();
            doc.LoadHtml(response);

            var table = doc.DocumentNode.SelectSingleNode("//table[contains(@class, 'datatable')]");
            var rows = table.SelectNodes(".//tr");

            var result = new List<List<string>>();
            foreach (var row in rows)
            {
                var cells = row.SelectNodes(".//td");
                if (cells == null) continue;

                result.Add(cells.Select(c => c.InnerText.Trim()).ToList());
            }

            return result;
        }
    }
}
