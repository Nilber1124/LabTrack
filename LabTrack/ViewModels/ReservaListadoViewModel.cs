using LabTrack.Models;

namespace LabTrack.ViewModels
{
    public class ReservaListadoViewModel
    {
        public int IdReserva { get; set; }
        public string Equipo { get; set; } = string.Empty;
        public string CodigoInventario { get; set; } = string.Empty;
        public DateTime Inicio { get; set; }
        public DateTime Fin { get; set; }
        public EstadoReserva Estado { get; set; }
        public string? MotivoRechazo { get; set; }
    }
}
