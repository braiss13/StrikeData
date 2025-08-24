using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using StrikeData.Data;

namespace StrikeData.Pages.PlayerData
{
    /// <summary>
    /// PageModel for the player pitching statistics page. This model loads the pitching
    /// statistics for all pitchers on a selected team and exposes them to the Razor
    /// view. It also provides metadata for each statistic so the view can show
    /// descriptive tooltips when the user hovers over a stat abbreviation.
    /// </summary>
    public class PitchingPlayerModel : PageModel
    {
        private readonly AppDbContext _context;

        public PitchingPlayerModel(AppDbContext context)
        {
            _context = context;
        }

        // Dropdown list of teams
        public List<SelectListItem> TeamOptions { get; set; } = new();

        // Bindings
        [BindProperty(SupportsGet = true)]
        public int SelectedTeamId { get; set; }

        [BindProperty(SupportsGet = true)]
        public string ViewMode { get; set; } = "basic"; // "basic" | "advanced"

        // Columns visible in the current view
        public List<string> VisibleColumns { get; private set; } = new();

        // Rows of statistics for display
        public List<PlayerRow> Rows { get; private set; } = new();

        private static readonly string CategoryName = "Pitching";

        // Basic and advanced pitching metrics. These lists define the order and which
        // statistics appear in the Basic and Advanced views. The same abbreviations
        // are used as keys in the StatMeta dictionary.
        private static readonly List<string> BasicCols = new()
        {
            "W","L","ERA","G","GS","CG","SHO","SV","SVO","IP","R","H","ER","HR","HB","BB","SO","WHIP","AVG"
        };

        private static readonly List<string> AdvancedCols = new()
        {
            "TBF","NP","P/IP","QS","GF","HLD","IBB","WP","BK","GDP","GO/AO","SO/9","BB/9","H/9","K/BB","BABIP","SB","CS","PK"
        };

        // Metadata for each statistic: long name and description
        public class StatInfo
        {
            public string LongName { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
        }

        public Dictionary<string, StatInfo> StatMeta { get; private set; } = new(StringComparer.OrdinalIgnoreCase);

        // Representation of a player's pitching statistics for the table
        public class PlayerRow
        {
            public int PlayerId { get; set; }
            public int? Number { get; set; }
            public string Name { get; set; } = string.Empty;
            public string? Position { get; set; }
            public string? Status { get; set; }
            public Dictionary<string, float?> Values { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Initializes the StatMeta dictionary with long names and descriptions
        /// for each pitching statistic. This should be called before the view
        /// renders so tooltips can be generated.
        /// </summary>
        private void InitStatMeta()
        {
            StatMeta.Clear();
            // Status explanation
            StatMeta["Status"] = new StatInfo
            {
                LongName = "Player Status",
                Description = "A => Active; RM => Reassigned to Minors; D[n] => Days Injured (n = number of days)."
            };
            // Basic statistics definitions
            StatMeta["W"] = new StatInfo
            {
                LongName = "Wins",
                Description = "Number of games credited as wins to the pitcher or team."
            };
            StatMeta["L"] = new StatInfo
            {
                LongName = "Losses",
                Description = "Number of games credited as losses to the pitcher or team."
            };
            StatMeta["ERA"] = new StatInfo
            {
                LongName = "Earned Run Average",
                Description = "Earned run average: earned runs allowed per nine innings pitched ((earned runs × 9) / innings pitched)."
            };
            StatMeta["G"] = new StatInfo
            {
                LongName = "Games",
                Description = "Number of games in which the pitcher appeared."
            };
            StatMeta["GS"] = new StatInfo
            {
                LongName = "Games Started",
                Description = "Number of games started by the pitcher."
            };
            StatMeta["CG"] = new StatInfo
            {
                LongName = "Complete Games",
                Description = "Number of games in which the pitcher threw the entire game without relief."
            };
            StatMeta["SHO"] = new StatInfo
            {
                LongName = "Shutouts",
                Description = "Complete games where the pitcher allowed no runs."
            };
            StatMeta["SV"] = new StatInfo
            {
                LongName = "Saves",
                Description = "Relief appearances that preserve a lead while meeting save criteria."
            };
            StatMeta["SVO"] = new StatInfo
            {
                LongName = "Save Opportunities",
                Description = "Total opportunities the pitcher has to earn a save, regardless of outcome."
            };
            StatMeta["IP"] = new StatInfo
            {
                LongName = "Innings Pitched",
                Description = "Total innings thrown; each out counts as one third of an inning."
            };
            StatMeta["R"] = new StatInfo
            {
                LongName = "Runs Allowed",
                Description = "Total runs (earned and unearned) given up by the pitcher or team."
            };
            StatMeta["H"] = new StatInfo
            {
                LongName = "Hits Allowed",
                Description = "Number of hits conceded to opposing batters. A hit occurs when a batter reaches at least first base safely on a fair ball without an error or fielder's choice."
            };
            StatMeta["ER"] = new StatInfo
            {
                LongName = "Earned Runs",
                Description = "Number of earned runs allowed by the pitcher. Earned runs exclude those that score due to errors or passed balls."
            };
            StatMeta["HR"] = new StatInfo
            {
                LongName = "Home Runs Allowed",
                Description = "Number of home runs conceded. A home run occurs when a batted ball allows the batter to round all bases in one play."
            };
            StatMeta["HB"] = new StatInfo
            {
                LongName = "Hit Batsmen",
                Description = "Number of times the pitcher hits a batter with a pitched ball, awarding first base."
            };
            StatMeta["BB"] = new StatInfo
            {
                LongName = "Walks",
                Description = "Number of bases on balls issued: times the pitcher throws four balls, allowing the batter to walk to first base."
            };
            StatMeta["SO"] = new StatInfo
            {
                LongName = "Strikeouts",
                Description = "Number of batters retired via strike three."
            };
            StatMeta["WHIP"] = new StatInfo
            {
                LongName = "Walks + Hits per Inning Pitched",
                Description = "Walks plus hits per inning pitched: (walks + hits) divided by innings pitched; measures baserunners allowed."
            };
            StatMeta["AVG"] = new StatInfo
            {
                LongName = "Batting Average Against",
                Description = "Opponents' batting average: hits allowed divided by at-bats against."
            };
            // Advanced statistics definitions
            StatMeta["TBF"] = new StatInfo
            {
                LongName = "Total Batters Faced",
                Description = "Number of batters faced by the pitcher."
            };
            StatMeta["NP"] = new StatInfo
            {
                LongName = "Number of Pitches",
                Description = "Total pitches thrown, including balls and strikes."
            };
            StatMeta["P/IP"] = new StatInfo
            {
                LongName = "Pitches per Inning",
                Description = "Average number of pitches thrown per inning pitched."
            };
            StatMeta["QS"] = new StatInfo
            {
                LongName = "Quality Starts",
                Description = "Number of starts where the pitcher pitches at least six innings and allows three or fewer earned runs."
            };
            StatMeta["GF"] = new StatInfo
            {
                LongName = "Games Finished",
                Description = "Number of games in which the pitcher recorded the final out."
            };
            StatMeta["HLD"] = new StatInfo
            {
                LongName = "Holds",
                Description = "Relief outings where the pitcher enters in a save situation, records at least one out and leaves with the lead intact."
            };
            StatMeta["IBB"] = new StatInfo
            {
                LongName = "Intentional Walks",
                Description = "Number of intentional bases on balls issued by the pitcher."
            };
            StatMeta["WP"] = new StatInfo
            {
                LongName = "Wild Pitches",
                Description = "Number of pitches so errant that a baserunner advances and it is not scored as a passed ball."
            };
            StatMeta["BK"] = new StatInfo
            {
                LongName = "Balks",
                Description = "Number of illegal pitching motions or actions that allow baserunners to advance."
            };
            StatMeta["GDP"] = new StatInfo
            {
                LongName = "Grounded into Double Play",
                Description = "Number of opponent ground balls off the pitcher that result in a double play."
            };
            StatMeta["GO/AO"] = new StatInfo
            {
                LongName = "Groundouts to Airouts Ratio",
                Description = "Ratio of outs on ground balls to outs on fly balls induced by the pitcher."
            };
            StatMeta["SO/9"] = new StatInfo
            {
                LongName = "Strikeouts per 9",
                Description = "Strikeouts per nine innings: (strikeouts × 9) divided by innings pitched."
            };
            StatMeta["BB/9"] = new StatInfo
            {
                LongName = "Walks per 9",
                Description = "Walks per nine innings: (walks × 9) divided by innings pitched."
            };
            StatMeta["H/9"] = new StatInfo
            {
                LongName = "Hits per 9",
                Description = "Hits allowed per nine innings: (hits allowed × 9) divided by innings pitched."
            };
            StatMeta["K/BB"] = new StatInfo
            {
                LongName = "Strikeout-to-Walk Ratio",
                Description = "Strikeouts divided by walks (K/BB)."
            };
            StatMeta["BABIP"] = new StatInfo
            {
                LongName = "Batting Average on Balls in Play",
                Description = "Average on balls put in play excluding home runs: (hits − home runs) / (at-bats − strikeouts − home runs + sacrifice flies)."
            };
            StatMeta["SB"] = new StatInfo
            {
                LongName = "Stolen Bases Allowed",
                Description = "Number of stolen bases allowed while the pitcher is on the mound."
            };
            StatMeta["CS"] = new StatInfo
            {
                LongName = "Caught Stealing",
                Description = "Number of baserunners caught stealing while the pitcher is on the mound."
            };
            StatMeta["PK"] = new StatInfo
            {
                LongName = "Pickoffs",
                Description = "Number of times the pitcher picks off a baserunner."
            };
        }

        public async Task OnGetAsync()
        {
            // Load statistic definitions
            InitStatMeta();

            // Populate the team dropdown
            TeamOptions = await _context.Teams
                .OrderBy(t => t.Name)
                .Select(t => new SelectListItem { Value = t.Id.ToString(), Text = t.Name })
                .ToListAsync();

            // Default to first team if none selected
            if (SelectedTeamId == 0 && TeamOptions.Any())
            {
                SelectedTeamId = int.Parse(TeamOptions.First().Value);
            }

            // Determine visible columns based on the selected view mode
            VisibleColumns = (ViewMode?.ToLowerInvariant() == "advanced") ? new List<string>(AdvancedCols) : new List<string>(BasicCols);

            // Load pitchers for the selected team (position == "P")
            var players = await _context.Players
                .AsNoTracking()
                .Where(p => p.TeamId == SelectedTeamId && p.Position == "P")
                .OrderBy(p => p.Name)
                .ToListAsync();

            var playerIds = players.Select(p => p.Id).ToList();

            // Get PlayerStatTypes in this category for the visible columns
            var statTypes = await _context.PlayerStatTypes
                .AsNoTracking()
                .Include(st => st.StatCategory)
                .Where(st => st.StatCategory != null
                             && st.StatCategory.Name == CategoryName
                             && VisibleColumns.Contains(st.Name))
                .ToListAsync();

            var typeIds = statTypes.Select(s => s.Id).ToList();
            var nameByTypeId = statTypes.ToDictionary(s => s.Id, s => s.Name);

            // Fetch the player stats for the selected players and stat types
            var stats = await _context.PlayerStats
                .AsNoTracking()
                .Where(ps => playerIds.Contains(ps.PlayerId) && typeIds.Contains(ps.PlayerStatTypeId))
                .ToListAsync();

            // Group stats by player id for quick lookup
            var byPlayer = stats.GroupBy(s => s.PlayerId)
                                .ToDictionary(g => g.Key, g => g.ToList());

            // Build rows for display
            var resultRows = new List<PlayerRow>();
            foreach (var p in players)
            {
                var row = new PlayerRow
                {
                    PlayerId = p.Id,
                    Name = p.Name,
                    Number = p.Number,
                    Position = p.Position,
                    Status = p.Status
                };
                if (byPlayer.TryGetValue(p.Id, out var list))
                {
                    foreach (var s in list)
                    {
                        if (nameByTypeId.TryGetValue(s.PlayerStatTypeId, out var nm))
                        {
                            row.Values[nm] = s.Total;
                        }
                    }
                }
                resultRows.Add(row);
            }
            Rows = resultRows;
        }
    }
}