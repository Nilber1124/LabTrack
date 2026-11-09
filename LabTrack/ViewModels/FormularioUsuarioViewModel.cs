using LabTrack.Reglas;
using System.ComponentModel.DataAnnotations;

namespace LabTrack.ViewModels
{
    public class FormularioUsuarioViewModel : IValidatableObject
    {
        public int IdUsuario { get; set; }

        [Required(ErrorMessage = "Escribe el código del usuario")]
        [StringLength(20, ErrorMessage = "El código no puede exceder los 20 caracteres")]
        [RegularExpression(@"^[A-Za-z0-9-]+$", ErrorMessage = "El código solo admite letras, números y guiones")]
        [Display(Name = "Código")]
        public string Codigo { get; set; } = string.Empty;

        [Required(ErrorMessage = "Escribe el nombre del usuario")]
        [StringLength(120, ErrorMessage = "El nombre no puede exceder los 120 caracteres")]
        [Display(Name = "Nombre completo")]
        public string Nombre { get; set; } = string.Empty;

        [Display(Name = "Rol")]
        public string? Rol { get; set; }

        [StringLength(100, MinimumLength = 8, ErrorMessage = "La contraseña debe tener entre 8 y 100 caracteres")]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña")]
        public string? Contrasena { get; set; }

        [Compare(nameof(Contrasena), ErrorMessage = "Las contraseñas no coinciden")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirmar contraseña")]
        public string? ConfirmarContrasena { get; set; }

        public bool EsNuevo => IdUsuario == 0;

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!EsNuevo)
            {
                yield break;
            }

            if (!NombresRol.Asignables.Contains(Rol))
            {
                yield return new ValidationResult(Mensajes.RolNoValido, [nameof(Rol)]);
            }

            if (string.IsNullOrEmpty(Contrasena))
            {
                yield return new ValidationResult(Mensajes.ContrasenaObligatoria, [nameof(Contrasena)]);
            }
        }
    }
}
