using Microsoft.EntityFrameworkCore;
using StrikeData.Models;

namespace StrikeData.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { 

        }

        public DbSet<Team> Teams { get; set; }
        public DbSet<Stats> Stats { get; set; }
        public DbSet<WinTrends> WinTrends { get; set; }
    }
}
