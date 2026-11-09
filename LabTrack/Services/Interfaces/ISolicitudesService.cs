using LabTrack.ViewModels;

namespace LabTrack.Services.Interfaces
{
    public enum EstadoResolucion { Completada, NoEncontrada, YaResuelta, ConflictoHorario, MotivoInvalido, EstudianteInvalido }

    public interface ISolicitudesService
    {
        Task<SolicitudesListadoViewModel> ListarPendientesAsync(long encargadoId, int pagina, string? busqueda = null, CancellationToken cancellationToken = default);
        Task<SolicitudPendienteViewModel?> ObtenerPendienteAsync(long encargadoId, long id, CancellationToken cancellationToken = default);
        Task<EstadoResolucion> AprobarAsync(long encargadoId, long id, CancellationToken cancellationToken = default);
        Task<EstadoResolucion> RechazarAsync(long encargadoId, long id, string motivo, CancellationToken cancellationToken = default);
    }
}
