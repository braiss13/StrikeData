using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using StrikeData.Data;

namespace StrikeData.Pages.PlayerData
{
    public class HittingPlayerModel : PageModel
    {
        private readonly AppDbContext _context;

        public HittingPlayerModel(AppDbContext context)
        {
            _context = context;
        }

        // Dropdown teams
        public List<SelectListItem> TeamOptions { get; set; } = new();

        // Bindings
        [BindProperty(SupportsGet = true)]
        public int SelectedTeamId { get; set; }

        [BindProperty(SupportsGet = true)]
        public string ViewMode { get; set; } = "basic"; // "basic" | "advanced"

        // Columns visible according to mode
        public List<string> VisibleColumns { get; private set; } = new();

        // Rows for display
        public List<PlayerRow> Rows { get; private set; } = new();

        private static readonly string CategoryName = "Hitting";

        // Basic and advanced column lists
        private static readonly List<string> BasicCols = new()
        {
            "G","AB","R","H","2B","3B","HR","RBI","BB","SO","SB","CS","AVG","OBP","SLG","OPS"
        };

        private static readonly List<string> AdvancedCols = new()
        {
            "PA","HBP","SAC","SF","GIDP","GO/AO","XBH","TB","IBB","BABIP","ISO","AB/HR","BB/K","BB%","SO%","HR%"
        };

        // Metadata for stats (long name and description)
        public Dictionary<string, StatInfo> StatMeta { get; private set; } = new(StringComparer.OrdinalIgnoreCase);

        public class StatInfo
        {
            public string LongName { get; set; } = "";
            public string Description { get; set; } = "";
        }

        public class PlayerRow
        {
            public int PlayerId { get; set; }
            public int? Number { get; set; }
            public string Name { get; set; } = "";
            public string? Position { get; set; }
            public string? Status { get; set; }
            public Dictionary<string, float?> Values { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        }

        private void InitStatMeta()
        {
            StatMeta.Clear();
            // Basic hitting stats definitions

            StatMeta["Status"] = new StatInfo
            {
                LongName = "Player Status",
                Description = "A => Active; M => Reassigned to Minors; D[n] => Days Injured (n = number of days)."
            };
            StatMeta["G"] = new StatInfo
            {
                LongName = "Games",
                Description = "Number of games in which the player appeared."
            };
            StatMeta["AB"] = new StatInfo
            {
                LongName = "At Bats",
                Description = "Official at-bats, excluding walks, hit-by-pitch, sacrifices and interference."
            };
            StatMeta["R"] = new StatInfo
            {
                LongName = "Runs",
                Description = "Total runs scored by the player."
            };
            StatMeta["H"] = new StatInfo
            {
                LongName = "Hits",
                Description = "Number of times the player reaches at least first base safely on a fair ball without an error or fielder's choice."
            };
            StatMeta["2B"] = new StatInfo
            {
                LongName = "Doubles",
                Description = "Hits on which the batter safely reaches second base."
            };
            StatMeta["3B"] = new StatInfo
            {
                LongName = "Triples",
                Description = "Hits on which the batter safely reaches third base."
            };
            StatMeta["HR"] = new StatInfo
            {
                LongName = "Home Runs",
                Description = "Hits on which the batter circles all bases in one play, usually by hitting the ball over the outfield fence."
            };
            StatMeta["RBI"] = new StatInfo
            {
                LongName = "Runs Batted In",
                Description = "Runs scored as a result of the player's at-bat, except when due to errors."
            };
            StatMeta["BB"] = new StatInfo
            {
                LongName = "Walks",
                Description = "Number of times the player reaches first base after four balls."
            };
            StatMeta["SO"] = new StatInfo
            {
                LongName = "Strikeouts",
                Description = "Number of times the player is retired by strike three."
            };
            StatMeta["SB"] = new StatInfo
            {
                LongName = "Stolen Bases",
                Description = "Number of bases stolen by the player without the help of a hit or error."
            };
            StatMeta["CS"] = new StatInfo
            {
                LongName = "Caught Stealing",
                Description = "Times the player is thrown out while attempting to steal a base."
            };
            StatMeta["AVG"] = new StatInfo
            {
                LongName = "Batting Average",
                Description = "Hits divided by at-bats: H/AB."
            };
            StatMeta["OBP"] = new StatInfo
            {
                LongName = "On-base Percentage",
                Description = "Frequency the player reaches base safely: (H + BB + HBP) / (AB + BB + HBP + SF)."
            };
            StatMeta["SLG"] = new StatInfo
            {
                LongName = "Slugging Percentage",
                Description = "Total bases per at-bat: (1×1B + 2×2B + 3×3B + 4×HR) / AB."
            };
            StatMeta["OPS"] = new StatInfo
            {
                LongName = "On-base Plus Slugging",
                Description = "Sum of on-base percentage and slugging percentage (OBP + SLG)."
            };

            // Advanced hitting stats definitions
            StatMeta["PA"] = new StatInfo
            {
                LongName = "Plate Appearances",
                Description = "Total completed batting appearances, including at-bats, walks, hit-by-pitch, sacrifices and times reached by interference."
            };
            StatMeta["HBP"] = new StatInfo
            {
                LongName = "Hit By Pitch",
                Description = "Number of times the batter is awarded first base after being hit by a pitched ball."
            };
            StatMeta["SAC"] = new StatInfo
            {
                LongName = "Sacrifice Bunts",
                Description = "Number of bunts that advance a runner while the batter is thrown out."
            };
            StatMeta["SF"] = new StatInfo
            {
                LongName = "Sacrifice Flies",
                Description = "Number of fly balls caught for outs that allow a runner to score."
            };
            StatMeta["GIDP"] = new StatInfo
            {
                LongName = "Grounded Into Double Play",
                Description = "Number of ground balls that result in a double play."
            };
            StatMeta["GO/AO"] = new StatInfo
            {
                LongName = "Groundouts to Airouts Ratio",
                Description = "Ratio of outs on ground balls to outs on fly balls."
            };
            StatMeta["XBH"] = new StatInfo
            {
                LongName = "Extra-Base Hits",
                Description = "Total number of doubles, triples and home runs."
            };
            StatMeta["TB"] = new StatInfo
            {
                LongName = "Total Bases",
                Description = "Sum of bases gained by hits: singles (1), doubles (2), triples (3) and home runs (4)."
            };
            StatMeta["IBB"] = new StatInfo
            {
                LongName = "Intentional Walks",
                Description = "Number of times the player is walked intentionally by the opposing team."
            };
            StatMeta["BABIP"] = new StatInfo
            {
                LongName = "Batting Average on Balls in Play",
                Description = "Average on balls put in play excluding home runs: (H − HR) / (AB − SO − HR + SF)."
            };
            StatMeta["ISO"] = new StatInfo
            {
                LongName = "Isolated Power",
                Description = "Power metric calculated as slugging percentage minus batting average (SLG − AVG)."
            };
            StatMeta["AB/HR"] = new StatInfo
            {
                LongName = "At-bats per Home Run",
                Description = "Average number of at-bats between home runs: AB/HR."
            };
            StatMeta["BB/K"] = new StatInfo
            {
                LongName = "Walk-to-Strikeout Ratio",
                Description = "Walks divided by strikeouts: BB/SO."
            };
            StatMeta["BB%"] = new StatInfo
            {
                LongName = "Walk Rate",
                Description = "Percentage of plate appearances resulting in walks: BB/PA."
            };
            StatMeta["SO%"] = new StatInfo
            {
                LongName = "Strikeout Rate",
                Description = "Percentage of plate appearances resulting in strikeouts: SO/PA."
            };
            StatMeta["HR%"] = new StatInfo
            {
                LongName = "Home Run Rate",
                Description = "Percentage of plate appearances resulting in home runs: HR/PA."
            };
        }

        public async Task OnGetAsync()
        {
            InitStatMeta();

            // Teams for dropdown
            TeamOptions = await _context.Teams
                .OrderBy(t => t.Name)
                .Select(t => new SelectListItem { Value = t.Id.ToString(), Text = t.Name })
                .ToListAsync();

            if (SelectedTeamId == 0 && TeamOptions.Any())
                SelectedTeamId = int.Parse(TeamOptions.First().Value);

            // Determine visible columns based on view mode
            VisibleColumns = (ViewMode?.ToLowerInvariant() == "advanced") ? AdvancedCols : BasicCols;

            // Non-pitcher players for the selected team
            // Hitting.cshtml.cs
            var players = await _context.Players
                .AsNoTracking()
                .Where(p =>
                    p.TeamId == SelectedTeamId &&
                    (p.Position == null || p.Position.ToUpper() != "P"))
                .OrderBy(p => p.Name)
                .ToListAsync();

            var playerIds = players.Select(p => p.Id).ToList();

            // Get PlayerStatTypes in this category for visible columns
            var statTypes = await _context.PlayerStatTypes
                .AsNoTracking()
                .Include(st => st.StatCategory)
                .Where(st => st.StatCategory != null
                             && st.StatCategory.Name == CategoryName
                             && VisibleColumns.Contains(st.Name))
                .ToListAsync();

            var typeIds = statTypes.Select(s => s.Id).ToList();
            var nameByTypeId = statTypes.ToDictionary(s => s.Id, s => s.Name);

            // PlayerStats for those players and metrics
            var stats = await _context.PlayerStats
                .AsNoTracking()
                .Where(ps => playerIds.Contains(ps.PlayerId) && typeIds.Contains(ps.PlayerStatTypeId))
                .ToListAsync();

            // Build rows
            var rows = new List<PlayerRow>();
            var byPlayer = stats.GroupBy(s => s.PlayerId)
                                .ToDictionary(g => g.Key, g => g.ToList());

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
                            row.Values[nm] = s.Total;
                    }
                }

                rows.Add(row);
            }

            Rows = rows;
        }
    }
}
