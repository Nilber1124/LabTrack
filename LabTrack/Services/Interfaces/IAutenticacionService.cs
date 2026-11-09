using LabTrack.ViewModels;

namespace LabTrack.Services.Interfaces
{
    public interface IAutenticacionService
    {
        Task<ResultadoAutenticacion> AutenticarAsync(string? codigo, string? contrasena, CancellationToken cancellationToken = default);
        Task<bool> EstaActivoAsync(int idUsuario, CancellationToken cancellationToken = default);
        Task<UsuarioBarraViewModel?> ObtenerResumenAsync(int idUsuario, CancellationToken cancellationToken = default);
    }
}
