using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using StrikeData.Data;
using StrikeData.Models;

namespace StrikeData.Pages.PlayerData
{
    public class FieldingModel : PageModel
    {
        private readonly AppDbContext _context;

        public FieldingModel(AppDbContext context)
        {
            _context = context;
        }

        // Dropdown equipos
        public List<SelectListItem> TeamOptions { get; set; } = new();

        // Filtros (Team)
        [BindProperty(SupportsGet = true)]
        public int SelectedTeamId { get; set; }

        // Columnas visibles
        public List<string> VisibleColumns { get; private set; } = new();

        // Filas de la tabla
        public List<PlayerRow> Rows { get; private set; } = new();

        private static readonly string CategoryName = "Fielding";

        // Columnas pedidas: OUTS, TC, CH, PO, A, E, DP, PB, CASB, CACS, FLD%
        private static readonly List<string> Columns = new()
        {
            "OUTS","TC","CH","PO","A","E","DP","PB","CASB","CACS","FLD%"
        };

        public class PlayerRow
        {
            public int PlayerId { get; set; }
            public int? Number { get; set; }
            public string Name { get; set; } = "";
            public string? Position { get; set; }
            public Dictionary<string, float?> Values { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        }

        public async Task OnGetAsync()
        {
            // Cargar equipos al dropdown
            TeamOptions = await _context.Teams
                .OrderBy(t => t.Name)
                .Select(t => new SelectListItem { Value = t.Id.ToString(), Text = t.Name })
                .ToListAsync();

            if (SelectedTeamId == 0 && TeamOptions.Any())
                SelectedTeamId = int.Parse(TeamOptions.First().Value);

            VisibleColumns = Columns;

            // Jugadores del equipo (TODAS las posiciones para fielding)
            var players = await _context.Players
                .AsNoTracking()
                .Where(p => p.TeamId == SelectedTeamId)
                .OrderBy(p => p.Name)
                .ToListAsync();

            var playerIds = players.Select(p => p.Id).ToList();

            // Buscar PlayerStatTypes SOLO de la categoría Fielding para las columnas visibles
            var fieldingCat = await _context.StatCategories
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Name == CategoryName);

            // Si no hay categoría, no habrá datos (mostrar solo jugadores)
            List<PlayerStatType> statTypes = new();
            if (fieldingCat != null)
            {
                statTypes = await _context.PlayerStatTypes
                    .AsNoTracking()
                    .Where(st => st.StatCategoryId == fieldingCat.Id && VisibleColumns.Contains(st.Name))
                    .ToListAsync();
            }

            var typeIds = statTypes.Select(s => s.Id).ToList();
            var nameByTypeId = statTypes.ToDictionary(s => s.Id, s => s.Name);

            // PlayerStats para esos jugadores y esas métricas (si no hay tipos, no traerá nada)
            var stats = new List<PlayerStat>();
            if (typeIds.Count > 0)
            {
                stats = await _context.PlayerStats
                    .AsNoTracking()
                    .Where(ps => playerIds.Contains(ps.PlayerId) && typeIds.Contains(ps.PlayerStatTypeId))
                    .ToListAsync();
            }

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

                // Mostrar SOLO si tiene al menos un valor de las columnas visibles
                if (row.Values.Values.Any(v => v.HasValue))
                {
                    rows.Add(row);
                }
            }

            Rows = rows;
        }
    }
}
