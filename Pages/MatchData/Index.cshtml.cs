using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using StrikeData.Data;

namespace StrikeData.Pages.MatchData
{
    /*  Page model that lists matches for a selected date.
        Defaults to "yesterday" in Europe/Madrid if no date is provided.
        Each item is a small projection suitable for the card layout.
    */
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;

        public IndexModel(AppDbContext context)
        {
            _context = context;
        }

        /// Bound date from query string (yyyy-MM-dd). use only the date part.
        [BindProperty(SupportsGet = true)]
        public DateTime? SelectedDate { get; set; }

        /// Lightweight projection used by the view to render cards. Keeps data transfer small.
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

            /*  Determine the target local date:
                - If a date query is provided, use that
                - Otherwise default to "yesterday" in Europe/Madrid to avoid same-day partial results
            */
            var madrid = TimeZoneInfo.FindSystemTimeZoneById("Europe/Madrid");
            var todayLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, madrid).Date;
            var selectedLocalDate = SelectedDate?.Date ?? todayLocal.AddDays(-1);

            // Reflect the normalized date back to the form field.
            SelectedDate = selectedLocalDate;

            // Translate local day boundaries to UTC to query by Match.Date (stored in UTC).
            var startUtc = TimeZoneInfo.ConvertTimeToUtc(selectedLocalDate, madrid);
            var endUtc = TimeZoneInfo.ConvertTimeToUtc(selectedLocalDate.AddDays(1), madrid);

            // Query matches starting within the chosen local day. Include team names for display.
            // AsNoTracking for read-only improves performance and reduces memory overhead.
            var matches = await _context.Matches
                .Include(m => m.HomeTeam)
                .Include(m => m.AwayTeam)
                .AsNoTracking()
                .Where(m => m.Date >= startUtc && m.Date < endUtc)
                .ToListAsync();

            // Project to a compact view model sorted by team names for stable ordering.
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
