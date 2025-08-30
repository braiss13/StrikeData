using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using StrikeData.Data;
using StrikeData.Services.Glossary;

namespace StrikeData.Pages.PlayerData
{
    /// <summary>
    /// Razor PageModel for listing hitting stats by team.
    /// It renders a basic/advanced view with tooltips sourced from the glossary.
    /// </summary>
    public class HittingPlayerModel : PageModel
    {
        private readonly AppDbContext _context;

        public HittingPlayerModel(AppDbContext context)
        {
            _context = context;
        }

        // Team dropdown options
        public List<SelectListItem> TeamOptions { get; set; } = new();

        // Query string bindings
        [BindProperty(SupportsGet = true)]
        public int SelectedTeamId { get; set; }

        [BindProperty(SupportsGet = true)]
        public string ViewMode { get; set; } = "basic"; // "basic" | "advanced"

        // Columns resolved for the selected view mode
        public List<string> VisibleColumns { get; private set; } = new();

        // Rows to render in the view
        public List<PlayerRow> Rows { get; private set; } = new();

        private static readonly string CategoryName = "Hitting";

        // Base column sets
        private static readonly List<string> BasicCols = new()
        {
            "G","AB","R","H","2B","3B","HR","RBI","BB","SO","SB","CS","AVG","OBP","SLG","OPS"
        };

         // Advanced column sets
        private static readonly List<string> AdvancedCols = new()
        {
            "PA","HBP","SAC","SF","GIDP","GO/AO","XBH","TB","IBB","BABIP","ISO","AB/HR","BB/K","BB%","SO%","HR%"
        };

        // Tooltip metadata (long name + description) per stat key
        public Dictionary<string, StatInfo> StatMeta { get; private set; } =
            new(StringComparer.OrdinalIgnoreCase);

        public class StatInfo
        {
            public string LongName { get; set; } = "";
            public string Description { get; set; } = "";
        }

        public class PlayerRow
        {
            public int PlayerId { get; set; }
            public int? Number { get; set; }
            public string Name { get; set; } = "";
            public string? Position { get; set; }
            public string? Status { get; set; }
            public Dictionary<string, float?> Values { get; set; } =
                new(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Loads glossary metadata for the current set of visible keys (Status + columns).
        /// </summary>
        private void InitStatMeta()
        {
            StatMeta.Clear();

            var map = StatGlossary.GetMap(StatDomain.PlayerHitting);

            var keys = new List<string> { "Status" };
            keys.AddRange(VisibleColumns);

            foreach (var key in keys)
            {
                if (map.TryGetValue(key, out var st))
                {
                    StatMeta[key] = new StatInfo
                    {
                        LongName = st.LongName,
                        Description = st.Description
                    };
                }
                else
                {
                    // Fallback when a key is not defined in the glossary
                    StatMeta[key] = new StatInfo { LongName = key, Description = "" };
                }
            }
        }

        public async Task OnGetAsync()
        {
            // Load teams for the dropdown
            TeamOptions = await _context.Teams
                .OrderBy(t => t.Name)
                .Select(t => new SelectListItem { Value = t.Id.ToString(), Text = t.Name })
                .ToListAsync();

            // Default to the first team when none is selected
            if (SelectedTeamId == 0 && TeamOptions.Any())
                SelectedTeamId = int.Parse(TeamOptions.First().Value);

            // Resolve visible columns based on the selected mode
            VisibleColumns = (ViewMode?.ToLowerInvariant() == "advanced") ? AdvancedCols : BasicCols;

            // Prepare glossary metadata for tooltips
            InitStatMeta();

            // Non-pitchers for the selected team
            var players = await _context.Players
                .AsNoTracking()
                .Where(p =>
                    p.TeamId == SelectedTeamId &&
                    (p.Position == null || p.Position.ToUpper() != "P"))
                .OrderBy(p => p.Name)
                .ToListAsync();

            var playerIds = players.Select(p => p.Id).ToList();

            // PlayerStatTypes for "Hitting" that match the visible columns
            var statTypes = await _context.PlayerStatTypes
                .AsNoTracking()
                .Include(st => st.StatCategory)
                .Where(st => st.StatCategory != null
                             && st.StatCategory.Name == CategoryName
                             && VisibleColumns.Contains(st.Name))
                .ToListAsync();

            var typeIds = statTypes.Select(s => s.Id).ToList();
            var nameByTypeId = statTypes.ToDictionary(s => s.Id, s => s.Name);

            // PlayerStats for those players and types
            var stats = await _context.PlayerStats
                .AsNoTracking()
                .Where(ps => playerIds.Contains(ps.PlayerId) && typeIds.Contains(ps.PlayerStatTypeId))
                .ToListAsync();

            // Build rows keyed by player
            var rows = new List<PlayerRow>();
            var byPlayer = stats
                .GroupBy(s => s.PlayerId)
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var p in players)
            {
                var row = new PlayerRow
                {
                    PlayerId = p.Id,
                    Name = p.Name,
                    Number = p.Number,
                    Position = p.Position,
                    Status = p.Status
                };

                if (byPlayer.TryGetValue(p.Id, out var list))
                {
                    foreach (var s in list)
                    {
                        if (nameByTypeId.TryGetValue(s.PlayerStatTypeId, out var nm))
                            row.Values[nm] = s.Total;
                    }
                }

                rows.Add(row);
            }

            Rows = rows;
        }
        }
}
