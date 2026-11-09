using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LabTrack.Models
{
    [Table("usuarios")]
    public class Usuario
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        [Required(ErrorMessage = "El cÃ³digo del usuario es obligatorio.")]
        [StringLength(20, ErrorMessage = "El cÃ³digo no puede exceder los 20 caracteres.")]
        public string Codigo { get; set; } = string.Empty;

        [Required(ErrorMessage = "El nombre del usuario es obligatorio.")]
        [StringLength(120, ErrorMessage = "El nombre no puede exceder los 120 caracteres.")]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El hash de la contraseÃ±a es obligatorio.")]
        [StringLength(255, ErrorMessage = "El hash no puede exceder los 255 caracteres.")]
        public string PasswordHash { get; set; } = string.Empty;

        [EnumDataType(typeof(RolUsuario), ErrorMessage = "El rol del usuario no es vÃ¡lido.")]
        public RolUsuario Rol { get; set; } = RolUsuario.Estudiante;

        [EnumDataType(typeof(EstadoUsuario), ErrorMessage = "El estado del usuario no es vÃ¡lido.")]
        public EstadoUsuario Estado { get; set; } = EstadoUsuario.Activo;

        [Range(0, short.MaxValue, ErrorMessage = "Los intentos fallidos no pueden ser negativos.")]
        public short IntentosFallidos { get; set; }

        [Column(TypeName = "datetimeoffset")]
        public DateTimeOffset? BloqueadoHasta { get; set; }

        [ValidateNever]
        public ICollection<Reserva> ReservasComoEstudiante { get; set; } = new List<Reserva>();

        [ValidateNever]
        public ICollection<Reserva> ReservasResueltas { get; set; } = new List<Reserva>();
    }
}
