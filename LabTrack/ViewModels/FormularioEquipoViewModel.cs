using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace LabTrack.ViewModels
{
    public class FormularioEquipoViewModel
    {
        // Debe llegar en el formulario (0 = nuevo); si falta, el modelo es inválido.
        [BindRequired]
        public int IdEquipo { get; set; }

        [Required(ErrorMessage = "Escribe el nombre del equipo")]
        [StringLength(120, ErrorMessage = "El nombre no puede exceder los 120 caracteres")]
        [Display(Name = "Nombre del equipo")]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "Escribe el código de inventario")]
        [StringLength(30, ErrorMessage = "El código no puede exceder los 30 caracteres")]
        [RegularExpression(@"^[A-Za-z0-9-]+$", ErrorMessage = "El código solo admite letras, números y guiones")]
        [Display(Name = "Código de inventario")]
        public string CodigoInventario { get; set; } = string.Empty;

        [Required(ErrorMessage = "Selecciona la categoría")]
        [Display(Name = "Categoría")]
        public int? IdCategoria { get; set; }

        [Required(ErrorMessage = "Selecciona el laboratorio")]
        [Display(Name = "Laboratorio")]
        public string Laboratorio { get; set; } = string.Empty;

        [ValidateNever]
        [Display(Name = "Foto del equipo")]
        public IFormFile? Foto { get; set; }

        [BindNever]
        public string? FotoActual { get; set; }

        [BindNever]
        public IReadOnlyList<CategoriaFiltroViewModel> Categorias { get; set; } = [];

        public bool EsNuevo => IdEquipo == 0;
    }
}
