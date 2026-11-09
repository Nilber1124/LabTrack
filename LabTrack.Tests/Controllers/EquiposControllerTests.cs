using LabTrack.Controllers;
using LabTrack.Reglas;
using LabTrack.Services;
using LabTrack.Services.Interfaces;
using LabTrack.Tests.Soporte;
using LabTrack.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace LabTrack.Tests.Controllers
{
    public class EquiposControllerTests
    {
        private readonly IEquipoService _servicio = Substitute.For<IEquipoService>();
        private readonly EquiposController _controlador;

        public EquiposControllerTests()
        {
            _controlador = new EquiposController(_servicio);
            ContextoControlador.Configurar(_controlador, 3, NombresRol.Administrador);
            _servicio.ObtenerFormularioNuevoAsync(Arg.Any<CancellationToken>()).Returns(new FormularioEquipoViewModel());
        }

        private static FormularioEquipoViewModel Formulario() => new()
        {
            Nombre = "Analizador",
            CodigoInventario = "AE-0033",
            IdCategoria = 2,
            Laboratorio = "Lab. B-110"
        };

        private void ResponderGuardar(ResultadoOperacion resultado)
            => _servicio.GuardarAsync(Arg.Any<DatosEquipo>(), Arg.Any<ImagenSubida?>(), Arg.Any<CancellationToken>()).Returns(resultado);

        [Fact]
        [Trait("HU", "ADM")]
        [Trait("CA", "ADM-CA2")]
        public async Task Index_ConFiltros_ConsultaElServicio()
        {
            var modelo = new GestionEquiposViewModel();
            _servicio.ListarAsync("Laptop", "Redes", 2, Arg.Any<CancellationToken>()).Returns(modelo);

            var vista = Assert.IsType<ViewResult>(await _controlador.Index("Laptop", "Redes", 2));

            Assert.Same(modelo, vista.Model);
        }

        [Fact]
        [Trait("HU", "ADM")]
        [Trait("CA", "ADM-CA3")]
        public async Task Editar_Inexistente_DevuelveNotFound()
        {
            _servicio.ObtenerFormularioEdicionAsync(5, Arg.Any<CancellationToken>()).Returns((FormularioEquipoViewModel?)null);

            Assert.IsType<NotFoundResult>(await _controlador.Editar(5));
        }

        [Fact]
        [Trait("HU", "ADM")]
        [Trait("CA", "ADM-CA3")]
        public async Task Guardar_Correcto_RedirigeConExitoSinImagen()
        {
            ResponderGuardar(ResultadoOperacion.Ok("Equipo guardado"));

            Assert.IsType<RedirectToActionResult>(await _controlador.Guardar(Formulario()));

            Assert.Equal("Equipo guardado", _controlador.TempData["Success"]);
            await _servicio.Received(1).GuardarAsync(Arg.Is<DatosEquipo>(d => d.CodigoInventario == "AE-0033"), null, Arg.Any<CancellationToken>());
        }

        [Fact]
        [Trait("HU", "ADM")]
        [Trait("CA", "ADM-CA3")]
        public async Task Guardar_ConFoto_EntregaLaImagenAlServicio()
        {
            ResponderGuardar(ResultadoOperacion.Ok("Equipo guardado"));
            var modelo = Formulario();
            using var flujo = new MemoryStream(new byte[] { 0xFF, 0xD8, 0xFF });
            modelo.Foto = new FormFile(flujo, 0, flujo.Length, "Foto", "foto.jpg");

            await _controlador.Guardar(modelo);

            await _servicio.Received(1).GuardarAsync(Arg.Any<DatosEquipo>(), Arg.Is<ImagenSubida?>(i => i!.Bytes == 3), Arg.Any<CancellationToken>());
        }

        [Fact]
        [Trait("HU", "ADM")]
        [Trait("CA", "ADM-CA3")]
        public async Task Guardar_ModeloInvalido_NoLlamaAlServicio()
        {
            _controlador.ModelState.AddModelError(nameof(FormularioEquipoViewModel.Nombre), "Obligatorio");

            var vista = Assert.IsType<ViewResult>(await _controlador.Guardar(Formulario()));

            Assert.Equal("Formulario", vista.ViewName);
            await _servicio.DidNotReceiveWithAnyArgs().GuardarAsync(default!, default, default);
        }

        [Fact]
        [Trait("HU", "ADM")]
        [Trait("CA", "ADM-CA3")]
        public async Task Guardar_CodigoRepetido_MarcaElCampoConElDetalle()
        {
            ResponderGuardar(ResultadoOperacion.Fallo(TipoResultado.Conflicto, Mensajes.CodigoRepetido, "AE-0033 pertenece a Otro."));

            Assert.IsType<ViewResult>(await _controlador.Guardar(Formulario()));

            var error = Assert.Single(_controlador.ModelState[nameof(FormularioEquipoViewModel.CodigoInventario)]!.Errors);
            Assert.Equal($"{Mensajes.CodigoRepetido}. AE-0033 pertenece a Otro.", error.ErrorMessage);
        }

        [Fact]
        [Trait("HU", "ADM")]
        [Trait("CA", "ADM-CA3")]
        public async Task Guardar_ImagenNoValida_MuestraErrorGeneral()
        {
            ResponderGuardar(ResultadoOperacion.Fallo(TipoResultado.Validacion, Mensajes.ImagenNoSubida, Mensajes.ImagenNoValida));

            Assert.IsType<ViewResult>(await _controlador.Guardar(Formulario()));

            var error = Assert.Single(_controlador.ModelState[string.Empty]!.Errors);
            Assert.Equal($"{Mensajes.ImagenNoSubida}. {Mensajes.ImagenNoValida}", error.ErrorMessage);
        }

        [Fact]
        [Trait("HU", "ADM")]
        [Trait("CA", "ADM-CA4")]
        public async Task DarDeBaja_EquipoPrestado_RedirigeConMotivo()
        {
            _servicio.DarDeBajaAsync(5, Arg.Any<CancellationToken>())
                .Returns(ResultadoOperacion.Fallo(TipoResultado.Conflicto, Mensajes.NoSePuedeDarDeBaja, "Detalle."));

            Assert.IsType<RedirectToActionResult>(await _controlador.DarDeBaja(5));

            Assert.Equal($"{Mensajes.NoSePuedeDarDeBaja}. Detalle.", _controlador.TempData["Error"]);
        }

        [Fact]
        [Trait("HU", "ADM")]
        [Trait("CA", "ADM-CA4")]
        public async Task DarDeBaja_Inexistente_DevuelveNotFound()
        {
            _servicio.DarDeBajaAsync(5, Arg.Any<CancellationToken>())
                .Returns(ResultadoOperacion.Fallo(TipoResultado.NoEncontrado, Mensajes.EquipoNoEncontrado));

            Assert.IsType<NotFoundResult>(await _controlador.DarDeBaja(5));
        }

        [Fact]
        [Trait("HU", "ADM")]
        [Trait("CA", "ADM-CA4")]
        public async Task Reactivar_Correcto_RedirigeConExito()
        {
            _servicio.ReactivarAsync(5, Arg.Any<CancellationToken>()).Returns(ResultadoOperacion.Ok("Reactivado"));

            Assert.IsType<RedirectToActionResult>(await _controlador.Reactivar(5));

            Assert.Equal("Reactivado", _controlador.TempData["Success"]);
        }
    }
}
