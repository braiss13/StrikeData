using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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

        [Required]
        public float? CurrentSeason { get; set; }
        public float? Total { get; set; }
        public float? Last3Games { get; set; }
        public float? LastGame { get; set; }
        public float? Home { get; set; }
        public float? Away { get; set; }
        public float? PrevSeason { get; set; }
    }
}
