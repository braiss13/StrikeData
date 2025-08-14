using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StrikeData.Models
{
    public class Match
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public DateTime Date { get; set; }

        [ForeignKey("Team")]
        public int? HomeTeamId { get; set; }
        public Team HomeTeam { get; set; }  // Propiedad de navegación, por recomendación de EF Core se usa para acceder más fácil al objeto

        [ForeignKey("Team")]
        public int? AwayTeamId { get; set; }
        public Team AwayTeam { get; set; }  // Propiedad de navegación, por recomendación de EF Core se usa para acceder más fácil al objeto

        [MaxLength(100)]
        public string Venue { get; set; }

        public int? HomeScore { get; set; }
        public int? AwayScore { get; set; }
        public int? Attendance { get; set; }
        public int? DurationMinutes { get; set; }
    }
}
