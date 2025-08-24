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

        public Dictionary<string, StatInfo> StatMeta { get; private set; } = new(StringComparer.OrdinalIgnoreCase);

        public class StatInfo
        {
            public string LongName { get; set; } = "";
            public string Description { get; set; } = "";
        }

        private void InitStatMeta()
        {
            // Básicas
            StatMeta["ERA"] = new StatInfo { LongName = "Earned Run Average", Description = "Carreras merecidas permitidas por cada 9 entradas: (ER×9)/IP." };
            StatMeta["SHO"] = new StatInfo { LongName = "Shutouts", Description = "Partidos completos sin permitir carreras al rival." };
            StatMeta["CG"] = new StatInfo { LongName = "Complete Games", Description = "Juegos completos lanzados por el pitcher titular." };
            StatMeta["SV"] = new StatInfo { LongName = "Saves", Description = "Juegos salvados en situación de salvamento." };
            StatMeta["SVO"] = new StatInfo { LongName = "Save Opportunities", Description = "Oportunidades totales de salvamento." };
            StatMeta["IP"] = new StatInfo { LongName = "Innings Pitched", Description = "Entradas lanzadas (un tercio = 0.1)." };
            StatMeta["H"] = new StatInfo { LongName = "Hits Allowed", Description = "Hits permitidos por el lanzador/equipo." };
            StatMeta["R"] = new StatInfo { LongName = "Runs Allowed", Description = "Carreras recibidas." };
            StatMeta["HR"] = new StatInfo { LongName = "Home Runs Allowed", Description = "Home runs permitidos." };
            StatMeta["W"] = new StatInfo { LongName = "Wins", Description = "Juegos ganados por el lanzador/equipo." };
            StatMeta["SO"] = new StatInfo { LongName = "Strikeouts", Description = "Ponches propinados." };
            StatMeta["WHIP"] = new StatInfo { LongName = "Walks + Hits per Inning Pitched", Description = "Promedio de corredores por entrada: (BB+H)/IP." };
            StatMeta["AVG"] = new StatInfo { LongName = "Batting Average Against", Description = "Promedio de bateo de los rivales: H/(AB vs el lanzador/equipo)." };

            // Avanzadas (ajusta según tus columnas)
            StatMeta["TBF"] = new StatInfo { LongName = "Total Batters Faced", Description = "Bateadores enfrentados." };
            StatMeta["NP"] = new StatInfo { LongName = "Number of Pitches", Description = "Número total de lanzamientos." };
            StatMeta["P/IP"] = new StatInfo { LongName = "Pitches per Inning", Description = "Lanzamientos por entrada." };
            StatMeta["GF"] = new StatInfo { LongName = "Games Finished", Description = "Juegos finalizados por el lanzador." };
            StatMeta["HLD"] = new StatInfo { LongName = "Holds", Description = "Relevista mantiene la ventaja en situación de salvamento." };
            StatMeta["IBB"] = new StatInfo { LongName = "Intentional Walks", Description = "Bases por bolas intencionales." };
            StatMeta["WP"] = new StatInfo { LongName = "Wild Pitches", Description = "Lanzamientos descontrolados que permiten avance." };
            StatMeta["K/BB"] = new StatInfo { LongName = "Strikeout-to-Walk Ratio", Description = "Relación SO/BB." };
            StatMeta["OP/G"] = new StatInfo { LongName = "Opponent Runs per Game", Description = "Carreras recibidas por juego (TeamRankings)." };
            StatMeta["ER/G"] = new StatInfo { LongName = "Earned Runs per Game", Description = "Carreras merecidas por juego (TeamRankings)." };
            StatMeta["SO/9"] = new StatInfo { LongName = "Strikeouts per 9", Description = "Ponches por nueve entradas." };
            StatMeta["H/9"] = new StatInfo { LongName = "Hits per 9", Description = "Hits por nueve entradas." };
            StatMeta["HR/9"] = new StatInfo { LongName = "Home Runs per 9", Description = "Home runs por nueve entradas." };
            StatMeta["W/9"] = new StatInfo { LongName = "Walks per 9", Description = "Bases por bolas por nueve entradas." };
        }

        public class PitchingStatsViewModel
        {
            public string TeamName { get; set; } = string.Empty;
            public int Games { get; set; }
            public Dictionary<string, float?> Stats { get; set; } = new();
        }

        public async Task OnGetAsync()
        {
            InitStatMeta();        // Inicializa las descripciones de estadísticas
            
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

            /*
            // Ordenar por ERA ascendente y luego por nombre de equipo para mayor legibilidad
            TeamPitchingStats = TeamPitchingStats
                .OrderBy(vm => vm.Stats.ContainsKey("ERA") ? vm.Stats["ERA"] ?? float.MaxValue : float.MaxValue)
                .ThenBy(vm => vm.TeamName)
                .ToList();
            */
        }
    }
}
