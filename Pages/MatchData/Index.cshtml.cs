using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using StrikeData.Data;
using StrikeData.Models;

namespace StrikeData.Pages.MatchData
{
    /// <summary>
    /// Page model for displaying a list of matches for a given date.  The page
    /// defaults to showing yesterday's games in the Europe/Madrid timezone if
    /// no date is provided.  Each match card links to a details view.
    /// </summary>
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;

        public IndexModel(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Date selected by the user via query string.  This value is bound
        /// on GET requests only.  The time component is ignored.
        /// </summary>
        [BindProperty(SupportsGet = true)]
        public DateTime? SelectedDate { get; set; }

        /// <summary>
        /// A lightweight projection of the Match entity used to populate
        /// the cards on the index page.  Includes team names, runs and
        /// venue information.
        /// </summary>
        public List<MatchSummary> Matches { get; private set; } = new();

        public class MatchSummary
        {
            public int Id { get; set; }
            public long GamePk { get; set; }
            public string HomeTeam { get; set; } = "";
            public string AwayTeam { get; set; } = "";
            public int? HomeRuns { get; set; }
            public int? AwayRuns { get; set; }
            public string? Venue { get; set; }
        }

        public async Task OnGetAsync()
        {
            // Determine the date we need to query.  If a date is provided, use
            // its date portion.  Otherwise default to yesterday based on the
            // Europe/Madrid timezone.
            var madrid = TimeZoneInfo.FindSystemTimeZoneById("Europe/Madrid");
            var todayLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, madrid).Date;
            var selectedLocalDate = SelectedDate?.Date ?? todayLocal.AddDays(-1);

            // Reflect back the normalized selected date so it binds back into the
            // date input correctly on post-back.
            SelectedDate = selectedLocalDate;

            // Convert the local date boundaries to UTC for comparison in the database.
            var startUtc = TimeZoneInfo.ConvertTimeToUtc(selectedLocalDate, madrid);
            var endUtc = TimeZoneInfo.ConvertTimeToUtc(selectedLocalDate.AddDays(1), madrid);

            // Query matches that start within the selected local day.  Bring
            // related team information into the query to avoid n+1 lookups.
            var matches = await _context.Matches
                .Include(m => m.HomeTeam)
                .Include(m => m.AwayTeam)
                .AsNoTracking()
                .Where(m => m.Date >= startUtc && m.Date < endUtc)
                .ToListAsync();

            Matches = matches
                .Select(m => new MatchSummary
                {
                    Id = m.Id,
                    GamePk = m.GamePk,
                    HomeTeam = m.HomeTeam?.Name ?? string.Empty,
                    AwayTeam = m.AwayTeam?.Name ?? string.Empty,
                    HomeRuns = m.HomeRuns,
                    AwayRuns = m.AwayRuns,
                    Venue = m.Venue
                })
                .OrderBy(ms => ms.HomeTeam)
                .ThenBy(ms => ms.AwayTeam)
                .ToList();
        }
    }
}