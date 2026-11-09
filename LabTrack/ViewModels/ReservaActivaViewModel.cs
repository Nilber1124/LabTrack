namespace LabTrack.ViewModels
{
    public class ReservaActivaViewModel
    {
        public int IdReserva { get; set; }
        public int IdUsuario { get; set; }
        public string Estudiante { get; set; } = string.Empty;
        public string CodigoEstudiante { get; set; } = string.Empty;
        public string Equipo { get; set; } = string.Empty;
        public string CodigoInventario { get; set; } = string.Empty;
        public DateTime Inicio { get; set; }
        public DateTime Fin { get; set; }
        public bool Vencida { get; set; }
        public string Situacion { get; set; } = string.Empty;
        public string Nota { get; set; } = string.Empty;
    }
}
