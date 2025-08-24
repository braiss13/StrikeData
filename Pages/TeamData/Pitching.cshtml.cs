using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using StrikeData.Data;

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
            // Basic statistics (English names and explanatory descriptions)
            StatMeta["ERA"] = new StatInfo
            {
                LongName = "Earned Run Average",
                Description = "Earned run average: earned runs allowed per nine innings pitched ((earned runs × 9) / innings pitched)."
            };
            StatMeta["SHO"] = new StatInfo
            {
                LongName = "Shutouts",
                Description = "Shutouts: complete games where no runs are allowed by the pitcher or team."
            };
            StatMeta["CG"] = new StatInfo
            {
                LongName = "Complete Games",
                Description = "Complete games: games in which the starting pitcher pitches the entire game without relief."
            };
            StatMeta["SV"] = new StatInfo
            {
                LongName = "Saves",
                Description = "Saves: relief appearances that preserve a lead while meeting the save criteria."
            };
            StatMeta["SVO"] = new StatInfo
            {
                LongName = "Save Opportunities",
                Description = "Save opportunities: total chances a pitcher has to earn a save (regardless of outcome)."
            };
            StatMeta["IP"] = new StatInfo
            {
                LongName = "Innings Pitched",
                Description = "Innings pitched: total innings thrown; each out equals one third of an inning."
            };
            StatMeta["H"] = new StatInfo
            {
                LongName = "Hits Allowed",
                Description = "Hits allowed: total hits conceded to opposing batters. A hit occurs when a batter reaches at least first base safely after putting the ball in play, without an error or fielder's choice."
            };
            StatMeta["R"] = new StatInfo
            {
                LongName = "Runs Allowed",
                Description = "Runs allowed: total runs (earned and unearned) given up by the pitcher or team. A run scores when a runner safely circles the bases and touches home plate."
            };
            StatMeta["HR"] = new StatInfo
            {
                LongName = "Home Runs Allowed",
                Description = "Home runs allowed: number of home runs conceded to opponents. A home run occurs when a batted ball in fair territory clears the outfield fence or the batter circles all the bases on an inside-the-park hit."
            };
            StatMeta["W"] = new StatInfo
            {
                LongName = "Wins",
                Description = "Wins: games credited as wins to the pitcher or team."
            };
            StatMeta["SO"] = new StatInfo
            {
                LongName = "Strikeouts",
                Description = "Strikeouts: number of batters retired via strike three."
            };
            StatMeta["WHIP"] = new StatInfo
            {
                LongName = "Walks + Hits per Inning Pitched",
                Description = "Walks plus hits per inning pitched: (walks + hits) divided by innings pitched; measures baserunners allowed."
            };
            StatMeta["AVG"] = new StatInfo
            {
                LongName = "Batting Average Against",
                Description = "Batting average against: opponents' batting average; hits allowed divided by at-bats against."
            };

            // Advanced statistics (English names and explanations)
            StatMeta["TBF"] = new StatInfo
            {
                LongName = "Total Batters Faced",
                Description = "Total batters faced: number of plate appearances against the pitcher or team."
            };
            StatMeta["NP"] = new StatInfo
            {
                LongName = "Number of Pitches",
                Description = "Number of pitches: total pitches thrown (balls and strikes)."
            };
            StatMeta["P/IP"] = new StatInfo
            {
                LongName = "Pitches per Inning",
                Description = "Pitches per inning: average number of pitches thrown per inning pitched."
            };
            StatMeta["GF"] = new StatInfo
            {
                LongName = "Games Finished",
                Description = "Games finished: appearances where the pitcher recorded the final out for his team."
            };
            StatMeta["HLD"] = new StatInfo
            {
                LongName = "Holds",
                Description = "Holds: relief outings where the pitcher enters in a save situation, records at least one out and leaves with the lead intact."
            };
            StatMeta["IBB"] = new StatInfo
            {
                LongName = "Intentional Walks",
                Description = "Intentional walks: walks issued intentionally by the pitcher."
            };
            StatMeta["WP"] = new StatInfo
            {
                LongName = "Wild Pitches",
                Description = "Wild pitches: errant pitches that allow baserunners to advance."
            };
            StatMeta["K/BB"] = new StatInfo
            {
                LongName = "Strikeout-to-Walk Ratio",
                Description = "Strikeout-to-walk ratio: strikeouts divided by walks."
            };
            StatMeta["OP/G"] = new StatInfo
            {
                LongName = "Opponent Runs per Game",
                Description = "Opponent runs per game: average runs allowed per game (TeamRankings data)."
            };
            StatMeta["ER/G"] = new StatInfo
            {
                LongName = "Earned Runs per Game",
                Description = "Earned runs per game: average earned runs allowed per game (TeamRankings data)."
            };
            StatMeta["SO/9"] = new StatInfo
            {
                LongName = "Strikeouts per 9",
                Description = "Strikeouts per nine innings: (strikeouts × 9) / innings pitched."
            };
            StatMeta["H/9"] = new StatInfo
            {
                LongName = "Hits per 9",
                Description = "Hits per nine innings: (hits allowed × 9) / innings pitched."
            };
            StatMeta["HR/9"] = new StatInfo
            {
                LongName = "Home Runs per 9",
                Description = "Home runs per nine innings: (home runs allowed × 9) / innings pitched."
            };
            StatMeta["W/9"] = new StatInfo
            {
                LongName = "Walks per 9",
                Description = "Walks per nine innings: (walks × 9) / innings pitched."
            };
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

        }
    }
}
