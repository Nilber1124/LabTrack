namespace LabTrack.ViewModels
{
    public class SolicitudPendienteViewModel
    {
        public int IdReserva { get; set; }
        public string Estudiante { get; set; } = string.Empty;
        public string CodigoEstudiante { get; set; } = string.Empty;
        public string Equipo { get; set; } = string.Empty;
        public string CodigoInventario { get; set; } = string.Empty;
        public DateTime Inicio { get; set; }
        public DateTime Fin { get; set; }
    }
}
