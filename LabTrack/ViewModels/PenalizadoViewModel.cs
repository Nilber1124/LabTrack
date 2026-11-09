namespace LabTrack.ViewModels
{
    public class PenalizadoViewModel
    {
        public int IdUsuario { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Codigo { get; set; } = string.Empty;
        public DateTime? FechaPenalizacion { get; set; }
        public string Motivo { get; set; } = string.Empty;
    }
}
