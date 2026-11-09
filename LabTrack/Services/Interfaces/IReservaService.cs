using LabTrack.Models;
using LabTrack.ViewModels;

namespace LabTrack.Services.Interfaces
{
    public interface IReservaService
    {
        Task<CrearReservaViewModel?> ObtenerFormularioAsync(int idEquipo, CancellationToken cancellationToken = default);
        Task<ResultadoOperacion> CrearAsync(int idUsuario, int idEquipo, DateOnly fecha, TimeOnly horaInicio, TimeOnly horaFin, CancellationToken cancellationToken = default);
        Task<MisReservasViewModel> ListarPropiasAsync(int idUsuario, EstadoReserva? estado, int pagina, CancellationToken cancellationToken = default);
    }
}
