using Microsoft.AspNetCore.Mvc.RazorPages;
using StrikeData.Services;
using System.Threading.Tasks;
using StrikeData.Data;
using StrikeData.Services.TeamData;
using System.Net.Http;

namespace StrikeData.Pages
{
    public class IndexModel : PageModel
    {
        private readonly HittingImporter _hitting_importer;
        private readonly PitchingImporter _pitching_importer;

        public IndexModel(AppDbContext context)
        {
            _hitting_importer = new HittingImporter(context);
            // Para PitchingImporter necesitamos un HttpClient adicional
            _pitching_importer = new PitchingImporter(context, new HttpClient());
        }

        public async Task OnGetAsync()
        {
            // TODO: Revisar lo de WinTrends -> await _importer.ImportWinTrendsAsync();
            await _hitting_importer.ImportAllStatsAsync();
            await _pitching_importer.ImportAllStatsAsync();
        }
    }
}
