using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using StrikeData.Data;
using StrikeData.Services.Glossary; 

namespace StrikeData.Pages.TeamData
{
    public class PitchingModel : PageModel
    {
        private readonly AppDbContext _context;

        public PitchingModel(AppDbContext context)
        {
            _context = context;
        }

        // Listas de abreviaturas según el tipo de vista
        public List<string> BasicStatNames { get; private set; } = new();
        public List<string> AdvancedStatNames { get; private set; } = new();

        // Lista de view models para la tabla
        public List<PitchingStatsViewModel> TeamPitchingStats { get; private set; } = new();

        public Dictionary<string, StatInfo> StatMeta { get; private set; } = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Contiene información de depuración sobre el número de registros recuperados
        /// y el contenido de la primera fila. Se puede mostrar en la vista para
        /// diagnosticar por qué la tabla aparece vacía.
        /// </summary>
        public string DebugInfo { get; private set; } = string.Empty;

        public class StatInfo
        {
            public string LongName { get; set; } = "";
            public string Description { get; set; } = "";
        }

        /// <summary>
        /// Rellena StatMeta usando el glosario central (TeamPitching) para todas las
        /// abreviaturas definidas en BasicStatNames y AdvancedStatNames.
        /// </summary>
        private void InitStatMeta()
        {
            StatMeta.Clear();

            var glossary = StatGlossary.GetMap(StatDomain.TeamPitching);

            foreach (var abbr in BasicStatNames.Concat(AdvancedStatNames))
            {
                if (glossary.TryGetValue(abbr, out var st))
                {
                    StatMeta[abbr] = new StatInfo
                    {
                        LongName = st.LongName,
                        Description = st.Description
                    };
                }
                else
                {
                    // Fallback por si alguna clave no estuviera en el glosario
                    StatMeta[abbr] = new StatInfo
                    {
                        LongName = abbr,
                        Description = ""
                    };
                }
            }
        }

        public class PitchingStatsViewModel
        {
            public string TeamName { get; set; } = string.Empty;
            public int Games { get; set; }
            public Dictionary<string, float?> Stats { get; set; } = new();
        }

        public async Task OnGetAsync()
        {
            // 1) Define las abreviaturas que mostrarán las tablas
            BasicStatNames = new List<string>
            {
                "ERA", "SHO", "CG", "SV", "SVO", "IP",
                "H", "R", "HR", "W", "SO", "WHIP", "AVG"
            };

            AdvancedStatNames = new List<string>
            {
                "TBF", "NP", "P/IP", "GF", "HLD", "IBB", "WP", "K/BB",
                "OP/G", "ER/G", "SO/9", "H/9", "HR/9", "W/9"
            };

            // 2) Carga definiciones (tooltips) desde el glosario
            InitStatMeta();

            // 3) Reiniciar la lista de estadísticas por equipo
            TeamPitchingStats.Clear();

            // 4) Obtener StatTypes de la categoría Pitching
            var statTypeMap = await _context.StatTypes
                .Include(st => st.StatCategory)
                .Where(st => st.StatCategory != null && st.StatCategory.Name == "Pitching")
                .ToDictionaryAsync(st => st.Name, st => st.Id);

            // 5) Cargar TeamStats asociados a Pitching
            var pitchingStats = await _context.TeamStats
                .Include(ts => ts.StatType)
                .Include(ts => ts.Team)
                .Where(ts => statTypeMap.Values.Contains(ts.StatTypeId))
                .ToListAsync();

            // 6) Cargar todos los equipos
            var teams = await _context.Teams.ToListAsync();

            // 7) Construir un view model por equipo
            foreach (var team in teams)
            {
                var vm = new PitchingStatsViewModel
                {
                    TeamName = team.Name,
                    Games = team.Games ?? 0
                };

                var statsDict = new Dictionary<string, float?>();
                foreach (var statName in BasicStatNames.Concat(AdvancedStatNames))
                {
                    if (statTypeMap.TryGetValue(statName, out var statTypeId))
                    {
                        var ts = pitchingStats.FirstOrDefault(s => s.TeamId == team.Id && s.StatTypeId == statTypeId);
                        float? value = null;
                        if (ts != null)
                        {
                            // Total para MLB, CurrentSeason para TeamRankings (fallback)
                            value = ts.Total ?? ts.CurrentSeason;
                        }
                        statsDict[statName] = value;
                    }
                    else
                    {
                        statsDict[statName] = null;
                    }
                }
                vm.Stats = statsDict;
                TeamPitchingStats.Add(vm);
            }
        }
    }
}
