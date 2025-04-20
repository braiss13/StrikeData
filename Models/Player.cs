using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StrikeData.Models
{
    public class Player
    {
        [Key]
        public int Id { get; set; }

        [Required, ForeignKey("Team")]
        public int TeamId { get; set; }
        public Team Team { get; set; }   // Propiedad de navegación, por recomendación de EF Core se usa para acceder más fácil al objeto

        [Required, MaxLength(100)]
        public string Name { get; set; }

        [MaxLength(50)]
        public string Position { get; set; }

        public float? BattingAverage { get; set; }
        public int? HomeRuns { get; set; }
        public int? RunsBattedIn { get; set; }
        public int? GamesPlayed { get; set; }
        public int? StolenBases { get; set; }
        public float? OnBasePercentage { get; set; }
        public float? SluggingPercentage { get; set; }
    }
}
