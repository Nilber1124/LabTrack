using LabTrack.Services;

namespace LabTrack.Tests.Services
{
    public class ArchivoEnvTests
    {
        [Fact]
        [Trait("HU", "ADM")]
        [Trait("CA", "ADM-CA3")]
        public void Interpretar_ClaveYValor_LosDevuelve()
        {
            var valores = ArchivoEnv.Interpretar(["Cloudinary__ApiKey=abc123"]);

            Assert.Equal("abc123", valores["Cloudinary__ApiKey"]);
        }

        [Fact]
        [Trait("HU", "ADM")]
        [Trait("CA", "ADM-CA3")]
        public void Interpretar_ValorConIgualesYPuntoYComa_LoConservaCompleto()
        {
            var valores = ArchivoEnv.Interpretar(["Cadena=Server=x;Password=a=b!;"]);

            Assert.Equal("Server=x;Password=a=b!;", valores["Cadena"]);
        }

        [Theory]
        [Trait("HU", "ADM")]
        [Trait("CA", "ADM-CA3")]
        [InlineData("Nombre=\"Carlos Vega\"")]
        [InlineData("Nombre='Carlos Vega'")]
        [InlineData("  Nombre = Carlos Vega  ")]
        public void Interpretar_ComillasOEspacios_LosNormaliza(string linea)
        {
            Assert.Equal("Carlos Vega", ArchivoEnv.Interpretar([linea])["Nombre"]);
        }

        [Theory]
        [Trait("HU", "ADM")]
        [Trait("CA", "ADM-CA3")]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("# Clave=Comentario")]
        [InlineData("SinIgual")]
        [InlineData("=SinClave")]
        [InlineData("Vacia=")]
        public void Interpretar_LineaSinDato_LaIgnora(string linea)
        {
            Assert.Empty(ArchivoEnv.Interpretar([linea]));
        }
    }
}
