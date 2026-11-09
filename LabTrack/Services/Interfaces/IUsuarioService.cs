using LabTrack.ViewModels;

namespace LabTrack.Services.Interfaces
{
    public interface IUsuarioService
    {
        Task<GestionUsuariosViewModel> ListarAsync(string? busqueda, string? rol, int pagina, CancellationToken cancellationToken = default);
        Task<FormularioUsuarioViewModel?> ObtenerFormularioEdicionAsync(int idUsuario, CancellationToken cancellationToken = default);
        Task<ResultadoOperacion> GuardarAsync(DatosUsuario datos, CancellationToken cancellationToken = default);
        Task<ResultadoOperacion> DarDeBajaAsync(int idUsuario, CancellationToken cancellationToken = default);
        Task<ResultadoOperacion> ReactivarAsync(int idUsuario, CancellationToken cancellationToken = default);
    }
}
