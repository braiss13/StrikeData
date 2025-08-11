using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using StrikeData.Data;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

        public class PitchingStatsViewModel
        {
            public string TeamName { get; set; } = string.Empty;
            public int Games { get; set; }
            public Dictionary<string, float?> Stats { get; set; } = new();
        }

        public async Task OnGetAsync()
        {
            // Abreviaturas de MLB básicas
            BasicStatNames = new List<string>
            {
                "ERA", "SHO", "CG", "SV", "SVO", "IP",
                "H", "R", "HR", "W", "SO", "WHIP", "AVG"
            };

            // Resto de abreviaturas MLB + las de TeamRankings (avanzadas)
            AdvancedStatNames = new List<string>
            {
                "TBF", "NP", "P/IP", "GF", "HLD", "IBB", "WP", "K/BB",
                "OP/G", "ER/G", "SO/9", "H/9", "HR/9", "W/9"
            };

            // Obtener StatTypes de la categoría Pitching
            var statTypeMap = await _context.StatTypes
                .Include(st => st.StatCategory)
                .Where(st => st.StatCategory != null && st.StatCategory.Name == "Pitching")
                .ToDictionaryAsync(st => st.Name, st => st.Id);

            // Cargar TeamStats asociados a Pitching
            var pitchingStats = await _context.TeamStats
                .Include(ts => ts.StatType)
                .Include(ts => ts.Team)
                .Where(ts => statTypeMap.Values.Contains(ts.StatTypeId))
                .ToListAsync();

            // Cargar todos los equipos
            var teams = await _context.Teams.ToListAsync();

            // Construir un view model por equipo
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
                            // Total para MLB, CurrentSeason para TeamRankings
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

            // Ordenar por ERA ascendente y luego por nombre de equipo para mayor legibilidad
            TeamPitchingStats = TeamPitchingStats
                .OrderBy(vm => vm.Stats.ContainsKey("ERA") ? vm.Stats["ERA"] ?? float.MaxValue : float.MaxValue)
                .ThenBy(vm => vm.TeamName)
                .ToList();
        }
    }
}
