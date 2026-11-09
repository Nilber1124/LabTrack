using LabTrack.ViewModels;

namespace LabTrack.Services.Interfaces
{
    public interface IDevolucionService
    {
        Task<DevolucionesViewModel> ListarActivasAsync(string? busqueda, int pagina, CancellationToken cancellationToken = default);
        Task<ResultadoOperacion> MarcarDevolucionAsync(int idReserva, CancellationToken cancellationToken = default);
        Task<PenalizadosViewModel> ListarPenalizadosAsync(string? busqueda, int pagina, CancellationToken cancellationToken = default);
        Task<ResultadoOperacion> LevantarPenalizacionAsync(int idUsuario, CancellationToken cancellationToken = default);
    }
}
