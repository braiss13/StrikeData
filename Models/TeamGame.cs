using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StrikeData.Models
{
    /// <summary>
    /// Representa un partido del calendario de un equipo en una temporada concreta.
    /// </summary>
    public class TeamGame
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int TeamId { get; set; }
        public Team Team { get; set; }

        /// <summary>Temporada (año) a la que pertenece el partido.</summary>
        [Required]
        public int Season { get; set; }

        [Required]
        public int GameNumber { get; set; }

        [Required]
        public DateTime Date { get; set; }

        /// <summary>Indica si este equipo juega como local (vs) o visitante (at).</summary>
        [Required]
        public bool IsHome { get; set; }

        /// <summary>FK al equipo rival, si se logra mapear el nombre; si no, queda a null.</summary>
        public int? OpponentTeamId { get; set; }
        public Team OpponentTeam { get; set; }

        /// <summary>Nombre del rival tal como aparece en Baseball‑Almanac, normalizado.</summary>
        [Required, MaxLength(100)]
        public string OpponentName { get; set; }

        /// <summary>Marcador del partido (p. ej. "5-3").</summary>
        [MaxLength(20)]
        public string Score { get; set; }

        /// <summary>Decisión: W (win) o L (loss).</summary>
        [MaxLength(2)]
        public string Decision { get; set; }

        /// <summary>Récord acumulado tras el partido (p. ej. "7-3").</summary>
        [MaxLength(20)]
        public string Record { get; set; }
    }
}
