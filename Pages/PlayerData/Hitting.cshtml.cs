using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using StrikeData.Data;
using StrikeData.Services.Glossary;

namespace StrikeData.Pages.PlayerData
{
    public class HittingPlayerModel : PageModel
    {
        private readonly AppDbContext _context;

        public HittingPlayerModel(AppDbContext context)
        {
            _context = context;
        }

        // Dropdown teams
        public List<SelectListItem> TeamOptions { get; set; } = new();

        // Bindings
        [BindProperty(SupportsGet = true)]
        public int SelectedTeamId { get; set; }

        [BindProperty(SupportsGet = true)]
        public string ViewMode { get; set; } = "basic"; // "basic" | "advanced"

        // Columns visible according to mode
        public List<string> VisibleColumns { get; private set; } = new();

        // Rows for display
        public List<PlayerRow> Rows { get; private set; } = new();

        private static readonly string CategoryName = "Hitting";

        // Basic and advanced column lists
        private static readonly List<string> BasicCols = new()
        {
            "G","AB","R","H","2B","3B","HR","RBI","BB","SO","SB","CS","AVG","OBP","SLG","OPS"
        };

        private static readonly List<string> AdvancedCols = new()
        {
            "PA","HBP","SAC","SF","GIDP","GO/AO","XBH","TB","IBB","BABIP","ISO","AB/HR","BB/K","BB%","SO%","HR%"
        };

        // Metadata for stats (long name and description)
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
            public string? Status { get; set; }
            public Dictionary<string, float?> Values { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        }

        private void InitStatMeta()
        {
            StatMeta.Clear();

            // Trae el mapa del dominio (glosario completo)
            var map = StatGlossary.GetMap(StatDomain.PlayerHitting);

            // Claves visibles en esta vista: Status + columnas visibles (basic/advanced)
            var keys = new List<string> { "Status" };
            keys.AddRange(VisibleColumns);

            foreach (var key in keys)
            {
                if (map.TryGetValue(key, out var st))
                    StatMeta[key] = new StatInfo { LongName = st.LongName, Description = st.Description };
                else
                    StatMeta[key] = new StatInfo { LongName = key, Description = "" }; // fallback
            }
        }


        public async Task OnGetAsync()
        {
            // Teams for dropdown
            TeamOptions = await _context.Teams
                .OrderBy(t => t.Name)
                .Select(t => new SelectListItem { Value = t.Id.ToString(), Text = t.Name })
                .ToListAsync();

            if (SelectedTeamId == 0 && TeamOptions.Any())
                SelectedTeamId = int.Parse(TeamOptions.First().Value);

            // Determina columnas visibles según el modo
            VisibleColumns = (ViewMode?.ToLowerInvariant() == "advanced") ? AdvancedCols : BasicCols;

            InitStatMeta();

            // Non-pitcher players for the selected team
            var players = await _context.Players
                .AsNoTracking()
                .Where(p =>
                    p.TeamId == SelectedTeamId &&
                    (p.Position == null || p.Position.ToUpper() != "P"))
                .OrderBy(p => p.Name)
                .ToListAsync();

            var playerIds = players.Select(p => p.Id).ToList();

            // Get PlayerStatTypes in this category for visible columns
            var statTypes = await _context.PlayerStatTypes
                .AsNoTracking()
                .Include(st => st.StatCategory)
                .Where(st => st.StatCategory != null
                             && st.StatCategory.Name == CategoryName
                             && VisibleColumns.Contains(st.Name))
                .ToListAsync();

            var typeIds = statTypes.Select(s => s.Id).ToList();
            var nameByTypeId = statTypes.ToDictionary(s => s.Id, s => s.Name);

            // PlayerStats for those players and metrics
            var stats = await _context.PlayerStats
                .AsNoTracking()
                .Where(ps => playerIds.Contains(ps.PlayerId) && typeIds.Contains(ps.PlayerStatTypeId))
                .ToListAsync();

            // Build rows
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
