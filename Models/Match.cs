using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StrikeData.Models
{
    public class Match
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Identificador de la MLB (gamePk). Añade un índice único sobre esta columna.
        /// </summary>
        [Required]
        public long GamePk { get; set; }

        [Required]
        public DateTime Date { get; set; }

        [ForeignKey("Team")]
        public int? HomeTeamId { get; set; }
        public Team HomeTeam { get; set; }

        [ForeignKey("Team")]
        public int? AwayTeamId { get; set; }
        public Team AwayTeam { get; set; }

        [MaxLength(100)]
        public string Venue { get; set; }

        // Resultados finales (runs, hits, errors)
        public int? HomeRuns  { get; set; }
        public int? HomeHits  { get; set; }
        public int? HomeErrors{ get; set; }
        public int? AwayRuns  { get; set; }
        public int? AwayHits  { get; set; }
        public int? AwayErrors{ get; set; }

        // Alias para compatibilidad con código existente
        public int? HomeScore { get; set; }
        public int? AwayScore { get; set; }

        // Récord de cada equipo al llegar al partido
        public int? HomeWins  { get; set; }
        public int? HomeLosses{ get; set; }
        public decimal? HomePct { get; set; }
        public int? AwayWins  { get; set; }
        public int? AwayLosses{ get; set; }
        public decimal? AwayPct { get; set; }

        // Otros campos opcionales existentes (asistencia y duración)
        public int? Attendance { get; set; }
        public int? DurationMinutes { get; set; }

        // Colección de líneas por entrada
        public ICollection<MatchInning> Innings { get; set; } = new List<MatchInning>();
    }
}
