using LabTrack.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace LabTrack.Tests.Services
{
    // La conexión real con Cloudinary no se prueba aquí: una prueba unitaria no usa la red.
    public class AlmacenImagenesCloudinaryTests
    {
        private static AlmacenImagenesCloudinary SinCredenciales()
            => new(Options.Create(new CloudinaryOpciones()), NullLogger<AlmacenImagenesCloudinary>.Instance);

        [Fact]
        [Trait("HU", "ADM")]
        [Trait("CA", "ADM-CA3")]
        public async Task GuardarAsync_SinCredenciales_DevuelveNull()
        {
            using var contenido = new MemoryStream([0xFF, 0xD8, 0xFF]);

            Assert.Null(await SinCredenciales().GuardarAsync(contenido, ".jpg"));
        }

        [Fact]
        [Trait("HU", "ADM")]
        [Trait("CA", "ADM-CA3")]
        public async Task EliminarAsync_SinCredenciales_NoFalla()
        {
            await SinCredenciales().EliminarAsync("https://res.cloudinary.com/demo/image/upload/v1/abc.jpg");
        }

        [Theory]
        [Trait("HU", "ADM")]
        [Trait("CA", "ADM-CA3")]
        [InlineData(null, "clave", "secreto", false)]
        [InlineData("nube", " ", "secreto", false)]
        [InlineData("nube", "clave", null, false)]
        [InlineData("nube", "clave", "secreto", true)]
        public void EstaConfigurado_SegunCredenciales_DevuelveEsperado(string? nube, string? clave, string? secreto, bool esperado)
        {
            var opciones = new CloudinaryOpciones { CloudName = nube, ApiKey = clave, ApiSecret = secreto };

            Assert.Equal(esperado, opciones.EstaConfigurado);
        }
    }
}
