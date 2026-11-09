using LabTrack.Models;

namespace LabTrack.ViewModels
{
    public class MisReservasViewModel
    {
        public IReadOnlyList<ReservaListadoViewModel> Reservas { get; set; } = [];
        public IReadOnlyList<EstadoFiltroViewModel> Estados { get; set; } = [];
        public EstadoReserva? EstadoSeleccionado { get; set; }
        public bool EstudiantePenalizado { get; set; }
        public PaginacionViewModel Paginacion { get; set; } = new();

        public int TotalGeneral => Estados.Sum(e => e.Total);
        public int TotalPendientes => Estados.Where(e => e.Estado == EstadoReserva.Pendiente).Sum(e => e.Total);
    }
}
