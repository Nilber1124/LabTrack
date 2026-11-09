using LabTrack.Tests.Soporte;
using LabTrack.ViewModels;

namespace LabTrack.Tests.ViewModels
{
    public class CrearReservaViewModelTests
    {
        private static CrearReservaViewModel Valido() => new()
        {
            IdEquipo = 1,
            Fecha = new DateOnly(2026, 11, 10),
            HoraInicio = new TimeOnly(10, 0),
            HoraFin = new TimeOnly(12, 0)
        };

        [Fact]
        [Trait("HU", "HU-03")]
        [Trait("CA", "CA-08")]
        public void Validar_CamposCompletos_NoTieneErrores()
        {
            Assert.Empty(Validacion.Errores(Valido()));
        }

        [Fact]
        [Trait("HU", "HU-03")]
        [Trait("CA", "CA-08")]
        public void Validar_SinFecha_TieneError()
        {
            var modelo = Valido();
            modelo.Fecha = null;

            Assert.Contains(Validacion.Errores(modelo), e => e.MemberNames.Contains(nameof(modelo.Fecha)));
        }

        [Fact]
        [Trait("HU", "HU-03")]
        [Trait("CA", "CA-08")]
        public void Validar_SinHoraInicio_TieneError()
        {
            var modelo = Valido();
            modelo.HoraInicio = null;

            Assert.Contains(Validacion.Errores(modelo), e => e.MemberNames.Contains(nameof(modelo.HoraInicio)));
        }

        [Fact]
        [Trait("HU", "HU-03")]
        [Trait("CA", "CA-08")]
        public void Validar_SinHoraFin_TieneError()
        {
            var modelo = Valido();
            modelo.HoraFin = null;

            Assert.Contains(Validacion.Errores(modelo), e => e.MemberNames.Contains(nameof(modelo.HoraFin)));
        }

        [Fact]
        [Trait("HU", "HU-03")]
        [Trait("CA", "CA-08")]
        public void Validar_SinEquipo_TieneError()
        {
            var modelo = Valido();
            modelo.IdEquipo = 0;

            Assert.Contains(Validacion.Errores(modelo), e => e.MemberNames.Contains(nameof(modelo.IdEquipo)));
        }
    }
}
