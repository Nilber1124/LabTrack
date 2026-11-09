using LabTrack.Models;
using LabTrack.ViewModels;

namespace LabTrack.Services.Interfaces
{
    public interface IReservaService
    {
        Task<CrearReservaViewModel?> ObtenerFormularioAsync(int idUsuario, int idEquipo, DateOnly? fecha = null, CancellationToken cancellationToken = default);
        Task<ResultadoOperacion> CrearAsync(int idUsuario, int idEquipo, DateOnly fecha, TimeOnly horaInicio, TimeOnly horaFin, CancellationToken cancellationToken = default);
        Task<MisReservasViewModel> ListarPropiasAsync(int idUsuario, EstadoReserva? estado, int pagina, CancellationToken cancellationToken = default);
        Task<SolicitudesViewModel> ListarPendientesAsync(int pagina, CancellationToken cancellationToken = default);
        Task<SolicitudPendienteViewModel?> ObtenerPendienteAsync(int idReserva, CancellationToken cancellationToken = default);
        Task<ResultadoOperacion> AprobarAsync(int idReserva, CancellationToken cancellationToken = default);
        Task<ResultadoOperacion> RechazarAsync(int idReserva, string? motivo, CancellationToken cancellationToken = default);
    }
}
