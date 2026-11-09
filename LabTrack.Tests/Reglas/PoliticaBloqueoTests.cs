using LabTrack.Reglas;

namespace LabTrack.Tests.Reglas
{
    public class PoliticaBloqueoTests
    {
        private static readonly DateTime Ahora = new(2026, 11, 9, 10, 0, 0);

        [Theory]
        [Trait("HU", "HU-01")]
        [Trait("CA", "CA-03")]
        [InlineData(0, false)]
        [InlineData(1, false)]
        [InlineData(2, false)]
        [InlineData(3, true)]
        [InlineData(4, true)]
        public void AlcanzoLimite_SegunIntentos_DevuelveEsperado(int intentos, bool esperado)
        {
            Assert.Equal(esperado, PoliticaBloqueo.AlcanzoLimite(intentos));
        }

        [Fact]
        [Trait("HU", "HU-01")]
        [Trait("CA", "CA-03")]
        public void EstaBloqueada_SinBloqueo_DevuelveFalso()
        {
            Assert.False(PoliticaBloqueo.EstaBloqueada(null, Ahora));
        }

        [Fact]
        [Trait("HU", "HU-01")]
        [Trait("CA", "CA-03")]
        public void EstaBloqueada_UnSegundoAntesDeVencer_DevuelveVerdadero()
        {
            Assert.True(PoliticaBloqueo.EstaBloqueada(Ahora.AddSeconds(1), Ahora));
        }

        [Fact]
        [Trait("HU", "HU-01")]
        [Trait("CA", "CA-03")]
        public void EstaBloqueada_ExactamenteAlVencer_DevuelveFalso()
        {
            Assert.False(PoliticaBloqueo.EstaBloqueada(Ahora, Ahora));
        }

        [Fact]
        [Trait("HU", "HU-01")]
        [Trait("CA", "CA-03")]
        public void BloqueoVencido_SinBloqueo_DevuelveFalso()
        {
            Assert.False(PoliticaBloqueo.BloqueoVencido(null, Ahora));
        }

        [Fact]
        [Trait("HU", "HU-01")]
        [Trait("CA", "CA-03")]
        public void BloqueoVencido_ExactamenteAlVencer_DevuelveVerdadero()
        {
            Assert.True(PoliticaBloqueo.BloqueoVencido(Ahora, Ahora));
        }

        [Fact]
        [Trait("HU", "HU-01")]
        [Trait("CA", "CA-03")]
        public void BloqueoVencido_AunVigente_DevuelveFalso()
        {
            Assert.False(PoliticaBloqueo.BloqueoVencido(Ahora.AddSeconds(1), Ahora));
        }
    }
}
