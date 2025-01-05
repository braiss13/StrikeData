using Microsoft.AspNetCore.Mvc.RazorPages;
using StrikeData.Services;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StrikeData.Pages
{
    public class IndexModel : PageModel
    {
        private readonly TeamRankingScraper _scraper;

        // Propiedades para almacenar los datos de las tablas
        public List<List<string>> HomeData { get; set; } = new List<List<string>>();
        public List<List<string>> AwayData { get; set; } = new List<List<string>>();

        public IndexModel()
        {
            _scraper = new TeamRankingScraper();
        }

        public async Task OnGetAsync()
        {
            string homeUrl = "https://www.teamrankings.com/mlb/trends/win_trends/?sc=is_home";
            string awayUrl = "https://www.teamrankings.com/mlb/trends/win_trends/?sc=is_away";

            // Obtener datos de ambas tablas
            HomeData = await _scraper.ScrapeTable(homeUrl);
            AwayData = await _scraper.ScrapeTable(awayUrl);
        }
    }
}
