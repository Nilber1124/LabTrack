using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LabTrack.Models
{
    [Table("reservas")]
    public class Reserva
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        [Range(typeof(long), "1", "9223372036854775807", ErrorMessage = "Selecciona un estudiante vÃ¡lido.")]
        public long EstudianteId { get; set; }

        [Range(typeof(long), "1", "9223372036854775807", ErrorMessage = "Selecciona un equipo vÃ¡lido.")]
        public long EquipoId { get; set; }

        [Column(TypeName = "datetimeoffset")]
        public DateTimeOffset Inicio { get; set; }

        [Column(TypeName = "datetimeoffset")]
        public DateTimeOffset Fin { get; set; }

        [EnumDataType(typeof(EstadoReserva), ErrorMessage = "El estado de la reserva no es vÃ¡lido.")]
        public EstadoReserva Estado { get; set; } = EstadoReserva.Pendiente;

        [StringLength(255, ErrorMessage = "El motivo de rechazo no puede exceder los 255 caracteres.")]
        public string? MotivoRechazo { get; set; }

        [Range(typeof(long), "1", "9223372036854775807", ErrorMessage = "El encargado no es vÃ¡lido.")]
        public long? ResueltaPor { get; set; }

        // Los instantes DateTime se guardan en UTC.
        [Column(TypeName = "datetime2")]
        public DateTime? ResueltaEn { get; set; }

        [Column(TypeName = "datetime2")]
        public DateTime? DevueltaEn { get; set; }

        [Column(TypeName = "datetime2")]
        public DateTime CreadaEn { get; set; }

        [ValidateNever]
        public Usuario Estudiante { get; set; } = null!;

        [ValidateNever]
        public Usuario? EncargadoResolutor { get; set; }

        [ValidateNever]
        public Equipo Equipo { get; set; } = null!;

        [ValidateNever]
        public Penalizacion? Penalizacion { get; set; }
    }
}
