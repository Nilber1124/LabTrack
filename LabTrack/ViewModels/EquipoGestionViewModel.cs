namespace LabTrack.ViewModels
{
    public class EquipoGestionViewModel
    {
        public int IdEquipo { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string CodigoInventario { get; set; } = string.Empty;
        public int IdCategoria { get; set; }
        public string Categoria { get; set; } = string.Empty;
        public string Laboratorio { get; set; } = string.Empty;
        public string? FotoUrl { get; set; }
        public bool DeBaja { get; set; }
        public bool Prestado { get; set; }

        public string Situacion => DeBaja ? "baja" : Prestado ? "prestado" : "disponible";
    }
}
