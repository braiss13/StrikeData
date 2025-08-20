using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StrikeData.Models
{
    public class PlayerStat
    {
        [Key]
        public int Id { get; set; }

        [Required, ForeignKey("Player")]
        public int PlayerId { get; set; }
        public Player Player { get; set; }

        [Required, ForeignKey("PlayerStatType")]
        public int PlayerStatTypeId { get; set; }
        public PlayerStatType PlayerStatType { get; set; }

        // Valor de la estadística (número limpio, sin %)
        public float? Total { get; set; }
    }
}
