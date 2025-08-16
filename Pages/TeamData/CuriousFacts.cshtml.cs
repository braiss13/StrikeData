using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using StrikeData.Data;
using StrikeData.Models;
using StrikeData.Models.Enums;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

        public async Task OnGetAsync()
        {
            // Cargar los stat types de la categoría CuriousFacts
            StatTypeOptions = await _context.StatTypes
                .Where(st => st.StatCategory.Name == "CuriousFacts")
                .OrderBy(st => st.Name)
                .Select(st => new SelectListItem
                {
                    Value = st.Name,   // usamos Name como clave (en tu importer es la abreviatura base: YRFI, 1IR/G, etc.)
                    Text = st.Name     // si más adelante quieres etiquetas más bonitas, cámbialo aquí
                })
                .ToListAsync();

            // Si no hay selección previa, el primero por defecto
            if (string.IsNullOrWhiteSpace(SelectedStatType) && StatTypeOptions.Any())
            {
                SelectedStatType = StatTypeOptions.First().Value;
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
            // EF Core no tiene nulls last nativo en todas las bases, así que ordenamos por "has value" invertido
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
