using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using StrikeData.Data;
using StrikeData.Services.Glossary; 

namespace StrikeData.Pages.TeamData
{
    /*
        PageModel for the team-level Pitching view.
        Exposes two sets of abbreviations (Basic and Advanced), loads the corresponding
        values for each team, and provides glossary metadata used by the Razor view.
    */
    public class PitchingModel : PageModel
    {
        private readonly AppDbContext _context;

        public PitchingModel(AppDbContext context)
        {
            _context = context;
        }

        // Abbreviation lists for the two views. Order defines column order in the table.
        public List<string> BasicStatNames { get; private set; } = new();
        public List<string> AdvancedStatNames { get; private set; } = new();

        // One view model per team (team name, games, and a map of stat values).
        public List<PitchingStatsViewModel> TeamPitchingStats { get; private set; } = new();

        // Tooltip metadata keyed by abbreviation (long name + description).
        public Dictionary<string, StatInfo> StatMeta { get; private set; } = new(StringComparer.OrdinalIgnoreCase);

        // Optional diagnostics used during development (e.g., counts/first row).
        public string DebugInfo { get; private set; } = string.Empty;

        public class StatInfo
        {
            public string LongName { get; set; } = "";
            public string Description { get; set; } = "";
        }

        /*
            Fills StatMeta using the central glossary (TeamPitching) for all abbreviations
            present in BasicStatNames and AdvancedStatNames.
        */
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
                    // Fallback when an abbreviation is not present in the glossary.
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
            // 1) Define which abbreviations will be display in each table
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

            // 2) Load glossary metadata for tooltips
            InitStatMeta();

            // 3) Reset the list used by the view
            TeamPitchingStats.Clear();

            // 4) Build a map (abbr -> StatTypeId) for all Pitching StatTypes
            var statTypeMap = await _context.StatTypes
                .Include(st => st.StatCategory)
                .Where(st => st.StatCategory != null && st.StatCategory.Name == "Pitching")
                .ToDictionaryAsync(st => st.Name, st => st.Id);

            // 5) Load all TeamStats for those StatTypes (includes totals and TR splits)
            var pitchingStats = await _context.TeamStats
                .Include(ts => ts.StatType)
                .Include(ts => ts.Team)
                .Where(ts => statTypeMap.Values.Contains(ts.StatTypeId))
                .ToListAsync();

            // 6) Load teams (name and number of games)
            var teams = await _context.Teams.ToListAsync();

            // 7) Compose one view model per team with values for all requested abbreviations
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
                            /*
                                Convention:
                                - MLB values are season aggregates -> stored in Total
                                - TeamRankings values are per-game -> stored in CurrentSeason
                            */
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
