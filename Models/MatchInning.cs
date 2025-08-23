using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StrikeData.Models
{
    /// <summary>
    /// Línea por entrada para un partido. Cada fila almacena carreras, hits y errores
    /// de ambos equipos para una entrada concreta. Las entradas extra siguen numeradas
    /// a partir de 10.
    /// </summary>
    
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
