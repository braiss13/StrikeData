using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using StrikeData.Data;

namespace StrikeData.Pages.PlayerData
{
    public class FieldingModel : PageModel
    {
        private readonly AppDbContext _context;

        public FieldingModel(AppDbContext context)
        {
            _context = context;
        }

        // Team dropdown options
        public List<SelectListItem> TeamOptions { get; set; } = new();

        // Bound property for selected team
        [BindProperty(SupportsGet = true)]
        public int SelectedTeamId { get; set; }

        // Columns to display (all fielding stats)
        public List<string> VisibleColumns { get; private set; } = new();

        // Player rows
        public List<PlayerRow> Rows { get; private set; } = new();

        private static readonly string CategoryName = "Fielding";

        // Fielding stat abbreviations
        private static readonly List<string> Columns = new()
        {
            "OUTS", "TC", "CH", "PO", "A", "E", "DP", "PB", "CASB", "CACS", "FLD%"
        };

        // Metadata for stats: long name and description
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
            public Dictionary<string, float?> Values { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        }

        // Fill StatMeta with names and descriptions
        private void InitStatMeta()
        {
            StatMeta.Clear();
            StatMeta["OUTS"] = new StatInfo
            {
                LongName = "Outs",
                Description = "Total defensive outs recorded by the player."
            };
            StatMeta["TC"] = new StatInfo
            {
                LongName = "Total Chances",
                Description = "Total defensive chances: putouts + assists + errors."
            };
            StatMeta["CH"] = new StatInfo
            {
                LongName = "Chances",
                Description = "Number of opportunities to make a play (putouts + assists + errors)."
            };
            StatMeta["PO"] = new StatInfo
            {
                LongName = "Putouts",
                Description = "Number of outs credited by tagging a runner, force plays or catching a fly ball."
            };
            StatMeta["A"] = new StatInfo
            {
                LongName = "Assists",
                Description = "Number of times the player assists on an out."
            };
            StatMeta["E"] = new StatInfo
            {
                LongName = "Errors",
                Description = "Defensive miscues allowing a runner to reach or advance."
            };
            StatMeta["DP"] = new StatInfo
            {
                LongName = "Double Plays",
                Description = "Number of double plays in which the player participated."
            };
            StatMeta["PB"] = new StatInfo
            {
                LongName = "Passed Balls",
                Description = "Number of pitches a catcher fails to handle, allowing runners to advance."
            };
            StatMeta["CASB"] = new StatInfo
            {
                LongName = "Stolen Bases Allowed",
                Description = "Baserunners who successfully stole while the player was fielding."
            };
            StatMeta["CACS"] = new StatInfo
            {
                LongName = "Caught Stealing",
                Description = "Baserunners thrown out while attempting to steal a base."
            };
            StatMeta["FLD%"] = new StatInfo
            {
                LongName = "Fielding Percentage",
                Description = "Fielding percentage: (putouts + assists) divided by total chances."
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

            // All fielding stats are visible
            VisibleColumns = Columns;

            // Load players for selected team (any position)
            var players = await _context.Players
                .AsNoTracking()
                .Where(p => p.TeamId == SelectedTeamId)
                .OrderBy(p => p.Name)
                .ToListAsync();

            var playerIds = players.Select(p => p.Id).ToList();

            // Load stat types in the Fielding category for these columns
            var statTypes = await _context.PlayerStatTypes
                .AsNoTracking()
                .Include(st => st.StatCategory)
                .Where(st => st.StatCategory != null
                             && st.StatCategory.Name == CategoryName
                             && Columns.Contains(st.Name))
                .ToListAsync();

            var typeIds = statTypes.Select(s => s.Id).ToList();
            var nameByTypeId = statTypes.ToDictionary(s => s.Id, s => s.Name);

            // Load player stats
            var stats = await _context.PlayerStats
                .AsNoTracking()
                .Where(ps => playerIds.Contains(ps.PlayerId) && typeIds.Contains(ps.PlayerStatTypeId))
                .ToListAsync();

            // Build rows
            var rows = new List<PlayerRow>();
            var byPlayer = stats.GroupBy(s => s.PlayerId).ToDictionary(g => g.Key, g => g.ToList());

            foreach (var p in players)
            {
                var row = new PlayerRow
                {
                    PlayerId = p.Id,
                    Name = p.Name,
                    Number = p.Number,
                    Position = p.Position
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
