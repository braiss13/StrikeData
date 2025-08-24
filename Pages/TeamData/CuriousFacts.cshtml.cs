using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using StrikeData.Data;
using StrikeData.Models.Enums;

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

            // Construir descripciones (base key -> texto)
            BuildStatDescriptions();

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

        private void BuildStatDescriptions()
        {
            StatDescriptions.Clear();

            // Helper local para registrar descripciones
            void Add(string key, string desc)
            {
                // Evita excepciones si hubiera duplicados por cualquier motivo
                if (!StatDescriptions.ContainsKey(key))
                    StatDescriptions[key] = desc;
            }

            // Porcentajes Y/N Run 1st Inning
            Add("YRFI",  "Yes Run First Inning %: share of games with at least one run scored in the 1st inning.");
            Add("NRFI",  "No Run First Inning %: share of games with zero runs scored in the 1st inning.");

            // Inning-specific runs per game (team base key; la perspectiva se indica en el UI)
            Add("1IR/G", "1st-inning runs per game: average runs scored in the 1st inning.");
            Add("2IR/G", "2nd-inning runs per game: average runs scored in the 2nd inning.");
            Add("3IR/G", "3rd-inning runs per game: average runs scored in the 3rd inning.");
            Add("4IR/G", "4th-inning runs per game: average runs scored in the 4th inning.");
            Add("5IR/G", "5th-inning runs per game: average runs scored in the 5th inning.");
            Add("6IR/G", "6th-inning runs per game: average runs scored in the 6th inning.");
            Add("7IR/G", "7th-inning runs per game: average runs scored in the 7th inning.");
            Add("8IR/G", "8th-inning runs per game: average runs scored in the 8th inning.");
            Add("9IR/G", "9th-inning runs per game: average runs scored in the 9th inning.");
            Add("XTRAIR/G", "Extra-innings runs per game: average runs scored in extra innings.");

            // First N innings aggregates
            Add("F4IR/G", "First 4 innings runs per game: average runs scored across innings 1–4.");
            Add("F5IR/G", "First 5 innings runs per game: average runs scored across innings 1–5.");
            Add("F6IR/G", "First 6 innings runs per game: average runs scored across innings 1–6.");

            // Last N innings aggregates (regulation)
            Add("L2IR/G", "Last 2 innings runs per game: average runs scored across the final two regulation innings (typically 8–9).");
            Add("L3IR/G", "Last 3 innings runs per game: average runs scored across the final three regulation innings (typically 7–9).");
            Add("L4IR/G", "Last 4 innings runs per game: average runs scored across the final four regulation innings (typically 6–9).");

            // NOTA: Las variantes de oponente (OYRFI, O1IR/G, OF4IR/G, etc.) usan el MISMO nombre base
            // en StatType (sin la 'O'). La diferencia de significado (anotar vs permitir) se indica
            // mediante la 'Perspective' seleccionada y se añade en el UI ("— Perspective: Opponents/Team Itself").
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
