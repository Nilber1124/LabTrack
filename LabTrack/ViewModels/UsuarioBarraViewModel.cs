using LabTrack.Models;

namespace LabTrack.ViewModels
{
    public class UsuarioBarraViewModel
    {
        public string Nombre { get; set; } = string.Empty;
        public string Codigo { get; set; } = string.Empty;
        public string Rol { get; set; } = string.Empty;
        public EstadoUsuario Estado { get; set; }

        public string Iniciales => string.Concat(
            Nombre.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(2).Select(p => char.ToUpperInvariant(p[0])));
    }
}
