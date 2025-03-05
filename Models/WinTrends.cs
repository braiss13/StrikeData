using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StrikeData.Models
{
    public class WinTrends
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [ForeignKey("Team")]
        public int TeamId { get; set; }
        public required Team Team { get; set; }

        [Required]
        public int SeasonYear { get; set; }

        [MaxLength(20)]
        public required string OverallRecord { get; set; }

        public float? WinPercentage { get; set; }

        [MaxLength(20)]
        public required string HomeRecord { get; set; }

        [MaxLength(20)]
        public required string AwayRecord { get; set; }
    }
}
