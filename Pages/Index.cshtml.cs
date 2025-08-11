using Microsoft.AspNetCore.Mvc.RazorPages;
using StrikeData.Services;
using System.Threading.Tasks;
using StrikeData.Data;
using StrikeData.Services.TeamData;

namespace StrikeData.Pages
{
    public class IndexModel : PageModel
    {
        private readonly MainImporter _main_importer;

        public IndexModel(AppDbContext context)
        {
            _main_importer = new MainImporter(context);
        }

        public async Task OnGetAsync()
        {   
            // await _importer.ImportWinTrendsAsync();
            await _main_importer.ImportAllStatsAsync();
        }
    }
}
