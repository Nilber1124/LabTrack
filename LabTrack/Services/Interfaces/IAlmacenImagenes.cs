namespace LabTrack.Services.Interfaces
{
    // Punto de cambio para conectar Cloudinary sin tocar el resto de la aplicación.
    public interface IAlmacenImagenes
    {
        Task<string> GuardarAsync(Stream contenido, string extension, CancellationToken cancellationToken = default);
        Task EliminarAsync(string url, CancellationToken cancellationToken = default);
    }
}
