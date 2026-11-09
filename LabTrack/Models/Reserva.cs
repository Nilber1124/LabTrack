using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LabTrack.Models
{
    public class Reserva
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdReserva { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Selecciona un estudiante válido.")]
        public int IdUsuario { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Selecciona un equipo válido.")]
        public int IdEquipo { get; set; }

        public DateTime Inicio { get; set; }

        public DateTime Fin { get; set; }

        [EnumDataType(typeof(EstadoReserva), ErrorMessage = "El estado de la reserva no es válido.")]
        public EstadoReserva Estado { get; set; } = EstadoReserva.Pendiente;

        [StringLength(200, ErrorMessage = "El motivo de rechazo no puede exceder los 200 caracteres.")]
        public string? MotivoRechazo { get; set; }

        public DateTime? FechaDevolucionReal { get; set; }

        public DateTime? FechaSolicitud { get; set; }

        [Timestamp]
        public byte[] Version { get; set; } = [];

        [ValidateNever]
        public Usuario Usuario { get; set; } = null!;

        [ValidateNever]
        public Equipo Equipo { get; set; } = null!;
    }
}
