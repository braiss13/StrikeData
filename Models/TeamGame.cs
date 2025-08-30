using System.ComponentModel.DataAnnotations;

namespace StrikeData.Models
{
    // It represents a match on a team's schedule in a specific season.
    public class TeamGame
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int TeamId { get; set; }
        public Team Team { get; set; }

        // Season of the match.
        [Required]
        public int Season { get; set; }

        [Required]
        public int GameNumber { get; set; }

        [Required]
        public DateTime Date { get; set; }

        // Indicates if the match was played at home (true) or away (false).
        [Required]
        public bool IsHome { get; set; }

        // FK to away Team, nullable in case the opponent team is not in the database.
        public int? OpponentTeamId { get; set; }
        public Team OpponentTeam { get; set; }

        // Away Team Name as it appears on Baseball‑Almanac, normalized.
        [Required, MaxLength(100)]
        public string OpponentName { get; set; }

        [MaxLength(20)]
        public string Score { get; set; }

        // Decision: W (win) o L (loss).
        [MaxLength(2)]
        public string Decision { get; set; }

        // Accumulated record after the match (e.g. "7-3").
        [MaxLength(20)]
        public string Record { get; set; }
    }
}
