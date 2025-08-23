using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using StrikeData.Data;
using StrikeData.Models;

namespace StrikeData.Pages.MatchData
{
    /// <summary>
    /// Page model for displaying the details of a single match.  Shows per-inning
    /// statistics along with the final score and each team's record at the
    /// time of the game.  If the match is not found, returns a 404.
    /// </summary>
    public class DetailsModel : PageModel
    {
        private readonly AppDbContext _context;

        public DetailsModel(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// The full match entity including team and inning navigations.
        /// </summary>
        public Match? Match { get; set; }

        /// <summary>
        /// A collection of inning rows used to render the table on the page.
        /// </summary>
        public List<InningRow> InningRows { get; private set; } = new();

        /// <summary>
        /// Simple projection of a MatchInning used for display.
        /// </summary>
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
            // Load the match by its primary key and include the related teams and innings.
            Match = await _context.Matches
                .Include(m => m.HomeTeam)
                .Include(m => m.AwayTeam)
                .Include(m => m.Innings)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);

            if (Match == null)
            {
                return NotFound();
            }

            // Project each inning into a row for the table, ordered by inning number.
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