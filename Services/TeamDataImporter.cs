using HtmlAgilityPack;
using StrikeData.Data;
using StrikeData.Models;
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

        public async Task ImportRunsPerGameAsync()
        {
            var url = "https://www.teamrankings.com/mlb/stat/runs-per-game";
            var rows = await ScrapeTable(url);

            foreach (var row in rows)
            {
                if (row.Count < 3) continue;

                string name = row[1];
                if (!float.TryParse(row[2], out float runs)) continue;

                var team = _context.Teams.FirstOrDefault(t => t.Name == name);
                if (team == null)
                {
                    team = new Team { Name = name, SeasonYear = 2025 };
                    _context.Teams.Add(team);
                }

                team.RunsPerGame = runs;
            }

            await _context.SaveChangesAsync();
        }

        public async Task ImportHitsPerGameAsync()
        {
            var url = "https://www.teamrankings.com/mlb/stat/hits-per-game";
            var rows = await ScrapeTable(url);

            foreach (var row in rows)
            {
                if (row.Count < 3) continue;

                string name = row[1];
                if (!float.TryParse(row[2], out float hits)) continue;

                var team = _context.Teams.FirstOrDefault(t => t.Name == name);
                if (team == null)
                {
                    team = new Team { Name = name, SeasonYear = 2025 };
                    _context.Teams.Add(team);
                }

                team.HitsPerGame = hits;
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
