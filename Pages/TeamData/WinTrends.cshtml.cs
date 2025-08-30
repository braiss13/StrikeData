using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using StrikeData.Data;

namespace StrikeData.Pages.TeamData
{
    /*
        PageModel for the Win Trends page.
        Loads the available trend types (from StatTypes in the WinTrends category),
        applies the selected filter, and projects TeamStats into a lightweight row model.
    */
    public class WinTrendsModel : PageModel
    {
        private readonly AppDbContext _context;

        public WinTrendsModel(AppDbContext context)
        {
            _context = context;
        }

        // Options for the stat-type <select> (Value/Text = StatType.Name).
        public List<SelectListItem> StatTypeOptions { get; set; } = new();

        // Materialized rows for the current selection.
        public List<WinTrendRow> Rows { get; private set; } = new();

        // Two-way bound stat type name chosen by the user (via query string).
        [BindProperty(SupportsGet = true)]
        public string SelectedStatType { get; set; } = string.Empty;

        public async Task OnGetAsync()
        {
            // 1) Load available stat types in the WinTrends category
            StatTypeOptions = await _context.StatTypes
                .Where(st => st.StatCategory.Name == "WinTrends")
                .OrderBy(st => st.Name)
                .Select(st => new SelectListItem
                {
                    Value = st.Name,
                    Text = st.Name
                })
                .ToListAsync();

            // Default to the first option if nothing was selected
            if (string.IsNullOrWhiteSpace(SelectedStatType) && StatTypeOptions.Any())
            {
                SelectedStatType = StatTypeOptions.First().Value;
            }

            // 2) Query TeamStats filtered by the selected StatType and Perspective=Team
            var query = _context.TeamStats
                .AsNoTracking()
                .Include(ts => ts.Team)
                .Include(ts => ts.StatType)
                .Where(ts =>
                    ts.StatType.StatCategory.Name == "WinTrends" &&
                    ts.StatType.Name == SelectedStatType &&
                    ts.Perspective == Models.Enums.StatPerspective.Team);

            // 3) Order by WinPct descending (nulls last), then by team name
            var list = await query
                .OrderByDescending(ts => ts.WinPct.HasValue)
                .ThenByDescending(ts => ts.WinPct)
                .ThenBy(ts => ts.Team.Name)
                .Select(ts => new WinTrendRow
                {
                    TeamName = ts.Team.Name,
                    WinLossRecord = ts.WinLossRecord,
                    WinPct = ts.WinPct
                })
                .ToListAsync();

            Rows = list;
        }

        // Small DTO for rendering a table row (keeps the view simple).
        public class WinTrendRow
        {
            public string TeamName { get; set; } = "";
            public string? WinLossRecord { get; set; }
            public float? WinPct { get; set; }
        }
    }
}
