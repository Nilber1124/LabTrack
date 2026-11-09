using LabTrack.Models;

namespace LabTrack.ViewModels
{
    public class EquipoCatalogoViewModel
    {
        public int IdEquipo { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string CodigoInventario { get; set; } = string.Empty;
        public string Categoria { get; set; } = string.Empty;
        public EstadoEquipo Estado { get; set; }
        public DateTime? PrestadoHasta { get; set; }
    }
}
