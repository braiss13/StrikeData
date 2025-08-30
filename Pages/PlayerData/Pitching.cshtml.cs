using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using StrikeData.Data;
using StrikeData.Services.Glossary;

namespace StrikeData.Pages.PlayerData
{
    /*
        Razor PageModel for listing per-player pitching stats by team.
        Loads glossary metadata and PlayerStats for the "Pitching" category.
    */
    public class PitchingPlayerModel : PageModel
    {
        private readonly AppDbContext _context;

        public PitchingPlayerModel(AppDbContext context)
        {
            _context = context;
        }

        // Options for the team dropdown
        public List<SelectListItem> TeamOptions { get; set; } = new();

        // Query string bindings
        [BindProperty(SupportsGet = true)]
        public int SelectedTeamId { get; set; }

        [BindProperty(SupportsGet = true)]
        public string ViewMode { get; set; } = "basic"; // "basic" | "advanced"

        // Columns resolved for the selected view mode
        public List<string> VisibleColumns { get; private set; } = new();

        // Flattened rows for the view
        public List<PlayerRow> Rows { get; private set; } = new();

        private static readonly string CategoryName = "Pitching";

        // Basic and advanced column sets; order defines the table column order
        private static readonly List<string> BasicCols = new()
        {
            "W","L","ERA","G","GS","CG","SHO","SV","SVO","IP","R","H","ER","HR","HB","BB","SO","WHIP","AVG"
        };

        private static readonly List<string> AdvancedCols = new()
        {
            "TBF","NP","P/IP","QS","GF","HLD","IBB","WP","BK","GDP","GO/AO","SO/9","BB/9","H/9","K/BB","BABIP","SB","CS","PK"
        };

        // Tooltip metadata (long name + description) per stat key
        public class StatInfo
        {
            public string LongName { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
        }

        public Dictionary<string, StatInfo> StatMeta { get; private set; } =
            new(StringComparer.OrdinalIgnoreCase);

        // Row model for the Razor view
        public class PlayerRow
        {
            public int PlayerId { get; set; }
            public int? Number { get; set; }
            public string Name { get; set; } = string.Empty;
            public string? Position { get; set; }
            public string? Status { get; set; }
            public Dictionary<string, float?> Values { get; set; } =
                new(StringComparer.OrdinalIgnoreCase);
        }

        /*
            Initializes the StatMeta dictionary with the stats required by the current view.
            In Basic mode the "Status" column is included.
        */
        private void InitStatMeta()
        {
            StatMeta.Clear();

            var map = StatGlossary.GetMap(StatDomain.PlayerPitching);

            var keys = new List<string>(VisibleColumns);
            if ((ViewMode?.ToLowerInvariant() ?? "basic") == "basic")
                keys.Insert(0, "Status");

            foreach (var key in keys.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (map.TryGetValue(key, out var st))
                    StatMeta[key] = new StatInfo { LongName = st.LongName, Description = st.Description };
                else
                    StatMeta[key] = new StatInfo { LongName = key, Description = "" };
            }
        }

        public async Task OnGetAsync()
        {
            // Team dropdown data
            TeamOptions = await _context.Teams
                .OrderBy(t => t.Name)
                .Select(t => new SelectListItem { Value = t.Id.ToString(), Text = t.Name })
                .ToListAsync();

            // Default to the first team when none is selected
            if (SelectedTeamId == 0 && TeamOptions.Any())
                SelectedTeamId = int.Parse(TeamOptions.First().Value);

            // Resolve columns for the requested mode
            ViewMode = (ViewMode?.ToLowerInvariant() == "advanced") ? "advanced" : "basic";
            VisibleColumns = (ViewMode == "advanced")
                ? new List<string>(AdvancedCols)
                : new List<string>(BasicCols);

            InitStatMeta();

            // Load pitchers only (Position == "P") for the selected team
            var players = await _context.Players
                .AsNoTracking()
                .Where(p => p.TeamId == SelectedTeamId && p.Position == "P")
                .OrderBy(p => p.Name)
                .ToListAsync();

            var playerIds = players.Select(p => p.Id).ToList();

            // Resolve pitching stat types matching the current column set
            var statTypes = await _context.PlayerStatTypes
                .AsNoTracking()
                .Include(st => st.StatCategory)
                .Where(st => st.StatCategory != null
                             && st.StatCategory.Name == CategoryName
                             && VisibleColumns.Contains(st.Name))
                .ToListAsync();

            var typeIds = statTypes.Select(s => s.Id).ToList();
            var nameByTypeId = statTypes.ToDictionary(s => s.Id, s => s.Name);

            // Fetch the per-player stats
            var stats = await _context.PlayerStats
                .AsNoTracking()
                .Where(ps => playerIds.Contains(ps.PlayerId) && typeIds.Contains(ps.PlayerStatTypeId))
                .ToListAsync();

            // Group stats by player to speed up row construction
            var byPlayer = stats.GroupBy(s => s.PlayerId)
                                .ToDictionary(g => g.Key, g => g.ToList());

            // Compose rows for the view
            var resultRows = new List<PlayerRow>();
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

                resultRows.Add(row);
            }

            Rows = resultRows;
        }
    }
}
