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
            _importer = new TeamDataImporter(context);
        }

        public async Task OnGetAsync()
        {   
            // TODO: Descomentar estas líneas para importar los datos del scrapping, ahora mismo están comentadas para evitar que se ejecute cada vez que se carga la página.
            // await _importer.ImportWinTrendsAsync();
            await _importer.ImportAllStatsAsync();
        }
    }
}
