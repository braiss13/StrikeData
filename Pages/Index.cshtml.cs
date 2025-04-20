using Microsoft.AspNetCore.Mvc.RazorPages;
using StrikeData.Services;
using System.Threading.Tasks;
using StrikeData.Data;

namespace StrikeData.Pages
{
    public class IndexModel : PageModel
    {
        private readonly TeamDataImporter _importer;

        public IndexModel(AppDbContext context)
        {
            //_importer = new TeamDataImporter(context);
        }

        public async Task OnGetAsync()
        {
            await _importer.ImportWinTrendsAsync();
            await _importer.ImportRunsPerGameAsync();
            await _importer.ImportHitsPerGameAsync();
        }
    }
}
