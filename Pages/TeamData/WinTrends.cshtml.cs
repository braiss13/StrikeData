using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using StrikeData.Data;

namespace StrikeData.Pages.TeamData
{
    /// <summary>
    /// PageModel for the Win Trends page.
    /// Loads the available trend types (from StatTypes in the WinTrends category),
    /// applies the selected filter, and projects TeamStats into a lightweight row model.
    /// </summary>
    public class WinTrendsModel : PageModel
    {
        private readonly AppDbContext _context;

        public WinTrendsModel(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Options for the stat-type <select> (Value/Text = StatType.Name).
        /// </summary>
        public List<SelectListItem> StatTypeOptions { get; set; } = new();

        /// <summary>
        /// Materialized rows for the current selection.
        /// </summary>
        public List<WinTrendRow> Rows { get; private set; } = new();

        /// <summary>
        /// Two-way bound stat type name chosen by the user (via query string).
        /// </summary>
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

        /// <summary>
        /// Small DTO for rendering a table row (keeps the view simple).
        /// </summary>
        public class WinTrendRow
        {
            public string TeamName { get; set; } = "";
            public string? WinLossRecord { get; set; }
            public float? WinPct { get; set; }
        }
    }
}
