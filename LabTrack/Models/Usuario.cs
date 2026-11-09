using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LabTrack.Models
{
    public class Usuario
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdUsuario { get; set; }

        [Required(ErrorMessage = "El código del usuario es obligatorio.")]
        [StringLength(20, ErrorMessage = "El código no puede exceder los 20 caracteres.")]
        [RegularExpression(@"^[A-Za-z0-9-]+$", ErrorMessage = "El código solo admite letras, números y guiones.")]
        public string Codigo { get; set; } = string.Empty;

        [Required(ErrorMessage = "El nombre del usuario es obligatorio.")]
        [StringLength(120, ErrorMessage = "El nombre no puede exceder los 120 caracteres.")]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El hash de la contraseña es obligatorio.")]
        [StringLength(255, ErrorMessage = "El hash no puede exceder los 255 caracteres.")]
        public string PasswordHash { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "Selecciona un rol válido.")]
        public int IdRol { get; set; }

        [EnumDataType(typeof(EstadoUsuario), ErrorMessage = "El estado del usuario no es válido.")]
        public EstadoUsuario Estado { get; set; } = EstadoUsuario.Activo;

        [Range(0, int.MaxValue, ErrorMessage = "Los intentos fallidos no pueden ser negativos.")]
        public int IntentosFallidos { get; set; }

        public DateTime? BloqueadoHasta { get; set; }

        // Impide iniciar sesión y conserva el historial.
        public bool DeBaja { get; set; }

        [ValidateNever]
        public Rol Rol { get; set; } = null!;

        [ValidateNever]
        public ICollection<Reserva> Reservas { get; set; } = new List<Reserva>();
    }
}
