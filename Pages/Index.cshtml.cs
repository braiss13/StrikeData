using Microsoft.AspNetCore.Mvc.RazorPages;
using StrikeData.Data;
using StrikeData.Services.PlayerData;
using StrikeData.Services.MatchData;
using StrikeData.Services.TeamData.Importers;
using StrikeData.Services.TeamData.Scrapers;

namespace StrikeData.Pages
{
    /// Home page model that triggers *full data imports* when accessed.
    public class IndexModel : PageModel
    {
        #region Team Stat Importers
        // TEAM importers: responsible for different stat categories and schedule.
        private readonly HittingImporter _hitting_importer;
        private readonly PitchingImporter _pitching_importer;
        private readonly FieldingImporter _fielding_importer;
        private readonly TeamScheduleImporter _schedule_importer;
        private readonly CuriousFactsImporter _curious_importer;
        private readonly WinTrendsImporter _wintrends_importer;
        #endregion

        #region Player Stat Importers
        // PLAYER importers: roster + season stats, and player fielding by team.
        private readonly PlayerStatsImporter _playerStatsImporter;
        private readonly PlayerFieldingImporter _playerFieldingImporter;
        #endregion

        #region Matches Importer
        // Matches importer: pulls schedule + linescore + per-inning details from MLB API.
        private readonly MatchImporter _matchImporter;
        #endregion

        public IndexModel(AppDbContext context)
        {
            #region Importer Inicialization (TEAM)
            // NOTE: These importers each create and manage their own HttpClient unless otherwise injected.
            // They read/write via the shared EF Core DbContext injected into this page model.
            _hitting_importer = new HittingImporter(context);
            _pitching_importer = new PitchingImporter(context);
            _fielding_importer = new FieldingImporter(context);

            // Reuse a single HttpClient instance for all HTTP-based scrapers in this request scope.
            // This helps prevent socket exhaustion and improves performance.
            var httpClient = new HttpClient();

            // Team schedule scraper requires an HttpClient. Pass the shared instance.
            var teamScheduleScraper = new TeamScheduleScraper(httpClient);
            _schedule_importer = new TeamScheduleImporter(context, teamScheduleScraper);

            _curious_importer = new CuriousFactsImporter(context);
            _wintrends_importer = new WinTrendsImporter(context);
            #endregion

            #region Importer Inicialization (PLAYERS)
            // Pulls rosters and (hitting/pitching) season stats for all players.
            _playerStatsImporter = new PlayerStatsImporter(context);

            // Player fielding scraper uses the shared HttpClient. Importer coordinates persistence.
            var playerFieldingScraper = new PlayerFieldingScraper(httpClient);
            _playerFieldingImporter = new PlayerFieldingImporter(context, playerFieldingScraper);
            #endregion

            #region Matches Importer Initialization
            // Matches importer (uses the shared HttpClient to call MLB StatsAPI).
            _matchImporter = new MatchImporter(context, httpClient);
            #endregion
        }

        /*
            Handles GET by running all importers sequentially.
            
            Execution order rationale:
            1) Team stat categories (hitting, pitching, fielding) to populate core aggregates.
            2) Team schedule (games and splits), then derived categories (curious facts, win trends)
                which may assume teams already exist.
            3) Player imports (roster + stats, then player fielding) so team references and rosters align.
            4) Match importer (season to date) last, as it writes many records and benefits from existing teams.
        */
        public async Task OnGetAsync()
        {
            #region Team Stat Importers
            // MLB + TeamRankings: aggregates per team for the Hitting category.
            await _hitting_importer.ImportAllStatsAsyncH();

            // MLB + TeamRankings: aggregates per team for the Pitching category.
            await _pitching_importer.ImportAllStatsAsyncP();

            // TeamRankings: aggregates per team for the Fielding category.
            await _fielding_importer.ImportAllStatsAsyncF();

            // Baseball Almanac: team schedule + monthly and vs-opponent splits for the given season.
            await _schedule_importer.ImportAllTeamsScheduleAsync(2025);

            // TeamRankings: curious facts by perspective (team/opponent). Depends on teams existing.
            await _curious_importer.ImportAllStatsAsyncCF();

            // TeamRankings: win trends (record and win%). Depends on teams existing.
            await _wintrends_importer.ImportAllStatsAsyncWT();
            #endregion

            #region Player Stat Importers
            // (1) Roster + player season stats (hitting/pitching) for all teams.
            await _playerStatsImporter.ImportAllPlayersAndStatsAsync(2025);

            // (2) Player fielding (first table per team; only if player already exists).
            await _playerFieldingImporter.ImportAllTeamsPlayerFieldingAsync(2025);
            #endregion

            #region Matches Importer
            // MLB StatsAPI: season matches from 2025-03-27 through "yesterday" in Europe/Madrid TZ.
            await _matchImporter.ImportSeasonMatchesAsync();
            #endregion
        }
    }
}
