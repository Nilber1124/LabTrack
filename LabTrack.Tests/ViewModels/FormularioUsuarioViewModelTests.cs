using LabTrack.Reglas;
using LabTrack.Tests.Soporte;
using LabTrack.ViewModels;

namespace LabTrack.Tests.ViewModels
{
    public class FormularioUsuarioViewModelTests
    {
        private static FormularioUsuarioViewModel Nuevo() => new()
        {
            Codigo = "20240001",
            Nombre = "Ana Torres",
            Rol = NombresRol.Estudiante,
            Contrasena = "clave-segura",
            ConfirmarContrasena = "clave-segura"
        };

        private static IEnumerable<string> Campos(FormularioUsuarioViewModel modelo)
            => Validacion.Errores(modelo).SelectMany(e => e.MemberNames);

        [Fact]
        [Trait("HU", "ADM")]
        [Trait("CA", "ADM-USU")]
        public void Validar_NuevoCompleto_NoTieneErrores()
        {
            Assert.Empty(Validacion.Errores(Nuevo()));
        }

        [Fact]
        [Trait("HU", "ADM")]
        [Trait("CA", "ADM-USU")]
        public void Validar_NuevoSinContrasena_TieneError()
        {
            var modelo = Nuevo();
            modelo.Contrasena = null;
            modelo.ConfirmarContrasena = null;

            Assert.Contains(nameof(modelo.Contrasena), Campos(modelo));
        }

        [Theory]
        [Trait("HU", "ADM")]
        [Trait("CA", "ADM-USU")]
        [InlineData(null)]
        [InlineData("ADMINISTRADOR")]
        public void Validar_NuevoConRolNoAsignable_TieneError(string? rol)
        {
            var modelo = Nuevo();
            modelo.Rol = rol;

            Assert.Contains(nameof(modelo.Rol), Campos(modelo));
        }

        [Fact]
        [Trait("HU", "ADM")]
        [Trait("CA", "ADM-USU")]
        public void Validar_EdicionSinContrasenaNiRol_NoTieneErrores()
        {
            var modelo = new FormularioUsuarioViewModel { IdUsuario = 5, Codigo = "20240001", Nombre = "Ana Torres" };

            Assert.Empty(Validacion.Errores(modelo));
        }

        [Fact]
        [Trait("HU", "ADM")]
        [Trait("CA", "ADM-USU")]
        public void Validar_ContrasenasDistintas_TieneError()
        {
            var modelo = Nuevo();
            modelo.ConfirmarContrasena = "otra-clave";

            Assert.Contains(nameof(modelo.ConfirmarContrasena), Campos(modelo));
        }

        [Fact]
        [Trait("HU", "ADM")]
        [Trait("CA", "ADM-USU")]
        public void Validar_ContrasenaCorta_TieneError()
        {
            var modelo = Nuevo();
            modelo.Contrasena = "1234567";
            modelo.ConfirmarContrasena = "1234567";

            Assert.Contains(nameof(modelo.Contrasena), Campos(modelo));
        }

        [Fact]
        [Trait("HU", "ADM")]
        [Trait("CA", "ADM-USU")]
        public void Validar_CodigoConEspacios_TieneError()
        {
            var modelo = Nuevo();
            modelo.Codigo = "con espacios";

            Assert.Contains(nameof(modelo.Codigo), Campos(modelo));
        }
    }
}
