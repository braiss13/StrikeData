using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using StrikeData.Data;
using StrikeData.Models.Enums;
using StrikeData.Services.Glossary; 

namespace StrikeData.Pages.TeamData
{
    public class CuriousFactsModel : PageModel
    {
        private readonly AppDbContext _context;

        public CuriousFactsModel(AppDbContext context)
        {
            _context = context;
        }

        // Desplegable de tipos de estadística (CuriousFacts)
        public List<SelectListItem> StatTypeOptions { get; set; } = new();

        // Datos para la tabla
        public List<CuriousFactRow> Rows { get; private set; } = new();

        // Selecciones del usuario
        [BindProperty(SupportsGet = true)]
        public string SelectedStatType { get; set; } = string.Empty;

        // "team" o "opp"
        [BindProperty(SupportsGet = true)]
        public string Perspective { get; set; } = "team";

        // Diccionario abreviatura base -> descripción (para el panel bajo el select)
        public Dictionary<string, string> StatDescriptions { get; private set; } = new();

        public async Task OnGetAsync()
        {
            // Cargar los stat types de la categoría CuriousFacts (valores = nombre base sin 'O')
            StatTypeOptions = await _context.StatTypes
                .Where(st => st.StatCategory.Name == "CuriousFacts")
                .OrderBy(st => st.Name)
                .Select(st => new SelectListItem
                {
                    Value = st.Name,   // clave = base (YRFI, 1IR/G, F5IR/G, etc.)
                    Text = st.Name
                })
                .ToListAsync();

            // Si no hay selección previa, el primero por defecto
            if (string.IsNullOrWhiteSpace(SelectedStatType) && StatTypeOptions.Any())
            {
                SelectedStatType = StatTypeOptions.First().Value;
            }

            // ---- DESCRIPCIONES DESDE GLOSARIO CENTRAL ----
            // Cargamos el mapa del dominio CuriousFacts y construimos solo para las claves presentes en el select
            var glossary = StatGlossary.GetMap(StatDomain.CuriousFacts);
            StatDescriptions.Clear();
            foreach (var opt in StatTypeOptions)
            {
                var key = opt.Value; // p.ej. "YRFI", "1IR/G", "F5IR/G"
                if (glossary.TryGetValue(key, out var st) && !string.IsNullOrWhiteSpace(st.Description))
                    StatDescriptions[key] = st.Description;
                else
                    StatDescriptions[key] = "";
            }

            // Normaliza perspectiva
            var desiredPerspective = Perspective?.ToLowerInvariant() == "opp"
                ? StatPerspective.Opponent
                : StatPerspective.Team;

            // Consulta de TeamStats filtrada por tipo + perspectiva, incluyendo Team
            var query = _context.TeamStats
                .AsNoTracking()
                .Include(ts => ts.Team)
                .Include(ts => ts.StatType)
                .Where(ts =>
                    ts.StatType.StatCategory.Name == "CuriousFacts" &&
                    ts.StatType.Name == SelectedStatType &&
                    ts.Perspective == desiredPerspective);

            // Orden por CurrentSeason desc (nulls al final), y luego por nombre de equipo
            var list = await query
                .OrderByDescending(ts => ts.CurrentSeason.HasValue)
                .ThenByDescending(ts => ts.CurrentSeason)
                .ThenBy(ts => ts.Team.Name)
                .Select(ts => new CuriousFactRow
                {
                    TeamName = ts.Team.Name,
                    CurrentSeason = ts.CurrentSeason,
                    Last3Games = ts.Last3Games,
                    LastGame = ts.LastGame,
                    Home = ts.Home,
                    Away = ts.Away,
                    PrevSeason = ts.PrevSeason
                })
                .ToListAsync();

            Rows = list;
        }

        public class CuriousFactRow
        {
            public string TeamName { get; set; } = "";
            public float? CurrentSeason { get; set; }
            public float? Last3Games { get; set; }
            public float? LastGame { get; set; }
            public float? Home { get; set; }
            public float? Away { get; set; }
            public float? PrevSeason { get; set; }
        }
    }
}
