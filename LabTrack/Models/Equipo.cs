using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LabTrack.Models
{
    // El estado Disponible/Prestado no se guarda: se calcula desde las reservas (ver Reglas/EstadoPrestamo).
    public class Equipo
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdEquipo { get; set; }

        [Required(ErrorMessage = "El nombre del equipo es obligatorio.")]
        [StringLength(120, ErrorMessage = "El nombre no puede exceder los 120 caracteres.")]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El código de inventario es obligatorio.")]
        [StringLength(30, ErrorMessage = "El código de inventario no puede exceder los 30 caracteres.")]
        public string CodigoInventario { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "Selecciona una categoría válida.")]
        public int IdCategoria { get; set; }

        [Timestamp]
        public byte[] Version { get; set; } = [];

        [ValidateNever]
        public Categoria Categoria { get; set; } = null!;

        [ValidateNever]
        public ICollection<Reserva> Reservas { get; set; } = new List<Reserva>();
    }
}
