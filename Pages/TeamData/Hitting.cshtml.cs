using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using StrikeData.Data;
using StrikeData.Models;

namespace StrikeData.Pages.TeamData
{
    public class StatsModel : PageModel
    {
        private readonly AppDbContext _context;

        public StatsModel(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty(SupportsGet = true)]
        public int? SelectedStatTypeId { get; set; }

        public List<StatType> StatTypes { get; set; } = new();
        public List<TeamStat> TeamStats { get; set; } = new();

        public async Task OnGetAsync()
        {
            // Cargar solo los tipos de estadística de la categoría Hitting
            StatTypes = await _context.StatTypes
                .Include(st => st.StatCategory)
                .Where(st => st.StatCategory != null && st.StatCategory.Name == "Hitting")
                .ToListAsync();

            // Construir la consulta inicial de TeamStats únicamente para la categoría Hitting
            var query = _context.TeamStats
                .Include(ts => ts.Team)
                .Include(ts => ts.StatType)
                .Where(ts => ts.StatType.StatCategory != null && ts.StatType.StatCategory.Name == "Hitting")
                .AsQueryable();

            // Si el usuario ha seleccionado un tipo concreto, filtramos por su Id
            if (SelectedStatTypeId.HasValue)
            {
                query = query.Where(ts => ts.StatTypeId == SelectedStatTypeId.Value);
            }

            // Ordenamos y materializamos la lista de estadísticas
            TeamStats = await query
                .OrderByDescending(ts => ts.CurrentSeason) // o Total, según desees
                .ThenBy(ts => ts.Team.Name)
                .ToListAsync();
        }


    }
}
