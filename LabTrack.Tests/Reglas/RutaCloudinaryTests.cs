using LabTrack.Reglas;

namespace LabTrack.Tests.Reglas
{
    public class RutaCloudinaryTests
    {
        [Theory]
        [Trait("HU", "ADM")]
        [Trait("CA", "ADM-CA3")]
        [InlineData("https://res.cloudinary.com/demo/image/upload/v1712/labtrack/equipos/abc123.jpg", "labtrack/equipos/abc123")]
        [InlineData("https://res.cloudinary.com/demo/image/upload/labtrack/equipos/abc123.png", "labtrack/equipos/abc123")]
        [InlineData("http://res.cloudinary.com/demo/image/upload/v1/abc123", "abc123")]
        public void ObtenerPublicId_UrlDeCloudinary_DevuelveElIdentificador(string url, string esperado)
        {
            Assert.Equal(esperado, RutaCloudinary.ObtenerPublicId(url));
        }

        [Theory]
        [Trait("HU", "ADM")]
        [Trait("CA", "ADM-CA3")]
        [InlineData("/uploads/equipos/antigua.png")]
        [InlineData("https://otro.com/image/upload/v1/abc.jpg")]
        [InlineData("https://res.cloudinary.com/demo/image/fetch/abc.jpg")]
        [InlineData("https://res.cloudinary.com/demo/image/upload/")]
        [InlineData("")]
        public void ObtenerPublicId_UrlAjena_DevuelveNull(string url)
        {
            Assert.Null(RutaCloudinary.ObtenerPublicId(url));
        }
    }
}
