using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using StrikeData.Data;
using StrikeData.Models;
using StrikeData.Services.Glossary; 

namespace StrikeData.Pages.TeamData
{
    /*
        PageModel for TEAM Fielding. Loads the list of fielding StatTypes, builds the
        dropdown, retrieves TeamStats for the "Fielding" category, and exposes a
        description map (from the central glossary) keyed by StatType.Id for the UI.
    */
    public class FieldingModel : PageModel
    {
        private readonly AppDbContext _context;

        public FieldingModel(AppDbContext context)
        {
            _context = context;
        }

        // Bound filter: selected StatType to display. Null => show all fielding metrics.
        [BindProperty(SupportsGet = true)]
        public int? SelectedStatTypeId { get; set; }

        // Loaded from DB to populate options and render names in the table.
        public List<StatType> StatTypes { get; set; } = new();

        // Result set shown in the table (one row per team/stat combination).
        public List<TeamStat> TeamStats { get; set; } = new();

        // Dropdown options; includes a leading "-- All --" entry.
        public List<SelectListItem> StatOptions { get; set; } = new();

        // Id (as string) -> description text. The view uses this to show contextual help.
        public Dictionary<string, string> StatDescriptions { get; private set; } = new();

        public async Task OnGetAsync()
        {
            // 1) Load the StatTypes under the "Fielding" category.
            StatTypes = await _context.StatTypes
                .Include(st => st.StatCategory)
                .Where(st => st.StatCategory != null && st.StatCategory.Name == "Fielding")
                .OrderBy(st => st.Name)
                .ToListAsync();

            // 2) Build <select> options and prepend the "All" option for convenience.
            StatOptions = StatTypes
                .Select(st => new SelectListItem { Value = st.Id.ToString(), Text = st.Name })
                .ToList();
            StatOptions.Insert(0, new SelectListItem { Value = "", Text = "-- All --" });

            // 3) Build the Id -> description map using the centralized glossary.
            //    Lookups are by abbreviation (StatType.Name); empty description if not found.
            var glossary = StatGlossary.GetMap(StatDomain.TeamFielding);
            StatDescriptions.Clear();
            foreach (var st in StatTypes)
            {
                if (!string.IsNullOrWhiteSpace(st.Name) &&
                    glossary.TryGetValue(st.Name, out var statText) &&
                    !string.IsNullOrWhiteSpace(statText.Description))
                {
                    StatDescriptions[st.Id.ToString()] = statText.Description;
                }
                else
                {
                    StatDescriptions[st.Id.ToString()] = "";
                }
            }

            // 4) Base query: all TeamStats bound to the "Fielding" category.
            var query = _context.TeamStats
                .Include(ts => ts.Team)
                .Include(ts => ts.StatType)
                .Where(ts => ts.StatType.StatCategory != null && ts.StatType.StatCategory.Name == "Fielding")
                .AsQueryable();

            // 5) Optional filter: if a specific StatType was selected, narrow the result set.
            if (SelectedStatTypeId.HasValue)
            {
                query = query.Where(ts => ts.StatTypeId == SelectedStatTypeId.Value);
            }

            // 6) Sort by current season value (descending) and then by team name for a stable order.
            TeamStats = await query
                .OrderByDescending(ts => ts.CurrentSeason)
                .ThenBy(ts => ts.Team.Name)
                .ToListAsync();
        }
    }
}
