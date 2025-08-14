using System;
using System.ComponentModel.DataAnnotations;

namespace StrikeData.Models
{
    /// <summary>
    /// Resumen mensual de victorias/derrotas de un equipo en una temporada.
    /// </summary>
    public class TeamMonthlySplit
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int TeamId { get; set; }
        public Team Team { get; set; }

        [Required]
        public int Season { get; set; }

        [Required, MaxLength(20)]
        public string Month { get; set; }

        [Required]
        public int Games { get; set; }

        [Required]
        public int Wins { get; set; }

        [Required]
        public int Losses { get; set; }

        [Required]
        public decimal WinPercentage { get; set; }
    }
}
