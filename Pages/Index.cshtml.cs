using Microsoft.AspNetCore.Mvc.RazorPages;
using StrikeData.Services;
using System.Threading.Tasks;
using StrikeData.Data;
using StrikeData.Services.TeamData;

namespace StrikeData.Pages
{
    public class IndexModel : PageModel
    {
        private readonly HittingImporter _hitting_importer;

        public IndexModel(AppDbContext context)
        {
            _hitting_importer = new HittingImporter(context);
        }

        public async Task OnGetAsync()
        {   
            // TODO: Revisar lo de WinTrends -> await _importer.ImportWinTrendsAsync();
            await _hitting_importer.ImportAllStatsAsync();
        }
    }
}
