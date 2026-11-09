using LabTrack.Tests.Soporte;
using LabTrack.ViewModels;

namespace LabTrack.Tests.ViewModels
{
    public class LoginViewModelTests
    {
        [Fact]
        [Trait("HU", "HU-01")]
        [Trait("CA", "CA-01")]
        public void Validar_CamposCompletos_NoTieneErrores()
        {
            Assert.Empty(Validacion.Errores(new LoginViewModel { Codigo = "20231458", Contrasena = "clave" }));
        }

        [Theory]
        [Trait("HU", "HU-01")]
        [Trait("CA", "CA-02")]
        [InlineData("", "clave", nameof(LoginViewModel.Codigo))]
        [InlineData("   ", "clave", nameof(LoginViewModel.Codigo))]
        [InlineData("20231458", "", nameof(LoginViewModel.Contrasena))]
        public void Validar_CampoVacio_TieneErrorEnElCampo(string codigo, string contrasena, string campo)
        {
            var modelo = new LoginViewModel { Codigo = codigo, Contrasena = contrasena };

            Assert.Contains(Validacion.Errores(modelo), e => e.MemberNames.Contains(campo));
        }

        [Fact]
        [Trait("HU", "HU-01")]
        [Trait("CA", "CA-03")]
        public void EstaBloqueada_ConAlerta_DevuelveVerdadero()
        {
            Assert.True(new LoginViewModel { AlertaSeguridad = "alerta" }.EstaBloqueada);
        }

        [Fact]
        [Trait("HU", "HU-01")]
        [Trait("CA", "CA-03")]
        public void EstaBloqueada_SinAlerta_DevuelveFalso()
        {
            Assert.False(new LoginViewModel().EstaBloqueada);
        }
    }
}
