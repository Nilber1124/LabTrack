namespace LabTrack.ViewModels
{
    public class GestionEquiposViewModel
    {
        public IReadOnlyList<EquipoGestionViewModel> Equipos { get; set; } = [];
        public IReadOnlyList<CategoriaFiltroViewModel> Categorias { get; set; } = [];
        public string? CategoriaSeleccionada { get; set; }
        public string? Busqueda { get; set; }
        public int TotalDisponibles { get; set; }
        public int TotalDeBaja { get; set; }
        public FormularioEquipoViewModel Formulario { get; set; } = new();
        public PaginacionViewModel Paginacion { get; set; } = new();

        public int TotalGeneral => Categorias.Sum(c => c.Total);
    }
}
