using LabTrack.Models;
using LabTrack.Reglas;

namespace LabTrack.ViewModels
{
    public class UsuarioBarraViewModel
    {
        public string Nombre { get; set; } = string.Empty;
        public string Codigo { get; set; } = string.Empty;
        public string Rol { get; set; } = string.Empty;
        public EstadoUsuario Estado { get; set; }

        // Los estudiantes se identifican por su código; el personal, por su rol.
        public string EtiquetaSecundaria => Rol switch
        {
            NombresRol.Administrador => "Administrador",
            NombresRol.Encargado => "Encargado",
            _ => Codigo
        };

        public string Iniciales => string.Concat(
            Nombre.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(2).Select(p => char.ToUpperInvariant(p[0])));
    }
}
