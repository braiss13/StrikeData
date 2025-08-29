using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using StrikeData.Data;
using StrikeData.Services.Glossary;

namespace StrikeData.Pages.PlayerData
{
    /// <summary>
    /// PageModel for the player pitching statistics page. This model loads the pitching
    /// statistics for all pitchers on a selected team and exposes them to the Razor
    /// view. It also provides metadata for each statistic so the view can show
    /// descriptive tooltips when the user hovers over a stat abbreviation.
    /// </summary>
    public class PitchingPlayerModel : PageModel
    {
        private readonly AppDbContext _context;

        public PitchingPlayerModel(AppDbContext context)
        {
            _context = context;
        }

        // Dropdown list of teams
        public List<SelectListItem> TeamOptions { get; set; } = new();

        // Bindings
        [BindProperty(SupportsGet = true)]
        public int SelectedTeamId { get; set; }

        [BindProperty(SupportsGet = true)]
        public string ViewMode { get; set; } = "basic"; // "basic" | "advanced"

        // Columns visible in the current view
        public List<string> VisibleColumns { get; private set; } = new();

        // Rows of statistics for display
        public List<PlayerRow> Rows { get; private set; } = new();

        private static readonly string CategoryName = "Pitching";

        // Basic and advanced pitching metrics. These lists define the order and which
        // statistics appear in the Basic and Advanced views. The same abbreviations
        // are used as keys in the StatMeta dictionary.
        private static readonly List<string> BasicCols = new()
        {
            "W","L","ERA","G","GS","CG","SHO","SV","SVO","IP","R","H","ER","HR","HB","BB","SO","WHIP","AVG"
        };

        private static readonly List<string> AdvancedCols = new()
        {
            "TBF","NP","P/IP","QS","GF","HLD","IBB","WP","BK","GDP","GO/AO","SO/9","BB/9","H/9","K/BB","BABIP","SB","CS","PK"
        };

        // Metadata for each statistic: long name and description
        public class StatInfo
        {
            public string LongName { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
        }

        public Dictionary<string, StatInfo> StatMeta { get; private set; } = new(StringComparer.OrdinalIgnoreCase);

        // Representation of a player's pitching statistics for the table
        public class PlayerRow
        {
            public int PlayerId { get; set; }
            public int? Number { get; set; }
            public string Name { get; set; } = string.Empty;
            public string? Position { get; set; }
            public string? Status { get; set; }
            public Dictionary<string, float?> Values { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Initializes the StatMeta dictionary with long names and descriptions
        /// for each pitching statistic. This should be called before the view
        /// renders so tooltips can be generated.
        /// </summary>
        private void InitStatMeta()
        {
            StatMeta.Clear();

            // Trae el mapa del dominio (glosario central)
            var map = StatGlossary.GetMap(StatDomain.PlayerPitching);

            // Claves visibles: columnas visibles + (Status solo en BASIC)
            var keys = new List<string>(VisibleColumns);
            if ((ViewMode?.ToLowerInvariant() ?? "basic") == "basic")
                keys.Insert(0, "Status");

            foreach (var key in keys.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (map.TryGetValue(key, out var st))
                    StatMeta[key] = new StatInfo { LongName = st.LongName, Description = st.Description };
                else
                    StatMeta[key] = new StatInfo { LongName = key, Description = "" }; // fallback
            }
        }

        public async Task OnGetAsync()
        {
            // Dropdown equipos
            TeamOptions = await _context.Teams
                .OrderBy(t => t.Name)
                .Select(t => new SelectListItem { Value = t.Id.ToString(), Text = t.Name })
                .ToListAsync();

            if (SelectedTeamId == 0 && TeamOptions.Any())
                SelectedTeamId = int.Parse(TeamOptions.First().Value);

            // Determina columnas visibles según el modo
            ViewMode = (ViewMode?.ToLowerInvariant() == "advanced") ? "advanced" : "basic";
            VisibleColumns = (ViewMode == "advanced") ? new List<string>(AdvancedCols) : new List<string>(BasicCols);

            InitStatMeta();

            // Load pitchers for the selected team (position == "P")
            var players = await _context.Players
                .AsNoTracking()
                .Where(p => p.TeamId == SelectedTeamId && p.Position == "P")
                .OrderBy(p => p.Name)
                .ToListAsync();

            var playerIds = players.Select(p => p.Id).ToList();

            // Get PlayerStatTypes in this category for the visible columns
            var statTypes = await _context.PlayerStatTypes
                .AsNoTracking()
                .Include(st => st.StatCategory)
                .Where(st => st.StatCategory != null
                             && st.StatCategory.Name == CategoryName
                             && VisibleColumns.Contains(st.Name))
                .ToListAsync();

            var typeIds = statTypes.Select(s => s.Id).ToList();
            var nameByTypeId = statTypes.ToDictionary(s => s.Id, s => s.Name);

            // Fetch the player stats for the selected players and stat types
            var stats = await _context.PlayerStats
                .AsNoTracking()
                .Where(ps => playerIds.Contains(ps.PlayerId) && typeIds.Contains(ps.PlayerStatTypeId))
                .ToListAsync();

            // Group stats by player id for quick lookup
            var byPlayer = stats.GroupBy(s => s.PlayerId)
                                .ToDictionary(g => g.Key, g => g.ToList());

            // Build rows for display
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
                        {
                            row.Values[nm] = s.Total;
                        }
                    }
                }
                resultRows.Add(row);
            }
            Rows = resultRows;
        }
    }
}