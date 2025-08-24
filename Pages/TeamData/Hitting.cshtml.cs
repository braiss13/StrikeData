using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using StrikeData.Data;
using StrikeData.Models;

namespace StrikeData.Pages.TeamData
{
    public class HittingModel : PageModel
    {
        private readonly AppDbContext _context;

        public HittingModel(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty(SupportsGet = true)]
        public int? SelectedStatTypeId { get; set; }

        public List<StatType> StatTypes { get; set; } = new();
        public List<TeamStat> TeamStats { get; set; } = new();
        // Opciones estilizadas para el desplegable de estadísticas
        public List<SelectListItem> StatOptions { get; set; } = new();

        public async Task OnGetAsync()
        {
            // Cargar tipos de estadística de la categoría Hitting
            StatTypes = await _context.StatTypes
                .Include(st => st.StatCategory)
                .Where(st => st.StatCategory != null && st.StatCategory.Name == "Hitting")
                .OrderBy(st => st.Name)
                .ToListAsync();

            // Convertir a SelectListItem y añadir opción "All"
            StatOptions = StatTypes
                .Select(st => new SelectListItem { Value = st.Id.ToString(), Text = st.Name })
                .ToList();
            StatOptions.Insert(0, new SelectListItem { Value = "", Text = "-- All --" });

            // Construir la consulta de TeamStats filtrando por categoría Hitting
            var query = _context.TeamStats
                .Include(ts => ts.Team)
                .Include(ts => ts.StatType)
                .Where(ts => ts.StatType.StatCategory != null && ts.StatType.StatCategory.Name == "Hitting")
                .AsQueryable();

            // Filtrar por tipo seleccionado si se indica
            if (SelectedStatTypeId.HasValue)
            {
                query = query.Where(ts => ts.StatTypeId == SelectedStatTypeId.Value);
            }

            // Ordenar y materializar resultados
            TeamStats = await query
                .OrderByDescending(ts => ts.CurrentSeason)
                .ThenBy(ts => ts.Team.Name)
                .ToListAsync();
        }
    }
}
