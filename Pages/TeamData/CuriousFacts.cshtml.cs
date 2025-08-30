using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using StrikeData.Data;
using StrikeData.Models.Enums;
using StrikeData.Services.Glossary; 

namespace StrikeData.Pages.TeamData
{
    /// <summary>
    /// PageModel for "Curious Facts" team metrics. Provides:
    /// - The list of available stat types (base keys without the optional 'O' prefix)
    /// - A perspective toggle (team vs opponent)
    /// - A single results table bound to the current selection
    /// - A description dictionary used by the client-side helper to show contextual help
    /// </summary>
    public class CuriousFactsModel : PageModel
    {
        private readonly AppDbContext _context;

        public CuriousFactsModel(AppDbContext context)
        {
            _context = context;
        }

        // Select options for stat types (category = CuriousFacts)
        public List<SelectListItem> StatTypeOptions { get; set; } = new();

        // Table rows (projected view model)
        public List<CuriousFactRow> Rows { get; private set; } = new();

        // Current selections (bound from query string)
        [BindProperty(SupportsGet = true)]
        public string SelectedStatType { get; set; } = string.Empty;

        // "team" or "opp" (string for easy toggle binding)
        [BindProperty(SupportsGet = true)]
        public string Perspective { get; set; } = "team";

        // Base abbreviation -> description (used to populate the helper under the <select>)
        public Dictionary<string, string> StatDescriptions { get; private set; } = new();

        public async Task OnGetAsync()
        {
            // Load available stat types in the "CuriousFacts" category (values are base keys, e.g., "YRFI", "F5IR/G")
            StatTypeOptions = await _context.StatTypes
                .Where(st => st.StatCategory.Name == "CuriousFacts")
                .OrderBy(st => st.Name)
                .Select(st => new SelectListItem
                {
                    Value = st.Name,   // base key
                    Text = st.Name
                })
                .ToListAsync();

            // Default to the first stat type if nothing is selected
            if (string.IsNullOrWhiteSpace(SelectedStatType) && StatTypeOptions.Any())
            {
                SelectedStatType = StatTypeOptions.First().Value;
            }

            // Build description dictionary using the central glossary (domain = CuriousFacts)
            var glossary = StatGlossary.GetMap(StatDomain.CuriousFacts);
            StatDescriptions.Clear();
            foreach (var opt in StatTypeOptions)
            {
                var key = opt.Value;
                if (glossary.TryGetValue(key, out var st) && !string.IsNullOrWhiteSpace(st.Description))
                    StatDescriptions[key] = st.Description;
                else
                    StatDescriptions[key] = "";
            }

            // Map the UI perspective to the enum used in the database filter
            var desiredPerspective = Perspective?.ToLowerInvariant() == "opp"
                ? StatPerspective.Opponent
                : StatPerspective.Team;

            // Query TeamStats for the selected type and perspective; include Team for display name
            var query = _context.TeamStats
                .AsNoTracking()
                .Include(ts => ts.Team)
                .Include(ts => ts.StatType)
                .Where(ts =>
                    ts.StatType.StatCategory.Name == "CuriousFacts" &&
                    ts.StatType.Name == SelectedStatType &&
                    ts.Perspective == desiredPerspective);

            // Order by current-season value (desc), then team name (stable tie-breaker)
            var list = await query
                .OrderByDescending(ts => ts.CurrentSeason.HasValue)
                .ThenByDescending(ts => ts.CurrentSeason)
                .ThenBy(ts => ts.Team.Name)
                .Select(ts => new CuriousFactRow
                {
                    TeamName = ts.Team.Name,
                    CurrentSeason = ts.CurrentSeason,
                    Last3Games = ts.Last3Games,
                    LastGame = ts.LastGame,
                    Home = ts.Home,
                    Away = ts.Away,
                    PrevSeason = ts.PrevSeason
                })
                .ToListAsync();

            Rows = list;
        }

        /// <summary>
        /// View model projected for the table: only the fields displayed in the Razor view.
        /// </summary>
        public class CuriousFactRow
        {
            public string TeamName { get; set; } = "";
            public float? CurrentSeason { get; set; }
            public float? Last3Games { get; set; }
            public float? LastGame { get; set; }
            public float? Home { get; set; }
            public float? Away { get; set; }
            public float? PrevSeason { get; set; }
        }
    }
}
