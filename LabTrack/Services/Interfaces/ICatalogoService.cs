using LabTrack.ViewModels;

namespace LabTrack.Services.Interfaces
{
    public interface ICatalogoService
    {
        Task<CatalogoViewModel> ListarAsync(string? categoria, int pagina, CancellationToken cancellationToken = default);
    }
}
