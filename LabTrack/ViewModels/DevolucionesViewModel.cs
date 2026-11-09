namespace LabTrack.ViewModels
{
    public class DevolucionesViewModel
    {
        public IReadOnlyList<ReservaActivaViewModel> Reservas { get; set; } = [];
        public string? Busqueda { get; set; }
        public int TotalVencidas { get; set; }
        public DateTime Ahora { get; set; }
        public PaginacionViewModel Paginacion { get; set; } = new();
    }
}
