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
    public class PenalizadosControllerTests
    {
        private readonly IDevolucionService _servicio = Substitute.For<IDevolucionService>();
        private readonly PenalizadosController _controlador;

        public PenalizadosControllerTests()
        {
            _controlador = new PenalizadosController(_servicio);
            ContextoControlador.Configurar(_controlador, 2, NombresRol.Encargado);
        }

        [Fact]
        [Trait("HU", "HU-05")]
        [Trait("CA", "CA-16")]
        public async Task Index_ConBusqueda_MuestraElListado()
        {
            var modelo = new PenalizadosViewModel();
            _servicio.ListarPenalizadosAsync("Diego", 1, Arg.Any<CancellationToken>()).Returns(modelo);

            var vista = Assert.IsType<ViewResult>(await _controlador.Index("Diego"));

            Assert.Same(modelo, vista.Model);
        }

        [Fact]
        [Trait("HU", "HU-05")]
        [Trait("CA", "CA-16")]
        public async Task Levantar_Correcto_RedirigeConExito()
        {
            _servicio.LevantarPenalizacionAsync(7, Arg.Any<CancellationToken>()).Returns(ResultadoOperacion.Ok("Penalización levantada"));

            Assert.IsType<RedirectToActionResult>(await _controlador.Levantar(7));

            Assert.Equal("Penalización levantada", _controlador.TempData["Success"]);
        }

        [Fact]
        [Trait("HU", "HU-05")]
        [Trait("CA", "CA-16")]
        public async Task Levantar_SinPenalizacionActiva_RedirigeConError()
        {
            _servicio.LevantarPenalizacionAsync(7, Arg.Any<CancellationToken>())
                .Returns(ResultadoOperacion.Fallo(TipoResultado.EstadoInvalido, Mensajes.PenalizacionNoActiva));

            Assert.IsType<RedirectToActionResult>(await _controlador.Levantar(7));

            Assert.Equal(Mensajes.PenalizacionNoActiva, _controlador.TempData["Error"]);
        }

        [Fact]
        [Trait("HU", "HU-05")]
        [Trait("CA", "CA-16")]
        public async Task Levantar_Inexistente_DevuelveNotFound()
        {
            _servicio.LevantarPenalizacionAsync(7, Arg.Any<CancellationToken>())
                .Returns(ResultadoOperacion.Fallo(TipoResultado.NoEncontrado, Mensajes.EstudianteNoEncontrado));

            Assert.IsType<NotFoundResult>(await _controlador.Levantar(7));
        }
    }
}
