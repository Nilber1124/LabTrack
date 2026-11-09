using LabTrack.Services.Interfaces;

namespace LabTrack.Services
{
    public sealed class AlmacenImagenesLocal : IAlmacenImagenes
    {
        public const string RutaPublica = "/uploads";
        private const string Subcarpeta = "equipos";

        private readonly string _carpeta;

        public AlmacenImagenesLocal(IWebHostEnvironment entorno)
        {
            _carpeta = Path.Combine(CarpetaRaiz(entorno.ContentRootPath), Subcarpeta);
        }

        public static string CarpetaRaiz(string raizContenido) => Path.Combine(raizContenido, "uploads");

        public async Task<string> GuardarAsync(Stream contenido, string extension, CancellationToken cancellationToken = default)
        {
            Directory.CreateDirectory(_carpeta);
            var nombre = $"{Guid.NewGuid():N}{extension}";

            await using var destino = File.Create(Path.Combine(_carpeta, nombre));
            await contenido.CopyToAsync(destino, cancellationToken);

            return $"{RutaPublica}/{Subcarpeta}/{nombre}";
        }

        public Task EliminarAsync(string url, CancellationToken cancellationToken = default)
        {
            // GetFileName descarta cualquier ruta: nunca se borra fuera de la carpeta.
            var archivo = Path.Combine(_carpeta, Path.GetFileName(url));
            if (File.Exists(archivo))
            {
                File.Delete(archivo);
            }

            return Task.CompletedTask;
        }
    }
}
