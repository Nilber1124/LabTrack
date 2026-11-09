namespace LabTrack.Services
{
    public sealed class DatosEquipo
    {
        public int IdEquipo { get; init; }
        public string Nombre { get; init; } = string.Empty;
        public string CodigoInventario { get; init; } = string.Empty;
        public int IdCategoria { get; init; }
        public string Laboratorio { get; init; } = string.Empty;
    }
}
