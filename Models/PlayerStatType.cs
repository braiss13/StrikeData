using System.ComponentModel.DataAnnotations;

namespace StrikeData.Models
{
    public class PlayerStatType
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; }

        [Required]
        public int StatCategoryId { get; set; }
        public StatCategory StatCategory { get; set; }

        public ICollection<PlayerStat> PlayerStats { get; set; }
    }
}
