using Microsoft.AspNetCore.Mvc.RazorPages;
using StrikeData.Services;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StrikeData.Pages.TeamData
{
    public class WinTrendsModel : PageModel
    {
        private readonly TeamRankingScraper _scraper;

        public List<List<string>> HomeData { get; set; } = new List<List<string>>();
        public List<List<string>> AwayData { get; set; } = new List<List<string>>();
        public List<List<string>> WinTrendsData { get; set; } = new List<List<string>>();

        public WinTrendsModel()
        {
            _scraper = new TeamRankingScraper();
        }

        public async Task OnGetAsync()
        {
            string homeUrl = "https://www.teamrankings.com/mlb/trends/win_trends/?sc=is_home";
            string awayUrl = "https://www.teamrankings.com/mlb/trends/win_trends/?sc=is_away";
            string overallUrl = "https://www.teamrankings.com/mlb/trends/win_trends/";

            HomeData = await _scraper.ScrapeTable(homeUrl);
            AwayData = await _scraper.ScrapeTable(awayUrl);
            WinTrendsData = await _scraper.ScrapeTable(overallUrl);
        }
    }
}
