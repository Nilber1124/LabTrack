namespace LabTrack.Services
{
    public sealed class UsuarioAutenticado
    {
        public int IdUsuario { get; init; }
        public string Codigo { get; init; } = string.Empty;
        public string Nombre { get; init; } = string.Empty;
        public string Rol { get; init; } = string.Empty;
    }
}
