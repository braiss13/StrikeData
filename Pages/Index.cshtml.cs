using Microsoft.AspNetCore.Mvc.RazorPages;
using StrikeData.Data;
using StrikeData.Services.PlayerData;
using StrikeData.Services.TeamData;
using StrikeData.Services.MatchData;

namespace StrikeData.Pages
{
    public class IndexModel : PageModel
    {
        #region Importadores de estadísticas de equipos
        private readonly HittingImporter _hitting_importer;
        private readonly PitchingImporter _pitching_importer;
        private readonly FieldingImporter _fielding_importer;
        private readonly TeamScheduleImporter _schedule_importer;
        private readonly CuriousFactsImporter _curious_importer;
        private readonly WinTrendsImporter _wintrends_importer;
        #endregion

        #region Importadores de jugadores
        private readonly PlayerStatsImporter _playerStatsImporter;
        private readonly PlayerFieldingImporter _playerFieldingImporter;
        #endregion

        private readonly MatchImporter _matchImporter;

        public IndexModel(AppDbContext context)
        {
            #region Inicialización de importadores (TEAM)
            _hitting_importer   = new HittingImporter(context);
            _pitching_importer  = new PitchingImporter(context);
            _fielding_importer  = new FieldingImporter(context);

            // Un único HttpClient para ambos scrapers
            var httpClient = new HttpClient();

            // Scraper + importador de Schedule (teams)
            var teamScheduleScraper = new TeamScheduleScraper(httpClient);
            _schedule_importer      = new TeamScheduleImporter(context, teamScheduleScraper);

            _curious_importer    = new CuriousFactsImporter(context);
            _wintrends_importer  = new WinTrendsImporter(context);
            #endregion

            #region Inicialización de importadores (PLAYERS)
            _playerStatsImporter     = new PlayerStatsImporter(context); // roster + (hitting/pitching) season
            // scraper + importador de fielding (players)
            var playerFieldingScraper = new PlayerFieldingScraper(httpClient);
            _playerFieldingImporter   = new PlayerFieldingImporter(context, playerFieldingScraper);
            #endregion

            // Importador de partidos
            _matchImporter = new MatchImporter(context, httpClient);
        }

        public async Task OnGetAsync()
        {
            // #region Importadores de estadísticas de equipos
            // await _hitting_importer.ImportAllStatsAsyncH();
            // await _pitching_importer.ImportAllStatsAsyncP();
            // await _fielding_importer.ImportAllStatsAsyncF();
            // await _schedule_importer.ImportAllTeamsScheduleAsync(2025);
            // await _curious_importer.ImportAllStatsAsyncCF();
            // await _wintrends_importer.ImportAllStatsAsyncWT();
            // #endregion

            // #region Importadores de jugadores
            // // 1) Roster + stats (hitting/pitching) para todos los jugadores
            // await _playerStatsImporter.ImportAllPlayersAndStatsAsync(2025);

            // // 2) Fielding de jugadores (por equipo; sólo primera tabla; sólo si el jugador existe)
            // await _playerFieldingImporter.ImportAllTeamsPlayerFieldingAsync(2025);
            // #endregion

            // // Importador de partidos (desde 2025-03-27 hasta ayer)
            // await _matchImporter.ImportSeasonMatchesAsync();
        }
    }
}
