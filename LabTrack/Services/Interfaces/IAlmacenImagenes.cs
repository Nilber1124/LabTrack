namespace LabTrack.Services.Interfaces
{
    public interface IAlmacenImagenes
    {
        // Devuelve la URL pública, o null si la imagen no se pudo guardar.
        Task<string?> GuardarAsync(Stream contenido, string extension, CancellationToken cancellationToken = default);
        Task EliminarAsync(string url, CancellationToken cancellationToken = default);
    }
}
