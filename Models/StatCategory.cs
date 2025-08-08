using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace StrikeData.Models
{
    public class StatCategory
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; }

        public ICollection<StatType> StatTypes { get; set; }
    }
}