using Microsoft.AspNetCore.Mvc.RazorPages;
using StrikeData.Services;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StrikeData.Pages.TeamData
{
    public class StatsModel : PageModel
    {
        private readonly TeamRankingScraper _scraper;

        public List<List<string>> RunsData { get; set; } = new List<List<string>>();
        public List<List<string>> HitsData { get; set; } = new List<List<string>>();

        public StatsModel()
        {
            _scraper = new TeamRankingScraper();
        }

        public async Task OnGetAsync()
        {
            string runsUrl = "https://www.teamrankings.com/mlb/stat/runs-per-game";
            string hitsUrl = "https://www.teamrankings.com/mlb/stat/hits-per-game";

            RunsData = await _scraper.ScrapeTable(runsUrl);
            HitsData = await _scraper.ScrapeTable(hitsUrl);
        }
    }
}
