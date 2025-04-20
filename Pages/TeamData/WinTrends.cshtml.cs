using Microsoft.AspNetCore.Mvc.RazorPages;
using StrikeData.Data;
using StrikeData.Models;
using System.Collections.Generic;
using System.Linq;

namespace StrikeData.Pages.TeamData
{
    public class WinTrendsModel : PageModel
    {
        private readonly AppDbContext _context;

        public WinTrendsModel(AppDbContext context)
        {
            _context = context;
        }

        public List<Team> Teams { get; set; }

        public void OnGet()
        {
            Teams = _context.Teams
                .Where(t => t.OverallRecord != null && t.WinPercentage != null)
                .ToList();
        }
    }
}
