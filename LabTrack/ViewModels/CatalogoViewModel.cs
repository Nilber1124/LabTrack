namespace LabTrack.ViewModels
{
    public class CatalogoViewModel
    {
        public IReadOnlyList<EquipoCatalogoViewModel> Equipos { get; set; } = [];
        public IReadOnlyList<CategoriaFiltroViewModel> Categorias { get; set; } = [];
        public string? CategoriaSeleccionada { get; set; }
        public string? Busqueda { get; set; }
        public int TotalDisponibles { get; set; }
        public PaginacionViewModel Paginacion { get; set; } = new();
        public string? MensajeVacio { get; set; }

        public bool SinResultados => Equipos.Count == 0;
        public int TotalGeneral => Categorias.Sum(c => c.Total);
    }
}
