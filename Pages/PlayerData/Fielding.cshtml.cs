using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using StrikeData.Data;
using StrikeData.Services.Glossary;

namespace StrikeData.Pages.PlayerData
{
    /// <summary>
    /// Razor PageModel for listing per-player fielding metrics by team.
    /// Loads glossary metadata and PlayerStats for the "Fielding" category.
    /// </summary>
    public class FieldingModel : PageModel
    {
        private readonly AppDbContext _context;

        public FieldingModel(AppDbContext context)
        {
            _context = context;
        }

        // Options for the team dropdown
        public List<SelectListItem> TeamOptions { get; set; } = new();

        // Selected team id from query string
        [BindProperty(SupportsGet = true)]
        public int SelectedTeamId { get; set; }

        // Fielding metrics rendered in the table (fixed list)
        public List<string> VisibleColumns { get; private set; } = new();

        // Flattened rows for the view
        public List<PlayerRow> Rows { get; private set; } = new();

        private static readonly string CategoryName = "Fielding";

        // Set of fielding abbreviations expected to exist in PlayerStatTypes
        private static readonly List<string> Columns = new()
        {
            "OUTS", "TC", "CH", "PO", "A", "E", "DP", "PB", "CASB", "CACS", "FLD%"
        };

        // Tooltip metadata per abbreviation
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
            public Dictionary<string, float?> Values { get; set; } =
                new(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Populates StatMeta from the central glossary for the fixed Columns list.
        /// </summary>
        private void InitStatMeta()
        {
            StatMeta.Clear();

            var map = StatGlossary.GetMap(StatDomain.PlayerFielding);

            foreach (var col in Columns)
            {
                if (map.TryGetValue(col, out var st))
                    StatMeta[col] = new StatInfo { LongName = st.LongName, Description = st.Description };
                else
                    StatMeta[col] = new StatInfo { LongName = col, Description = "" };
            }
        }

        public async Task OnGetAsync()
        {
            InitStatMeta();

            // Team dropdown data
            TeamOptions = await _context.Teams
                .OrderBy(t => t.Name)
                .Select(t => new SelectListItem { Value = t.Id.ToString(), Text = t.Name })
                .ToListAsync();

            // Default to the first team if none selected
            if (SelectedTeamId == 0 && TeamOptions.Any())
                SelectedTeamId = int.Parse(TeamOptions.First().Value);

            // All fielding columns are visible in this view
            VisibleColumns = Columns;

            // Load players for the selected team (fielding applies to all positions)
            var players = await _context.Players
                .AsNoTracking()
                .Where(p => p.TeamId == SelectedTeamId)
                .OrderBy(p => p.Name)
                .ToListAsync();

            var playerIds = players.Select(p => p.Id).ToList();

            // Resolve fielding stat types that match the expected columns
            var statTypes = await _context.PlayerStatTypes
                .AsNoTracking()
                .Include(st => st.StatCategory)
                .Where(st => st.StatCategory != null
                             && st.StatCategory.Name == CategoryName
                             && Columns.Contains(st.Name))
                .ToListAsync();

            var typeIds = statTypes.Select(s => s.Id).ToList();
            var nameByTypeId = statTypes.ToDictionary(s => s.Id, s => s.Name);

            // Fetch PlayerStats for the selected players and fielding types
            var stats = await _context.PlayerStats
                .AsNoTracking()
                .Where(ps => playerIds.Contains(ps.PlayerId) && typeIds.Contains(ps.PlayerStatTypeId))
                .ToListAsync();

            // Group by player for quick lookups during row composition
            var rows = new List<PlayerRow>();
            var byPlayer = stats.GroupBy(s => s.PlayerId)
                                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var p in players)
            {
                var row = new PlayerRow
                {
                    PlayerId = p.Id,
                    Name = p.Name,
                    Number = p.Number,
                    Position = p.Position
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
