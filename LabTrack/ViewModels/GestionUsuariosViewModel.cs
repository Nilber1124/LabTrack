namespace LabTrack.ViewModels
{
    public class GestionUsuariosViewModel
    {
        public IReadOnlyList<UsuarioGestionViewModel> Usuarios { get; set; } = [];
        public string? RolSeleccionado { get; set; }
        public string? Busqueda { get; set; }
        public int TotalEstudiantes { get; set; }
        public int TotalEncargados { get; set; }
        public int TotalActivos { get; set; }
        public int TotalDeBaja { get; set; }
        public PaginacionViewModel Paginacion { get; set; } = new();
        public FormularioUsuarioViewModel Formulario { get; set; } = new();

        public int TotalGeneral => TotalEstudiantes + TotalEncargados;
    }
}
