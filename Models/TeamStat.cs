using System.ComponentModel.DataAnnotations;
using StrikeData.Models.Enums;

namespace StrikeData.Models
{
    public class TeamStat
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int TeamId { get; set; }
        public Team Team { get; set; }

        [Required]
        public int StatTypeId { get; set; }
        public StatType StatType { get; set; }

        // Listed to distinguish Team vs Opponent
        public StatPerspective Perspective { get; set; } = StatPerspective.Team;

        public float? CurrentSeason { get; set; }
        public float? Total { get; set; }
        public float? Last3Games { get; set; }
        public float? LastGame { get; set; }
        public float? Home { get; set; }
        public float? Away { get; set; }
        public float? PrevSeason { get; set; }
        
        [MaxLength(20)]
        public string? WinLossRecord { get; set; }   

        public float? WinPct { get; set; }
    }
}
