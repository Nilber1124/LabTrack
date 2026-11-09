using LabTrack.Controllers;
using LabTrack.Reglas;
using LabTrack.Services;
using LabTrack.Services.Interfaces;
using LabTrack.Tests.Soporte;
using LabTrack.ViewModels;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace LabTrack.Tests.Controllers
{
    public class UsuariosControllerTests
    {
        private readonly IUsuarioService _servicio = Substitute.For<IUsuarioService>();
        private readonly UsuariosController _controlador;

        public UsuariosControllerTests()
        {
            _controlador = new UsuariosController(_servicio);
            ContextoControlador.Configurar(_controlador, 3, NombresRol.Administrador);
        }

        private static FormularioUsuarioViewModel Formulario() => new()
        {
            Codigo = "20240001",
            Nombre = "Ana Torres",
            Rol = NombresRol.Encargado,
            Contrasena = "clave-segura"
        };

        private void ResponderGuardar(ResultadoOperacion resultado)
            => _servicio.GuardarAsync(Arg.Any<DatosUsuario>(), Arg.Any<CancellationToken>()).Returns(resultado);

        [Fact]
        [Trait("HU", "ADM")]
        [Trait("CA", "ADM-USU")]
        public async Task Index_ConFiltros_ConsultaElServicio()
        {
            var modelo = new GestionUsuariosViewModel();
            _servicio.ListarAsync("Ana", NombresRol.Encargado, 2, Arg.Any<CancellationToken>()).Returns(modelo);

            var vista = Assert.IsType<ViewResult>(await _controlador.Index("Ana", NombresRol.Encargado, 2));

            Assert.Same(modelo, vista.Model);
        }

        [Fact]
        [Trait("HU", "ADM")]
        [Trait("CA", "ADM-USU")]
        public async Task Editar_Inexistente_DevuelveNotFound()
        {
            _servicio.ObtenerFormularioEdicionAsync(5, Arg.Any<CancellationToken>()).Returns((FormularioUsuarioViewModel?)null);

            Assert.IsType<NotFoundResult>(await _controlador.Editar(5));
        }

        [Fact]
        [Trait("HU", "ADM")]
        [Trait("CA", "ADM-USU")]
        public async Task Guardar_Correcto_RedirigeConExito()
        {
            ResponderGuardar(ResultadoOperacion.Ok("Usuario creado"));

            Assert.IsType<RedirectToActionResult>(await _controlador.Guardar(Formulario()));

            Assert.Equal("Usuario creado", _controlador.TempData["Success"]);
            await _servicio.Received(1).GuardarAsync(
                Arg.Is<DatosUsuario>(d => d.Codigo == "20240001" && d.Rol == NombresRol.Encargado), Arg.Any<CancellationToken>());
        }

        [Fact]
        [Trait("HU", "ADM")]
        [Trait("CA", "ADM-USU")]
        public async Task Guardar_ModeloInvalido_NoLlamaAlServicio()
        {
            _controlador.ModelState.AddModelError(nameof(FormularioUsuarioViewModel.Nombre), "Obligatorio");

            var vista = Assert.IsType<ViewResult>(await _controlador.Guardar(Formulario()));

            Assert.Equal("Formulario", vista.ViewName);
            await _servicio.DidNotReceiveWithAnyArgs().GuardarAsync(default!, default);
        }

        [Fact]
        [Trait("HU", "ADM")]
        [Trait("CA", "ADM-USU")]
        public async Task Guardar_CodigoRepetido_MarcaElCampo()
        {
            ResponderGuardar(ResultadoOperacion.Fallo(TipoResultado.Conflicto, Mensajes.CodigoUsuarioRepetido, "X pertenece a Y."));

            Assert.IsType<ViewResult>(await _controlador.Guardar(Formulario()));

            var error = Assert.Single(_controlador.ModelState[nameof(FormularioUsuarioViewModel.Codigo)]!.Errors);
            Assert.Equal($"{Mensajes.CodigoUsuarioRepetido}. X pertenece a Y.", error.ErrorMessage);
        }

        [Fact]
        [Trait("HU", "ADM")]
        [Trait("CA", "ADM-USU")]
        public async Task DarDeBaja_ConReservaActiva_RedirigeConMotivo()
        {
            _servicio.DarDeBajaAsync(5, Arg.Any<CancellationToken>())
                .Returns(ResultadoOperacion.Fallo(TipoResultado.Conflicto, Mensajes.NoSePuedeDarDeBaja, "Detalle."));

            Assert.IsType<RedirectToActionResult>(await _controlador.DarDeBaja(5));

            Assert.Equal($"{Mensajes.NoSePuedeDarDeBaja}. Detalle.", _controlador.TempData["Error"]);
        }

        [Fact]
        [Trait("HU", "ADM")]
        [Trait("CA", "ADM-USU")]
        public async Task DarDeBaja_Inexistente_DevuelveNotFound()
        {
            _servicio.DarDeBajaAsync(5, Arg.Any<CancellationToken>())
                .Returns(ResultadoOperacion.Fallo(TipoResultado.NoEncontrado, Mensajes.UsuarioNoEncontrado));

            Assert.IsType<NotFoundResult>(await _controlador.DarDeBaja(5));
        }

        [Fact]
        [Trait("HU", "ADM")]
        [Trait("CA", "ADM-USU")]
        public async Task Reactivar_Correcto_RedirigeConExito()
        {
            _servicio.ReactivarAsync(5, Arg.Any<CancellationToken>()).Returns(ResultadoOperacion.Ok("Reactivado"));

            Assert.IsType<RedirectToActionResult>(await _controlador.Reactivar(5));

            Assert.Equal("Reactivado", _controlador.TempData["Success"]);
        }
    }
}
