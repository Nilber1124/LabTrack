using LabTrack.Models;

namespace LabTrack.ViewModels
{
    public class FichaEstudianteViewModel
    {
        public int IdUsuario { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Codigo { get; set; } = string.Empty;
        public EstadoUsuario Estado { get; set; }
        public int ReservasTotales { get; set; }
        public int DevolucionesTardias { get; set; }
        public string? ReservaActiva { get; set; }
        public string? DetallePenalizacion { get; set; }

        public string Iniciales => string.Concat(
            Nombre.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(2).Select(p => char.ToUpperInvariant(p[0])));
    }
}
