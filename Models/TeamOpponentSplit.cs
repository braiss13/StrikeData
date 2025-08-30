using System.ComponentModel.DataAnnotations;

namespace StrikeData.Models
{
    // Balance of matches against a specific rival in a season.
    public class TeamOpponentSplit
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int TeamId { get; set; }
        public Team Team { get; set; }

        [Required]
        public int Season { get; set; }

        public int? OpponentTeamId { get; set; }
        public Team OpponentTeam { get; set; }

        [Required, MaxLength(100)]
        public string OpponentName { get; set; }

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
