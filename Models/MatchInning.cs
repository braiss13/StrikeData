using System.ComponentModel.DataAnnotations;

namespace StrikeData.Models
{
    
    public class MatchInning
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int MatchId { get; set; }
        public Match Match { get; set; }

        [Required]
        public int InningNumber { get; set; }

        public int? HomeRuns { get; set; }
        public int? HomeHits { get; set; }
        public int? HomeErrors { get; set; }

        public int? AwayRuns { get; set; }
        public int? AwayHits { get; set; }
        public int? AwayErrors { get; set; }
    }
}
