using LabTrack.Controllers;
using LabTrack.Reglas;
using LabTrack.Services;
using LabTrack.Services.Interfaces;
using LabTrack.Tests.Soporte;
using LabTrack.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using System.Security.Claims;

namespace LabTrack.Tests.Controllers
{
    public class LoginControllerTests
    {
        private readonly IAutenticacionService _servicio = Substitute.For<IAutenticacionService>();
        private readonly LoginController _controlador;
        private readonly IAuthenticationService _http;

        public LoginControllerTests()
        {
            _controlador = new LoginController(_servicio);
            _http = ContextoControlador.Configurar(_controlador);
        }

        private static LoginViewModel Formulario() => new() { Codigo = "20231458", Contrasena = "clave" };

        private void Responder(ResultadoAutenticacion resultado)
            => _servicio.AutenticarAsync("20231458", "clave", Arg.Any<CancellationToken>()).Returns(resultado);

        private static ResultadoAutenticacion Exitosa(string rol) => ResultadoAutenticacion.Exitosa(
            new UsuarioAutenticado { IdUsuario = 7, Codigo = "20231458", Nombre = "Valeria", Rol = rol });

        private Task SesionIniciada(int veces)
            => _http.Received(veces).SignInAsync(
                Arg.Any<HttpContext>(), Arg.Any<string>(), Arg.Any<ClaimsPrincipal>(), Arg.Any<AuthenticationProperties>());

        [Fact]
        [Trait("HU", "HU-01")]
        [Trait("CA", "CA-01")]
        public void Index_Anonimo_MuestraFormulario()
        {
            var resultado = Assert.IsType<ViewResult>(_controlador.Index());

            Assert.IsType<LoginViewModel>(resultado.Model);
        }

        [Fact]
        [Trait("HU", "HU-01")]
        [Trait("CA", "CA-01")]
        public void Index_EncargadoYaAutenticado_RedirigeASolicitudes()
        {
            ContextoControlador.Configurar(_controlador, 1, NombresRol.Encargado);

            var resultado = Assert.IsType<RedirectToActionResult>(_controlador.Index());

            Assert.Equal("Solicitudes", resultado.ControllerName);
        }

        [Fact]
        [Trait("HU", "HU-01")]
        [Trait("CA", "CA-01")]
        public async Task Index_EstudianteValido_IniciaSesionYVaAlCatalogo()
        {
            Responder(Exitosa(NombresRol.Estudiante));

            var resultado = Assert.IsType<RedirectToActionResult>(await _controlador.Index(Formulario(), default));

            Assert.Equal("Catalogo", resultado.ControllerName);
            await SesionIniciada(1);
        }

        [Fact]
        [Trait("HU", "HU-01")]
        [Trait("CA", "CA-01")]
        public async Task Index_EncargadoValido_VaASolicitudes()
        {
            Responder(Exitosa(NombresRol.Encargado));

            var resultado = Assert.IsType<RedirectToActionResult>(await _controlador.Index(Formulario(), default));

            Assert.Equal("Solicitudes", resultado.ControllerName);
        }

        [Fact]
        [Trait("HU", "ADM")]
        [Trait("CA", "ADM-CA1")]
        public async Task Index_AdministradorValido_VaASolicitudes()
        {
            Responder(Exitosa(NombresRol.Administrador));

            var resultado = Assert.IsType<RedirectToActionResult>(await _controlador.Index(Formulario(), default));

            Assert.Equal("Solicitudes", resultado.ControllerName);
        }

        [Fact]
        [Trait("HU", "ADM")]
        [Trait("CA", "ADM-CA1")]
        public void Index_AdministradorYaAutenticado_RedirigeASolicitudes()
        {
            ContextoControlador.Configurar(_controlador, 1, NombresRol.Administrador);

            var resultado = Assert.IsType<RedirectToActionResult>(_controlador.Index());

            Assert.Equal("Solicitudes", resultado.ControllerName);
        }

        [Fact]
        [Trait("HU", "HU-01")]
        [Trait("CA", "CA-01")]
        public async Task Index_CredencialesValidas_EmiteClaimsDeIdentidadYRol()
        {
            Responder(Exitosa(NombresRol.Estudiante));
            ClaimsPrincipal? principal = null;
            await _http.SignInAsync(
                Arg.Any<HttpContext>(),
                CookieAuthenticationDefaults.AuthenticationScheme,
                Arg.Do<ClaimsPrincipal>(p => principal = p),
                Arg.Any<AuthenticationProperties>());

            await _controlador.Index(Formulario(), default);

            Assert.NotNull(principal);
            Assert.Equal("7", principal.FindFirstValue(ClaimTypes.NameIdentifier));
            Assert.Equal(NombresRol.Estudiante, principal.FindFirstValue(ClaimTypes.Role));
        }

        [Fact]
        [Trait("HU", "HU-01")]
        [Trait("CA", "CA-02")]
        public async Task Index_CredencialesInvalidas_MuestraMensajeExactoYNoAutentica()
        {
            Responder(ResultadoAutenticacion.Invalida());

            Assert.IsType<ViewResult>(await _controlador.Index(Formulario(), default));

            var error = Assert.Single(_controlador.ModelState[string.Empty]!.Errors);
            Assert.Equal(Mensajes.CredencialesInvalidas, error.ErrorMessage);
            await SesionIniciada(0);
        }

        [Fact]
        [Trait("HU", "HU-01")]
        [Trait("CA", "CA-02")]
        public async Task Index_ModeloInvalido_NoConsultaElServicio()
        {
            _controlador.ModelState.AddModelError(nameof(LoginViewModel.Codigo), "Obligatorio");

            Assert.IsType<ViewResult>(await _controlador.Index(new LoginViewModel(), default));

            await _servicio.DidNotReceiveWithAnyArgs().AutenticarAsync(default, default, default);
        }

        [Fact]
        [Trait("HU", "HU-01")]
        [Trait("CA", "CA-03")]
        public async Task Index_CuentaBloqueada_MuestraAlertaYNoAutentica()
        {
            var ahora = new DateTime(2026, 11, 9, 10, 0, 0);
            Responder(ResultadoAutenticacion.Bloqueada(ahora.AddMinutes(15), ahora));

            var vista = Assert.IsType<ViewResult>(await _controlador.Index(Formulario(), default));

            var modelo = Assert.IsType<LoginViewModel>(vista.Model);
            Assert.Equal(Mensajes.CuentaBloqueada, modelo.AlertaSeguridad);
            Assert.Equal(900, modelo.SegundosRestantes);
            await SesionIniciada(0);
        }

        [Fact]
        [Trait("HU", "HU-01")]
        [Trait("CA", "CA-01")]
        public async Task Salir_SesionActiva_CierraSesionYVuelveAlLogin()
        {
            var resultado = Assert.IsType<RedirectToActionResult>(await _controlador.Salir());

            Assert.Equal(nameof(LoginController.Index), resultado.ActionName);
            await _http.Received(1).SignOutAsync(
                Arg.Any<HttpContext>(), Arg.Any<string>(), Arg.Any<AuthenticationProperties>());
        }
    }
}
