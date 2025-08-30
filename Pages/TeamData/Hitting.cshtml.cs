using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using StrikeData.Data;
using StrikeData.Models;
using StrikeData.Services.Glossary; 

namespace StrikeData.Pages.TeamData
{
    /// <summary>
    /// PageModel for the team-level Hitting view.
    /// Loads available Hitting StatTypes, exposes a filterable list of TeamStats,
    /// and provides glossary descriptions for the selected stat (for UI tooltips/explanations).
    /// </summary>
    public class HittingModel : PageModel
    {
        private readonly AppDbContext _context;

        public HittingModel(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Bound query parameter for the selected StatType in the dropdown.
        /// Null (or empty) means "All".
        /// </summary>
        [BindProperty(SupportsGet = true)]
        public int? SelectedStatTypeId { get; set; }

        /// <summary>
        /// All hitting StatTypes loaded from the database (category = "Hitting").
        /// </summary>
        public List<StatType> StatTypes { get; set; } = new();

        /// <summary>
        /// TeamStat rows to be displayed in the table. Filtered by SelectedStatTypeId if provided.
        /// </summary>
        public List<TeamStat> TeamStats { get; set; } = new();

        /// <summary>
        /// Options for the <select> element (value = StatType.Id, text = StatType.Name).
        /// </summary>
        public List<SelectListItem> StatOptions { get; set; } = new();

        /// <summary>
        /// Maps StatType.Id (as string, matching the select's value) to the glossary description text.
        /// Used by the client-side script to display an explanation of the chosen stat.
        /// </summary>
        public Dictionary<string, string> StatDescriptions { get; private set; } = new();

        public async Task OnGetAsync()
        {
            // Load StatTypes under the "Hitting" category
            StatTypes = await _context.StatTypes
                .Include(st => st.StatCategory)
                .Where(st => st.StatCategory != null && st.StatCategory.Name == "Hitting")
                .OrderBy(st => st.Name)
                .ToListAsync();

            // Build dropdown options and prepend the "All" option
            StatOptions = StatTypes
                .Select(st => new SelectListItem { Value = st.Id.ToString(), Text = st.Name })
                .ToList();
            StatOptions.Insert(0, new SelectListItem { Value = "", Text = "-- All --" });

            // ===== Descriptions from the central glossary =====
            // Domain: TeamHitting. Keyed by abbreviation (st.Name).
            var glossary = StatGlossary.GetMap(StatDomain.TeamHitting);
            StatDescriptions = new Dictionary<string, string>();
            foreach (var st in StatTypes)
            {
                // The select expects string keys (StatType.Id as string)
                if (glossary.TryGetValue(st.Name, out var statText) && !string.IsNullOrWhiteSpace(statText.Description))
                    StatDescriptions[st.Id.ToString()] = statText.Description;
                else
                    StatDescriptions[st.Id.ToString()] = ""; // fallback: no description available
            }

            // Base query: all TeamStats for Hitting category
            var query = _context.TeamStats
                .Include(ts => ts.Team)
                .Include(ts => ts.StatType)
                .Where(ts => ts.StatType.StatCategory != null && ts.StatType.StatCategory.Name == "Hitting")
                .AsQueryable();

            // Optional filter: single StatType selection (omit when "-- All --")
            if (SelectedStatTypeId.HasValue)
            {
                query = query.Where(ts => ts.StatTypeId == SelectedStatTypeId.Value);
            }

            // Order by current-season per-game value desc, then by team name
            TeamStats = await query
                .OrderByDescending(ts => ts.CurrentSeason)
                .ThenBy(ts => ts.Team.Name)
                .ToListAsync();
        }
    }
}
