using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace StrikeData.Models
{
    public class Team
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public required string Name { get; set; }

        // Relación uno a muchos con Stats y WinTrends
        public required ICollection<Stats> Stats { get; set; }
        public required ICollection<WinTrends> WinTrends { get; set; }
    }
}
