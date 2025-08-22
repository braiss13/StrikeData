using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using StrikeData.Data;
using StrikeData.Models;

namespace StrikeData.Pages.PlayerData
{
    public class HittingPlayerModel : PageModel
    {
        private readonly AppDbContext _context;

        public HittingPlayerModel(AppDbContext context)
        {
            _context = context;
        }

        // Dropdown equipos
        public List<SelectListItem> TeamOptions { get; set; } = new();

        // Bindings
        [BindProperty(SupportsGet = true)]
        public int SelectedTeamId { get; set; }

        [BindProperty(SupportsGet = true)]
        public string ViewMode { get; set; } = "basic"; // "basic" | "advanced"

        // Columnas visibles según modo
        public List<string> VisibleColumns { get; private set; } = new();

        // Filas
        public List<PlayerRow> Rows { get; private set; } = new();

        private static readonly string CategoryName = "Hitting";

        private static readonly List<string> BasicCols = new()
        {
            "G","AB","R","H","2B","3B","HR","RBI","BB","SO","SB","CS","AVG","OBP","SLG","OPS"
        };

        private static readonly List<string> AdvancedCols = new()
        {
            "PA","HBP","SAC","SF","GIDP","GO/AO","XBH","TB","IBB","BABIP","ISO","AB/HR","BB/K","BB%","SO%","HR%"
        };

        public class PlayerRow
        {
            public int PlayerId { get; set; }
            public int? Number { get; set; }
            public string Name { get; set; } = "";
            public string? Position { get; set; }
            public string? Status { get; set; }
            public Dictionary<string, float?> Values { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        }

        public async Task OnGetAsync()
        {
            // Equipos para dropdown
            TeamOptions = await _context.Teams
                .OrderBy(t => t.Name)
                .Select(t => new SelectListItem { Value = t.Id.ToString(), Text = t.Name })
                .ToListAsync();

            if (SelectedTeamId == 0 && TeamOptions.Any())
                SelectedTeamId = int.Parse(TeamOptions.First().Value);

            VisibleColumns = (ViewMode?.ToLowerInvariant() == "advanced") ? AdvancedCols : BasicCols;

            // Jugadores HITTING: posición != "P"
            var players = await _context.Players
                .AsNoTracking()
                .Where(p => p.TeamId == SelectedTeamId && (p.Position == null || p.Position != "P"))
                .OrderBy(p => p.Name)
                .ToListAsync();

            var playerIds = players.Select(p => p.Id).ToList();

            // StatTypes por nombre (en esta categoría)
            var statTypes = await _context.PlayerStatTypes
                .AsNoTracking()
                .Where(st => st.StatCategory.Name == CategoryName && VisibleColumns.Contains(st.Name))
                .ToListAsync();

            var typeIds = statTypes.Select(s => s.Id).ToList();
            var nameByTypeId = statTypes.ToDictionary(s => s.Id, s => s.Name);

            // PlayerStats de esos jugadores y esas métricas
            var stats = await _context.PlayerStats
                .AsNoTracking()
                .Where(ps => playerIds.Contains(ps.PlayerId) && typeIds.Contains(ps.PlayerStatTypeId))
                .ToListAsync();

            // Montar filas
            var rows = new List<PlayerRow>();
            var byPlayer = stats.GroupBy(s => s.PlayerId).ToDictionary(g => g.Key, g => g.ToList());

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
