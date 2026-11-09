namespace LabTrack.ViewModels
{
    public sealed class SolicitudPendienteViewModel
    {
        public long Id { get; set; }
        public string Estudiante { get; set; } = string.Empty;
        public string CodigoEstudiante { get; set; } = string.Empty;
        public string Equipo { get; set; } = string.Empty;
        public string CodigoInventario { get; set; } = string.Empty;
        public DateTimeOffset Inicio { get; set; }
        public DateTimeOffset Fin { get; set; }
        public bool EquipoPrestado { get; set; }
    }

    public sealed class SolicitudesListadoViewModel
    {
        public IReadOnlyList<SolicitudPendienteViewModel> Solicitudes { get; set; } = [];
        public int Pagina { get; set; }
        public int Total { get; set; }
        public int TamanoPagina { get; set; }
        public bool EsVistaPrevia { get; set; }
        public string Busqueda { get; set; } = string.Empty;
        public int TotalPaginas => Math.Max(1, (int)Math.Ceiling(Total / (double)TamanoPagina));
    }
}
