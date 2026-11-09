using LabTrack.Tests.Soporte;
using LabTrack.ViewModels;

namespace LabTrack.Tests.ViewModels
{
    public class FormularioEquipoViewModelTests
    {
        private static FormularioEquipoViewModel Valido() => new()
        {
            Nombre = "Analizador de espectro",
            CodigoInventario = "AE-0033",
            IdCategoria = 2,
            Laboratorio = "Lab. B-110"
        };

        [Fact]
        [Trait("HU", "ADM")]
        [Trait("CA", "ADM-CA3")]
        public void Validar_DatosCompletos_NoTieneErrores()
        {
            Assert.Empty(Validacion.Errores(Valido()));
        }

        [Fact]
        [Trait("HU", "ADM")]
        [Trait("CA", "ADM-CA3")]
        public void Validar_SinFoto_NoTieneErrores()
        {
            var modelo = Valido();
            modelo.Foto = null;

            Assert.Empty(Validacion.Errores(modelo));
        }

        [Theory]
        [Trait("HU", "ADM")]
        [Trait("CA", "ADM-CA3")]
        [InlineData("")]
        [InlineData("código con espacios")]
        public void Validar_CodigoInvalido_TieneError(string codigo)
        {
            var modelo = Valido();
            modelo.CodigoInventario = codigo;

            Assert.Contains(Validacion.Errores(modelo), e => e.MemberNames.Contains(nameof(modelo.CodigoInventario)));
        }

        [Fact]
        [Trait("HU", "ADM")]
        [Trait("CA", "ADM-CA3")]
        public void Validar_SinNombre_TieneError()
        {
            var modelo = Valido();
            modelo.Nombre = string.Empty;

            Assert.Contains(Validacion.Errores(modelo), e => e.MemberNames.Contains(nameof(modelo.Nombre)));
        }

        [Fact]
        [Trait("HU", "ADM")]
        [Trait("CA", "ADM-CA3")]
        public void Validar_SinCategoria_TieneError()
        {
            var modelo = Valido();
            modelo.IdCategoria = null;

            Assert.Contains(Validacion.Errores(modelo), e => e.MemberNames.Contains(nameof(modelo.IdCategoria)));
        }

        [Fact]
        [Trait("HU", "ADM")]
        [Trait("CA", "ADM-CA3")]
        public void Validar_SinLaboratorio_TieneError()
        {
            var modelo = Valido();
            modelo.Laboratorio = string.Empty;

            Assert.Contains(Validacion.Errores(modelo), e => e.MemberNames.Contains(nameof(modelo.Laboratorio)));
        }
    }
}
