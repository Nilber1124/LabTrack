using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LabTrack.Models
{
    [Table("categorias")]
    public class Categoria
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public short Id { get; set; }

        [Required(ErrorMessage = "El nombre de la categorÃ­a es obligatorio.")]
        [StringLength(50, ErrorMessage = "El nombre no puede exceder los 50 caracteres.")]
        public string Nombre { get; set; } = string.Empty;

        [ValidateNever]
        public ICollection<Equipo> Equipos { get; set; } = new List<Equipo>();
    }
}
