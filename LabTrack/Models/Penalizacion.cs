using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LabTrack.Models
{
    [Table("penalizaciones")]
    public class Penalizacion
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        [Range(typeof(long), "1", "9223372036854775807", ErrorMessage = "Selecciona una reserva vÃ¡lida.")]
        public long ReservaId { get; set; }

        [Column(TypeName = "datetime2")]
        public DateTime CreadaEn { get; set; }

        [Column("Levantada_En", TypeName = "datetime2")]
        public DateTime? LevantadaEn { get; set; }

        [ValidateNever]
        public Reserva Reserva { get; set; } = null!;
    }
}
