using Microsoft.AspNetCore.Mvc.RazorPages;
using StrikeData.Data;
using StrikeData.Services.TeamData;

namespace StrikeData.Pages
{
    public class IndexModel : PageModel
    {
        private readonly HittingImporter _hitting_importer;
        private readonly PitchingImporter _pitching_importer;
        private readonly FieldingImporter _fielding_importer;

        public IndexModel(AppDbContext context)
        {
            _hitting_importer = new HittingImporter(context);

            // Para PitchingImporter necesitamos un HttpClient adicional
            _pitching_importer = new PitchingImporter(context);

            _fielding_importer = new FieldingImporter(context);
        }

        public async Task OnGetAsync()
        {
            await _hitting_importer.ImportAllStatsAsyncH();
            await _pitching_importer.ImportAllStatsAsyncP();
            await _fielding_importer.ImportAllStatsAsyncF();
        }
    }
}
