using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace StrikeData.Models
{
    public class Team
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; }

        // Campos antes en WinTrends:
        public float? WinPercentage { get; set; }

        [MaxLength(20)]
        public string? AwayRecord { get; set; }

        [MaxLength(20)]
        public string? HomeRecord { get; set; }

        [MaxLength(20)]
        public string? OverallRecord { get; set; }

        public int? Games { get; set; } 

        // Relaciones
        public ICollection<Player> Players { get; set; }
        public ICollection<Match> HomeMatches { get; set; }
        public ICollection<Match> AwayMatches { get; set; }
        public ICollection<TeamStat> TeamStats { get; set; }
    }
}
