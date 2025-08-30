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
        public Team Team { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; }

        [MaxLength(50)]
        public string Position { get; set; }

        // MLB official ID. 
        public long? MLB_Player_Id { get; set; }

        // JerseyNumber (0..99 ). 
        public int? Number { get; set; }

        // Player Status (ej.: "Active", "Injured"). 
        [MaxLength(30)]
        public string? Status { get; set; }

        public ICollection<PlayerStat> PlayerStats { get; set; }
    }
}
