using Microsoft.AspNetCore.Mvc.RazorPages;
using StrikeData.Services;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StrikeData.Pages
{
    public class TeamdataModel : PageModel
    {
        private readonly TeamRankingScraper _scraper;

        public List<List<string>> HomeData { get; set; } = new List<List<string>>();
        public List<List<string>> AwayData { get; set; } = new List<List<string>>();

        public TeamdataModel()
        {
            _scraper = new TeamRankingScraper();
        }

        public async Task OnGetAsync()
        {
            string homeUrl = "https://www.teamrankings.com/mlb/trends/win_trends/?sc=is_home";
            string awayUrl = "https://www.teamrankings.com/mlb/trends/win_trends/?sc=is_away";

            HomeData = await _scraper.ScrapeTable(homeUrl);
            AwayData = await _scraper.ScrapeTable(awayUrl);
        }
    }
}
