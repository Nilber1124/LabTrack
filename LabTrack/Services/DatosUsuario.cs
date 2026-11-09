namespace LabTrack.Services
{
    public sealed class DatosUsuario
    {
        public int IdUsuario { get; init; }
        public string Codigo { get; init; } = string.Empty;
        public string Nombre { get; init; } = string.Empty;
        public string? Rol { get; init; }
        public string? Contrasena { get; init; }
    }
}
