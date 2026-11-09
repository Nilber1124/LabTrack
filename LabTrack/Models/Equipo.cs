using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LabTrack.Models
{
    [Table("equipos")]
    public class Equipo
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        [Required(ErrorMessage = "El cÃ³digo de inventario es obligatorio.")]
        [StringLength(30, ErrorMessage = "El cÃ³digo de inventario no puede exceder los 30 caracteres.")]
        public string CodigoInventario { get; set; } = string.Empty;

        [Required(ErrorMessage = "El nombre del equipo es obligatorio.")]
        [StringLength(120, ErrorMessage = "El nombre no puede exceder los 120 caracteres.")]
        public string Nombre { get; set; } = string.Empty;

        [Range(1, short.MaxValue, ErrorMessage = "Selecciona una categorÃ­a vÃ¡lida.")]
        public short CategoriaId { get; set; }

        [ValidateNever]
        public Categoria Categoria { get; set; } = null!;

        [ValidateNever]
        public ICollection<Reserva> Reservas { get; set; } = new List<Reserva>();
    }
}
