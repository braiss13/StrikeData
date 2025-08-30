using System.ComponentModel.DataAnnotations;

namespace StrikeData.Models
{
    public class StatType
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; }

        [Required]
        public int StatCategoryId { get; set; } = 1;

        public StatCategory StatCategory { get; set; }

        public ICollection<TeamStat> TeamStats { get; set; }
    }
}
