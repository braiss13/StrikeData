using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using StrikeData.Data;
using StrikeData.Models;

namespace StrikeData.Pages.TeamData
{
    public class ScheduleModel : PageModel
    {
        private readonly AppDbContext _context;

        public ScheduleModel(AppDbContext context)
        {
            _context = context;
        }

        // Lista de equipos para el desplegable
        public List<SelectListItem> TeamOptions { get; set; } = new();

        // Propiedades para vincular el equipo y el modo seleccionados
        [BindProperty(SupportsGet = true)]
        public int SelectedTeamId { get; set; }

        [BindProperty(SupportsGet = true)]
        public string ViewMode { get; set; } = "expanded";

        // Datos que se mostrarán en la página
        public List<TeamGame> Schedule { get; private set; } = new();
        public List<TeamMonthlySplit> MonthlySplits { get; private set; } = new();
        public List<TeamOpponentSplit> TeamSplits { get; private set; } = new();

        public async Task OnGetAsync()
        {
            // Cargar equipos ordenados para el desplegable
            TeamOptions = await _context.Teams
                .OrderBy(t => t.Name)
                .Select(t => new SelectListItem { Value = t.Id.ToString(), Text = t.Name })
                .ToListAsync();

            // Si no se ha seleccionado un equipo, por defecto el primero
            if (SelectedTeamId == 0 && TeamOptions.Any())
            {
                SelectedTeamId = int.Parse(TeamOptions.First().Value);
            }

            // Filtrar por temporada actual (2025) si ya has importado esa temporada
            int season = 2025;

            // Cargar datos desde la base de datos para el equipo seleccionado
            Schedule = await _context.TeamGames
                .Where(g => g.TeamId == SelectedTeamId && g.Season == season)
                .OrderBy(g => g.GameNumber)
                .ToListAsync();

            MonthlySplits = await _context.TeamMonthlySplits
                .Where(s => s.TeamId == SelectedTeamId && s.Season == season)
                .OrderBy(s => s.Month)
                .ToListAsync();

            TeamSplits = await _context.TeamOpponentSplits
                .Where(s => s.TeamId == SelectedTeamId && s.Season == season)
                .OrderBy(s => s.OpponentName)
                .ToListAsync();
        }
    }
}
