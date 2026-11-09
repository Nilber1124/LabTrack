using LabTrack.Models;

namespace LabTrack.ViewModels
{
    public class EscenarioVistaPreviaViewModel
    {
        public List<SolicitudSimuladaViewModel> Solicitudes { get; set; } = [];
    }

    public class SolicitudSimuladaViewModel
    {
        public SolicitudPendienteViewModel Solicitud { get; set; } = new();
        public EstadoReserva Estado { get; set; } = EstadoReserva.Pendiente;
        public string? Motivo { get; set; }
    }
}
