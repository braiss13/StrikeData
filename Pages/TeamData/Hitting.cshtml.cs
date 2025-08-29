using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using StrikeData.Data;
using StrikeData.Models;
using StrikeData.Services.Glossary; 

namespace StrikeData.Pages.TeamData
{
    public class HittingModel : PageModel
    {
        private readonly AppDbContext _context;

        public HittingModel(AppDbContext context)
        {
            _context = context;
        }

        // Propiedad enlazada para el ID de la estadística seleccionada en el desplegable
        [BindProperty(SupportsGet = true)]
        public int? SelectedStatTypeId { get; set; }

        // Lista de tipos de estadística cargados desde la BD
        public List<StatType> StatTypes { get; set; } = new();
        // Lista de estadísticas de equipo que se mostrarán en la tabla
        public List<TeamStat> TeamStats { get; set; } = new();
        // Opciones para el desplegable de estadística, con formato SelectListItem
        public List<SelectListItem> StatOptions { get; set; } = new();
        // Diccionario que asocia el Id de StatType con su descripción (se usa en el panel bajo el select)
        public Dictionary<string, string> StatDescriptions { get; private set; } = new();

        public async Task OnGetAsync()
        {
            // Cargar los tipos de estadística de la categoría Hitting
            StatTypes = await _context.StatTypes
                .Include(st => st.StatCategory)
                .Where(st => st.StatCategory != null && st.StatCategory.Name == "Hitting")
                .OrderBy(st => st.Name)
                .ToListAsync();

            // Rellenar las opciones del desplegable y añadir una opción "All"
            StatOptions = StatTypes
                .Select(st => new SelectListItem { Value = st.Id.ToString(), Text = st.Name })
                .ToList();
            StatOptions.Insert(0, new SelectListItem { Value = "", Text = "-- All --" });

            // ===== Descripciones desde el glosario central =====
            // Usamos el dominio TeamHitting y mapeamos por st.Name (abreviatura)
            var glossary = StatGlossary.GetMap(StatDomain.TeamHitting);
            StatDescriptions = new Dictionary<string, string>();
            foreach (var st in StatTypes)
            {
                // st.Id.ToString() es la clave que usa el <select>; necesitamos dejar la descripción ahí.
                if (glossary.TryGetValue(st.Name, out var statText) && !string.IsNullOrWhiteSpace(statText.Description))
                    StatDescriptions[st.Id.ToString()] = statText.Description;
                else
                    StatDescriptions[st.Id.ToString()] = ""; // fallback si no hay entrada en el glosario
            }
            // ===================================================

            // Consulta base de estadísticas por equipo para la categoría Hitting
            var query = _context.TeamStats
                .Include(ts => ts.Team)
                .Include(ts => ts.StatType)
                .Where(ts => ts.StatType.StatCategory != null && ts.StatType.StatCategory.Name == "Hitting")
                .AsQueryable();

            // Si hay un tipo seleccionado (distinto de All), filtrar por ese Id
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
