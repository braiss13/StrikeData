using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using StrikeData.Data;
using StrikeData.Services.Glossary;

namespace StrikeData.Pages.PlayerData
{
    public class FieldingModel : PageModel
    {
        private readonly AppDbContext _context;

        public FieldingModel(AppDbContext context)
        {
            _context = context;
        }

        // Team dropdown options
        public List<SelectListItem> TeamOptions { get; set; } = new();

        // Bound property for selected team
        [BindProperty(SupportsGet = true)]
        public int SelectedTeamId { get; set; }

        // Columns to display (all fielding stats)
        public List<string> VisibleColumns { get; private set; } = new();

        // Player rows
        public List<PlayerRow> Rows { get; private set; } = new();

        private static readonly string CategoryName = "Fielding";

        // Fielding stat abbreviations
        private static readonly List<string> Columns = new()
        {
            "OUTS", "TC", "CH", "PO", "A", "E", "DP", "PB", "CASB", "CACS", "FLD%"
        };

        // Metadata for stats: long name and description
        public Dictionary<string, StatInfo> StatMeta { get; private set; } = new(StringComparer.OrdinalIgnoreCase);

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
            public Dictionary<string, float?> Values { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        }

        // Fill StatMeta with names and descriptions
        private void InitStatMeta()
        {
            StatMeta.Clear();

            // Trae el mapa del dominio
            var map = StatGlossary.GetMap(StatDomain.PlayerFielding);

            // Si quieres solo las columnas visibles:
            foreach (var col in Columns) // o VisibleColumns
            {
                if (map.TryGetValue(col, out var st))
                    StatMeta[col] = new StatInfo { LongName = st.LongName, Description = st.Description };
                else
                    StatMeta[col] = new StatInfo { LongName = col, Description = "" }; // fallback
            }
        }

        public async Task OnGetAsync()
        {
            InitStatMeta();

            // Teams for dropdown
            TeamOptions = await _context.Teams
                .OrderBy(t => t.Name)
                .Select(t => new SelectListItem { Value = t.Id.ToString(), Text = t.Name })
                .ToListAsync();

            if (SelectedTeamId == 0 && TeamOptions.Any())
                SelectedTeamId = int.Parse(TeamOptions.First().Value);

            // All fielding stats are visible
            VisibleColumns = Columns;

            // Load players for selected team (any position)
            var players = await _context.Players
                .AsNoTracking()
                .Where(p => p.TeamId == SelectedTeamId)
                .OrderBy(p => p.Name)
                .ToListAsync();

            var playerIds = players.Select(p => p.Id).ToList();

            // Load stat types in the Fielding category for these columns
            var statTypes = await _context.PlayerStatTypes
                .AsNoTracking()
                .Include(st => st.StatCategory)
                .Where(st => st.StatCategory != null
                             && st.StatCategory.Name == CategoryName
                             && Columns.Contains(st.Name))
                .ToListAsync();

            var typeIds = statTypes.Select(s => s.Id).ToList();
            var nameByTypeId = statTypes.ToDictionary(s => s.Id, s => s.Name);

            // Load player stats
            var stats = await _context.PlayerStats
                .AsNoTracking()
                .Where(ps => playerIds.Contains(ps.PlayerId) && typeIds.Contains(ps.PlayerStatTypeId))
                .ToListAsync();

            // Build rows
            var rows = new List<PlayerRow>();
            var byPlayer = stats.GroupBy(s => s.PlayerId).ToDictionary(g => g.Key, g => g.ToList());

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
