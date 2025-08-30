using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using StrikeData.Data;
using StrikeData.Models;

namespace StrikeData.Pages.TeamData
{
    /*
        PageModel for the Team Schedule page. It loads a team's full schedule and
        two kinds of summaries (monthly splits and opponent splits) for a fixed season.
        The view supports two modes: "expanded" (per-game table) and "summarized" (split tables).
    */
    public class ScheduleModel : PageModel
    {
        private readonly AppDbContext _context;

        public ScheduleModel(AppDbContext context)
        {
            _context = context;
        }

        // Options for the team selector <select>.
        public List<SelectListItem> TeamOptions { get; set; } = new();

        // Bound query parameters: selected team and current view mode.
        [BindProperty(SupportsGet = true)]
        public int SelectedTeamId { get; set; }

        [BindProperty(SupportsGet = true)]
        public string ViewMode { get; set; } = "expanded"; // "expanded" | "summarized"

        // Data exposed to the Razor view.
        public List<TeamGame> Schedule { get; private set; } = new();
        public List<TeamMonthlySplit> MonthlySplits { get; private set; } = new();
        public List<TeamOpponentSplit> TeamSplits { get; private set; } = new();

        public async Task OnGetAsync()
        {
            // Populate team dropdown (sorted alphabetically).
            TeamOptions = await _context.Teams
                .OrderBy(t => t.Name)
                .Select(t => new SelectListItem { Value = t.Id.ToString(), Text = t.Name })
                .ToListAsync();

            // Default to the first team when none is selected.
            if (SelectedTeamId == 0 && TeamOptions.Any())
            {
                SelectedTeamId = int.Parse(TeamOptions.First().Value);
            }

            // Fixed season context (align with importers).
            int season = 2025;

            // Load per-game schedule for the selected team.
            Schedule = await _context.TeamGames
                .Where(g => g.TeamId == SelectedTeamId && g.Season == season)
                .OrderBy(g => g.GameNumber)
                .ToListAsync();

            // Load monthly split aggregates.
            MonthlySplits = await _context.TeamMonthlySplits
                .Where(s => s.TeamId == SelectedTeamId && s.Season == season)
                .OrderBy(s => s.Month)
                .ToListAsync();

            // Load opponent split aggregates.
            TeamSplits = await _context.TeamOpponentSplits
                .Where(s => s.TeamId == SelectedTeamId && s.Season == season)
                .OrderBy(s => s.OpponentName)
                .ToListAsync();
        }
    }
}
