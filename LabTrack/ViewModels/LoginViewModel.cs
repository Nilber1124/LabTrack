using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.ComponentModel.DataAnnotations;

namespace LabTrack.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Ingresa tu código de estudiante")]
        [StringLength(20, ErrorMessage = "El código no puede exceder los 20 caracteres")]
        [Display(Name = "Código de estudiante")]
        public string Codigo { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ingresa tu contraseña")]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña")]
        public string Contrasena { get; set; } = string.Empty;

        [BindNever]
        public string? AlertaSeguridad { get; set; }

        [BindNever]
        public DateTime? BloqueadoHasta { get; set; }

        [BindNever]
        public int SegundosRestantes { get; set; }

        public bool EstaBloqueada => AlertaSeguridad is not null;
    }
}
