using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using StrikeData.Data;

namespace StrikeData.Pages.TeamData
{
    public class WinTrendsModel : PageModel
    {
        private readonly AppDbContext _context;

        public WinTrendsModel(AppDbContext context)
        {
            _context = context;
        }

        // Desplegable de tipos de estadística (WinTrends)
        public List<SelectListItem> StatTypeOptions { get; set; } = new();

        // Datos para la tabla
        public List<WinTrendRow> Rows { get; private set; } = new();

        // Selección del usuario
        [BindProperty(SupportsGet = true)]
        public string SelectedStatType { get; set; } = string.Empty;

        public async Task OnGetAsync()
        {
            // Cargar los stat types de la categoría WinTrends
            StatTypeOptions = await _context.StatTypes
                .Where(st => st.StatCategory.Name == "WinTrends")
                .OrderBy(st => st.Name)
                .Select(st => new SelectListItem
                {
                    Value = st.Name,
                    Text = st.Name
                })
                .ToListAsync();

            // Si no hay selección previa, el primero por defecto
            if (string.IsNullOrWhiteSpace(SelectedStatType) && StatTypeOptions.Any())
            {
                SelectedStatType = StatTypeOptions.First().Value;
            }

            // Consulta de TeamStats filtrada por tipo (Perspective fijo Team)
            var query = _context.TeamStats
                .AsNoTracking()
                .Include(ts => ts.Team)
                .Include(ts => ts.StatType)
                .Where(ts =>
                    ts.StatType.StatCategory.Name == "WinTrends" &&
                    ts.StatType.Name == SelectedStatType &&
                    ts.Perspective == Models.Enums.StatPerspective.Team);

            // Ordenar por WinPct desc (nulls al final), luego por nombre de equipo
            var list = await query
                .OrderByDescending(ts => ts.WinPct.HasValue)
                .ThenByDescending(ts => ts.WinPct)
                .ThenBy(ts => ts.Team.Name)
                .Select(ts => new WinTrendRow
                {
                    TeamName = ts.Team.Name,
                    WinLossRecord = ts.WinLossRecord,
                    WinPct = ts.WinPct
                })
                .ToListAsync();

            Rows = list;
        }

        public class WinTrendRow
        {
            public string TeamName { get; set; } = "";
            public string? WinLossRecord { get; set; }
            public float? WinPct { get; set; }
        }
    }
}
