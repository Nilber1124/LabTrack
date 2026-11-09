using LabTrack.Reglas;

namespace LabTrack.Tests.Reglas
{
    public class ValidacionImagenTests
    {
        [Theory]
        [Trait("HU", "ADM")]
        [Trait("CA", "ADM-CA3")]
        [InlineData(1, true)]
        [InlineData(ValidacionImagen.TamanoMaximoBytes, true)]
        [InlineData(0, false)]
        [InlineData(ValidacionImagen.TamanoMaximoBytes + 1, false)]
        public void TamanoValido_SegunBytes_DevuelveEsperado(long bytes, bool esperado)
        {
            Assert.Equal(esperado, ValidacionImagen.TamanoValido(bytes));
        }

        [Fact]
        [Trait("HU", "ADM")]
        [Trait("CA", "ADM-CA3")]
        public void DetectarExtension_CabeceraJpeg_DevuelveJpg()
        {
            Assert.Equal(".jpg", ValidacionImagen.DetectarExtension(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }));
        }

        [Fact]
        [Trait("HU", "ADM")]
        [Trait("CA", "ADM-CA3")]
        public void DetectarExtension_CabeceraPng_DevuelvePng()
        {
            Assert.Equal(".png", ValidacionImagen.DetectarExtension(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }));
        }

        [Fact]
        [Trait("HU", "ADM")]
        [Trait("CA", "ADM-CA3")]
        public void DetectarExtension_OtroContenido_DevuelveNull()
        {
            Assert.Null(ValidacionImagen.DetectarExtension("<html>"u8));
        }

        [Fact]
        [Trait("HU", "ADM")]
        [Trait("CA", "ADM-CA3")]
        public void DetectarExtension_SinContenido_DevuelveNull()
        {
            Assert.Null(ValidacionImagen.DetectarExtension(ReadOnlySpan<byte>.Empty));
        }
    }
}
