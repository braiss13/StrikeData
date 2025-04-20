using Microsoft.EntityFrameworkCore;
using StrikeData.Models;

namespace StrikeData.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        // Entidades nuevas
        public DbSet<Team> Teams { get; set; }
        public DbSet<Player> Players { get; set; }
        public DbSet<Match> Matches { get; set; }
        public DbSet<StatType> StatTypes { get; set; }
        public DbSet<TeamStat> TeamStats { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Unicidad en el nombre del equipo
            modelBuilder.Entity<Team>()
                .HasIndex(t => t.Name)
                .IsUnique();

            // Relación 1:N Team → Player
            modelBuilder.Entity<Player>()
                .HasOne(p => p.Team)
                .WithMany(t => t.Players)
                .HasForeignKey(p => p.TeamId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relación 1:N Team → Match (como equipo local)
            modelBuilder.Entity<Match>()
                .HasOne(m => m.HomeTeam)
                .WithMany(t => t.HomeMatches)
                .HasForeignKey(m => m.HomeTeamId)
                .OnDelete(DeleteBehavior.SetNull);

            // Relación 1:N Team → Match (como equipo visitante)
            modelBuilder.Entity<Match>()
                .HasOne(m => m.AwayTeam)
                .WithMany(t => t.AwayMatches)
                .HasForeignKey(m => m.AwayTeamId)
                .OnDelete(DeleteBehavior.SetNull);

            // Relación 1:N Team → TeamStat
            modelBuilder.Entity<TeamStat>()
                .HasOne(ts => ts.Team)
                .WithMany(t => t.TeamStats)
                .HasForeignKey(ts => ts.TeamId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relación 1:N StatType → TeamStat
            modelBuilder.Entity<TeamStat>()
                .HasOne(ts => ts.StatType)
                .WithMany(st => st.TeamStats)
                .HasForeignKey(ts => ts.StatTypeId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
