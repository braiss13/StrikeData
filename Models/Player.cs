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

        // --- NUEVO ---
        // Id oficial de MLB. Suele caber en int, pero usamos long? por seguridad.
        public long? MLB_Player_Id { get; set; }

        // Dorsal (0..99 normalmente). Lo dejamos nullable por si una fuente no lo trae.
        public int? Number { get; set; }

        // Estado del jugador (ej.: "Active", "Injured"). Texto corto.
        [MaxLength(30)]
        public string? Status { get; set; }

        // Navegación (opcional) a stats de jugador
        public ICollection<PlayerStat> PlayerStats { get; set; }
    }
}
