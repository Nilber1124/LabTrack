using LabTrack.Models;
using LabTrack.Reglas;

namespace LabTrack.ViewModels
{
    public class UsuarioGestionViewModel
    {
        public int IdUsuario { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Rol { get; set; } = string.Empty;
        public EstadoUsuario Estado { get; set; }
        public bool DeBaja { get; set; }
        public bool TieneReservaActiva { get; set; }

        public string RolEtiqueta => Rol == NombresRol.Encargado ? "Encargado" : "Estudiante";

        public string Situacion => DeBaja ? "baja" : Estado == EstadoUsuario.Penalizado ? "penalizado" : "activo";
    }
}
