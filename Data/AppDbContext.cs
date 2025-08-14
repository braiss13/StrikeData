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
        public DbSet<StatCategory> StatCategories { get; set; }
        public DbSet<TeamStat> TeamStats { get; set; }
        public DbSet<TeamGame> TeamGames { get; set; }
        public DbSet<TeamMonthlySplit> TeamMonthlySplits { get; set; }
        public DbSet<TeamOpponentSplit> TeamOpponentSplits { get; set; }

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

            // Relación 1:N StatCategory → StatType
            modelBuilder.Entity<StatType>()
                .HasOne(st => st.StatCategory)
                .WithMany(sc => sc.StatTypes)
                .HasForeignKey(st => st.StatCategoryId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<StatType>()
                .Property(st => st.StatCategoryId)
                .HasDefaultValue(1);

            // TeamGame -> Team (principal)
            modelBuilder.Entity<TeamGame>()
                .HasOne(tg => tg.Team)
                .WithMany(t => t.TeamGames)
                .HasForeignKey(tg => tg.TeamId)
                .OnDelete(DeleteBehavior.Cascade);

            // TeamGame -> OpponentTeam (secundaria)
            modelBuilder.Entity<TeamGame>()
                .HasOne(tg => tg.OpponentTeam)
                .WithMany()
                .HasForeignKey(tg => tg.OpponentTeamId)
                .OnDelete(DeleteBehavior.Restrict);

            // Índice único para evitar duplicados (un partido por número y temporada)
            modelBuilder.Entity<TeamGame>()
                .HasIndex(tg => new { tg.TeamId, tg.Season, tg.GameNumber })
                .IsUnique();

            // TeamMonthlySplit -> Team
            modelBuilder.Entity<TeamMonthlySplit>()
                .HasOne(ms => ms.Team)
                .WithMany(t => t.TeamMonthlySplits)
                .HasForeignKey(ms => ms.TeamId)
                .OnDelete(DeleteBehavior.Cascade);

            // Índice único (un mes por equipo y temporada)
            modelBuilder.Entity<TeamMonthlySplit>()
                .HasIndex(ms => new { ms.TeamId, ms.Season, ms.Month })
                .IsUnique();

            // TeamOpponentSplit -> Team
            modelBuilder.Entity<TeamOpponentSplit>()
                .HasOne(ts => ts.Team)
                .WithMany(t => t.TeamOpponentSplits)
                .HasForeignKey(ts => ts.TeamId)
                .OnDelete(DeleteBehavior.Cascade);

            // TeamOpponentSplit -> OpponentTeam
            modelBuilder.Entity<TeamOpponentSplit>()
                .HasOne(ts => ts.OpponentTeam)
                .WithMany()
                .HasForeignKey(ts => ts.OpponentTeamId)
                .OnDelete(DeleteBehavior.Restrict);

            // Índice único (rival por equipo y temporada)
            modelBuilder.Entity<TeamOpponentSplit>()
                .HasIndex(ts => new { ts.TeamId, ts.Season, ts.OpponentName })
                .IsUnique();

        }
    }
}
