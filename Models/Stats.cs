using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StrikeData.Models
{
    public class Stats
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [ForeignKey("Team")]
        public int TeamId { get; set; }
        public required Team Team { get; set; }

        [Required]
        public int SeasonYear { get; set; }

        public float? RunsPerGame { get; set; }
        public float? HitsPerGame { get; set; }
        public float? Last3Games { get; set; }
        public float? LastGame { get; set; }
        public float? HomePerformance { get; set; }
        public float? AwayPerformance { get; set; }
    }
}
