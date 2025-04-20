using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using StrikeData.Data;
using StrikeData.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StrikeData.Pages.TeamData
{
    public class StatsModel : PageModel
    {
        private readonly AppDbContext _context;

        public StatsModel(AppDbContext context)
        {
            _context = context;
        }

        // Este es el valor seleccionado en el dropdown (desplegable)
        [BindProperty(SupportsGet = true)]
        public int? SelectedStatTypeId { get; set; }

        // Todos los tipos de estadísticas disponibles para el desplegable
        public List<StatType> StatTypes { get; set; } = new();

        // Todas las estadísticas de equipos que se mostrarán en la tabla
        public List<TeamStat> TeamStats { get; set; } = new();

        public async Task OnGetAsync()
        {
            // Cargar todos los tipos para el dropdown
            StatTypes = await _context.StatTypes.ToListAsync();

            // Query base
            var query = _context.TeamStats
                .Include(ts => ts.Team)
                .Include(ts => ts.StatType)
                .AsQueryable();

            // Filtrar por el tipo seleccionado si hay uno
            if (SelectedStatTypeId.HasValue)
            {
                query = query.Where(ts => ts.StatTypeId == SelectedStatTypeId.Value);
            }

            TeamStats = await query.ToListAsync();
        }
    }
}
