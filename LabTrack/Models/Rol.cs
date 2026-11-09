using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LabTrack.Models
{
    public class Rol
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdRol { get; set; }

        [Required(ErrorMessage = "El nombre del rol es obligatorio.")]
        [StringLength(20, ErrorMessage = "El nombre del rol no puede exceder los 20 caracteres.")]
        public string Nombre { get; set; } = string.Empty;

        [ValidateNever]
        public ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();
    }
}
