using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using StrikeData.Data;
using StrikeData.Models;

namespace StrikeData.Pages.TeamData
{
    public class FieldingModel : PageModel
    {
        private readonly AppDbContext _context;

        public FieldingModel(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty(SupportsGet = true)]
        public int? SelectedStatTypeId { get; set; }

        public List<StatType> StatTypes { get; set; } = new();
        public List<TeamStat> TeamStats { get; set; } = new();

        // Opciones estilizadas para el desplegable de estadísticas
        public List<SelectListItem> StatOptions { get; set; } = new();

        // Diccionario Id -> descripción de la estadística (para el panel bajo el select)
        public Dictionary<string, string> StatDescriptions { get; private set; } = new();

        public async Task OnGetAsync()
        {
            // Cargar tipos de estadística de la categoría Fielding
            StatTypes = await _context.StatTypes
                .Include(st => st.StatCategory)
                .Where(st => st.StatCategory != null && st.StatCategory.Name == "Fielding")
                .OrderBy(st => st.Name)
                .ToListAsync();

            // Crear opciones del select y añadir "All"
            StatOptions = StatTypes
                .Select(st => new SelectListItem { Value = st.Id.ToString(), Text = st.Name })
                .ToList();
            StatOptions.Insert(0, new SelectListItem { Value = "", Text = "-- All --" });

            // Construir el diccionario de descripciones por Id
            StatDescriptions.Clear();
            foreach (var st in StatTypes)
            {
                // Abreviatura en mayúsculas para mapear de forma robusta
                string key = st.Name?.ToUpperInvariant() ?? string.Empty;

                string desc = key switch
                {
                    "DP" => "Double plays per game: average number of defensive plays that record two outs in one continuous sequence",
                    "E"  => "Errors per game: average number of misplays that allow a runner to reach or advance when an ordinary effort should have produced an out",
                    _    => ""
                };

                // Guardamos la descripción asociada al Id (que es el value del <select>)
                StatDescriptions[st.Id.ToString()] = desc;
            }

            // Consulta base de TeamStats para Fielding
            var query = _context.TeamStats
                .Include(ts => ts.Team)
                .Include(ts => ts.StatType)
                .Where(ts => ts.StatType.StatCategory != null && ts.StatType.StatCategory.Name == "Fielding")
                .AsQueryable();

            // Filtrado por tipo de estadística si el usuario selecciona uno
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
