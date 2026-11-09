using LabTrack.Reglas;

namespace LabTrack.Tests.Reglas
{
    public class MensajesTests
    {
        [Fact]
        [Trait("HU", "HU-01")]
        [Trait("CA", "CA-02")]
        public void CredencialesInvalidas_TextoExigido_CoincideExactamente()
        {
            Assert.Equal("Credenciales inválidas", Mensajes.CredencialesInvalidas);
        }

        [Fact]
        [Trait("HU", "HU-02")]
        [Trait("CA", "CA-06")]
        public void SinEquiposEnCategoria_TextoExigido_CoincideExactamente()
        {
            Assert.Equal("No hay equipos disponibles en esta categoría", Mensajes.SinEquiposEnCategoria);
        }
    }
}
