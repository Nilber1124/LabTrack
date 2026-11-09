using LabTrack.ViewModels;

namespace LabTrack.Services.Interfaces
{
    public interface IEquipoService
    {
        Task<GestionEquiposViewModel> ListarAsync(string? busqueda, string? categoria, int pagina, CancellationToken cancellationToken = default);
        Task<FormularioEquipoViewModel> ObtenerFormularioNuevoAsync(CancellationToken cancellationToken = default);
        Task<FormularioEquipoViewModel?> ObtenerFormularioEdicionAsync(int idEquipo, CancellationToken cancellationToken = default);
        Task<ResultadoOperacion> GuardarAsync(DatosEquipo datos, ImagenSubida? imagen, CancellationToken cancellationToken = default);
        Task<ResultadoOperacion> DarDeBajaAsync(int idEquipo, CancellationToken cancellationToken = default);
        Task<ResultadoOperacion> ReactivarAsync(int idEquipo, CancellationToken cancellationToken = default);
    }
}
