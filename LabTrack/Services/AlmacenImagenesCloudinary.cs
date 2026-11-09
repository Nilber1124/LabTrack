using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using LabTrack.Reglas;
using LabTrack.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace LabTrack.Services
{
    public sealed class AlmacenImagenesCloudinary : IAlmacenImagenes
    {
        private const string Carpeta = "labtrack/equipos";

        private readonly Cloudinary? _cloudinary;
        private readonly ILogger<AlmacenImagenesCloudinary> _logger;

        public AlmacenImagenesCloudinary(IOptions<CloudinaryOpciones> opciones, ILogger<AlmacenImagenesCloudinary> logger)
        {
            _logger = logger;

            // Sin credenciales la aplicación funciona igual; solo se rechazan las fotos.
            var datos = opciones.Value;
            if (datos.EstaConfigurado)
            {
                _cloudinary = new Cloudinary(new Account(datos.CloudName, datos.ApiKey, datos.ApiSecret));
                _cloudinary.Api.Secure = true;
            }
        }

        public async Task<string?> GuardarAsync(Stream contenido, string extension, CancellationToken cancellationToken = default)
        {
            if (_cloudinary is null)
            {
                _logger.LogWarning("No se puede subir la imagen: falta la configuración de Cloudinary.");
                return null;
            }

            var parametros = new ImageUploadParams
            {
                File = new FileDescription($"{Guid.NewGuid():N}{extension}", contenido),
                Folder = Carpeta,
                UseFilename = false,
                UniqueFilename = true,
                Overwrite = false
            };

            try
            {
                var resultado = await _cloudinary.UploadAsync(parametros, cancellationToken);
                if (resultado.Error is not null || resultado.SecureUrl is null)
                {
                    _logger.LogError("Cloudinary rechazó la imagen: {Error}", resultado.Error?.Message);
                    return null;
                }

                return resultado.SecureUrl.ToString();
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "No se pudo conectar con Cloudinary para subir la imagen.");
                return null;
            }
        }

        public async Task EliminarAsync(string url, CancellationToken cancellationToken = default)
        {
            var publicId = RutaCloudinary.ObtenerPublicId(url);
            if (_cloudinary is null || publicId is null)
            {
                return;
            }

            try
            {
                var resultado = await _cloudinary.DestroyAsync(new DeletionParams(publicId));
                if (resultado.Error is not null)
                {
                    _logger.LogWarning("Cloudinary no eliminó {PublicId}: {Error}", publicId, resultado.Error.Message);
                }
            }
            catch (HttpRequestException ex)
            {
                // Una imagen huérfana no debe hacer fallar una operación ya guardada.
                _logger.LogWarning(ex, "No se pudo conectar con Cloudinary para eliminar {PublicId}.", publicId);
            }
        }
    }
}
