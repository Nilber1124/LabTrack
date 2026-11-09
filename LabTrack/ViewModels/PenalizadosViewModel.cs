namespace LabTrack.ViewModels
{
    public class PenalizadosViewModel
    {
        public IReadOnlyList<PenalizadoViewModel> Penalizados { get; set; } = [];
        public string? Busqueda { get; set; }
        public PaginacionViewModel Paginacion { get; set; } = new();
    }
}
