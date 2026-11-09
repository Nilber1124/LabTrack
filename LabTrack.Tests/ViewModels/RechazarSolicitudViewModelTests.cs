using LabTrack.Tests.Soporte;
using LabTrack.ViewModels;

namespace LabTrack.Tests.ViewModels
{
    public class RechazarSolicitudViewModelTests
    {
        [Fact]
        [Trait("HU", "HU-04")]
        [Trait("CA", "CA-13")]
        public void Validar_ConMotivo_NoTieneErrores()
        {
            var modelo = new RechazarSolicitudViewModel { IdReserva = 1, Motivo = "Equipo en mantenimiento" };

            Assert.Empty(Validacion.Errores(modelo));
        }

        [Theory]
        [Trait("HU", "HU-04")]
        [Trait("CA", "CA-13")]
        [InlineData("")]
        [InlineData("     ")]
        public void Validar_MotivoEnBlanco_TieneError(string motivo)
        {
            var modelo = new RechazarSolicitudViewModel { IdReserva = 1, Motivo = motivo };

            Assert.Contains(Validacion.Errores(modelo), e => e.MemberNames.Contains(nameof(modelo.Motivo)));
        }

        [Fact]
        [Trait("HU", "HU-04")]
        [Trait("CA", "CA-13")]
        public void Validar_MotivoDe200Caracteres_NoTieneErrores()
        {
            var modelo = new RechazarSolicitudViewModel { IdReserva = 1, Motivo = new string('x', 200) };

            Assert.Empty(Validacion.Errores(modelo));
        }

        [Fact]
        [Trait("HU", "HU-04")]
        [Trait("CA", "CA-13")]
        public void Validar_MotivoDe201Caracteres_TieneError()
        {
            var modelo = new RechazarSolicitudViewModel { IdReserva = 1, Motivo = new string('x', 201) };

            Assert.Contains(Validacion.Errores(modelo), e => e.MemberNames.Contains(nameof(modelo.Motivo)));
        }
    }
}
