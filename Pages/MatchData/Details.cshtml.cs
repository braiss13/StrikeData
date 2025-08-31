using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using StrikeData.Data;
using StrikeData.Models;

namespace StrikeData.Pages.MatchData
{
    /*  Page model that renders a single match with its per-inning breakdown.
      - Loads Match by PK including HomeTeam, AwayTeam, and Innings.
    */
    public class DetailsModel : PageModel
    {
        private readonly AppDbContext _context;

        public DetailsModel(AppDbContext context)
        {
            _context = context;
        }

        // The full match entity including team and inning navigations.
        public Match? Match { get; set; }

        // Projection used to render the per-inning table compactly.
        public List<InningRow> InningRows { get; private set; } = new();

        /// Presentation model for a single inning line.
        public class InningRow
        {
            public int InningNumber { get; set; }
            public int? HomeRuns { get; set; }
            public int? HomeHits { get; set; }
            public int? HomeErrors { get; set; }
            public int? AwayRuns { get; set; }
            public int? AwayHits { get; set; }
            public int? AwayErrors { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            // Fetch match by primary key. Include() reduces extra DB roundtrips.
            Match = await _context.Matches
                .Include(m => m.HomeTeam)
                .Include(m => m.AwayTeam)
                .Include(m => m.Innings)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);

            if (Match == null)
            {
                // Return a proper 404 if the match does not exist.
                return NotFound();
            }

            // Order innings ascending and project to the presentation model.
            InningRows = Match.Innings
                .OrderBy(i => i.InningNumber)
                .Select(i => new InningRow
                {
                    InningNumber = i.InningNumber,
                    HomeRuns = i.HomeRuns,
                    HomeHits = i.HomeHits,
                    HomeErrors = i.HomeErrors,
                    AwayRuns = i.AwayRuns,
                    AwayHits = i.AwayHits,
                    AwayErrors = i.AwayErrors
                })
                .ToList();

            return Page();
        }
    }
}
