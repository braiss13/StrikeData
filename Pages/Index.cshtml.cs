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
        private readonly TeamScheduleImporter _schedule_importer;
        private readonly CuriousFactsImporter _curious_importer;

        public IndexModel(AppDbContext context)
        {
            _hitting_importer = new HittingImporter(context);

            _pitching_importer = new PitchingImporter(context);

            _fielding_importer = new FieldingImporter(context);

            // Instanciar HttpClient y scraper, y crear el importador de schedule
            var httpClient = new HttpClient();
            var scraper = new TeamScheduleScraper(httpClient);
            _schedule_importer = new TeamScheduleImporter(context, scraper);

            _curious_importer = new CuriousFactsImporter(context);
        }

        public async Task OnGetAsync()
        {
            
            // Importador de estadísticas para batting de equipos
            await _hitting_importer.ImportAllStatsAsyncH();
            // Importador de estadísticas para pitching de equipos
            await _pitching_importer.ImportAllStatsAsyncP();
            // Importador de estadísticas para fielding de equipos
            await _fielding_importer.ImportAllStatsAsyncF();
            // Iportador de estadístcas de resultados de equipos (para el año que se pasa por parámetro)
            await _schedule_importer.ImportAllTeamsScheduleAsync(2025);
            // Importador de estadísticas para stats de equipos más curiosas
            await _curious_importer.ImportAllStatsAsyncCF();
            
        }
    }
}
