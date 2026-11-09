namespace LabTrack.ViewModels
{
    public class SolicitudesViewModel
    {
        public IReadOnlyList<SolicitudPendienteViewModel> Solicitudes { get; set; } = [];
        public PaginacionViewModel Paginacion { get; set; } = new();
        public string? AntiguedadMayor { get; set; }
    }
}
