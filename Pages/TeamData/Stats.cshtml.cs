using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using StrikeData.Data;
using StrikeData.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StrikeData.Pages.TeamData
{
    public class StatsModel : PageModel
    {
        private readonly AppDbContext _context;

        public StatsModel(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty(SupportsGet = true)]
        public int? SelectedStatTypeId { get; set; }

        public List<StatType> StatTypes { get; set; } = new();
        public List<TeamStat> TeamStats { get; set; } = new();

        public async Task OnGetAsync()
        {
            StatTypes = await _context.StatTypes.ToListAsync();

            var query = _context.TeamStats
                .Include(ts => ts.Team)
                .Include(ts => ts.StatType)
                .AsQueryable();

            if (SelectedStatTypeId.HasValue)
            {
                query = query.Where(ts => ts.StatTypeId == SelectedStatTypeId.Value);
            }

            TeamStats = await query
                .OrderByDescending(ts => ts.CurrentSeason)
                .ThenBy(ts => ts.Team.Name)
                .ToListAsync();
        }

    }
}
