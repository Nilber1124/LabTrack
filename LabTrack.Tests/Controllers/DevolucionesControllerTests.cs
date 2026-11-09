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
    public class DevolucionesControllerTests
    {
        private readonly IDevolucionService _servicio = Substitute.For<IDevolucionService>();
        private readonly DevolucionesController _controlador;

        public DevolucionesControllerTests()
        {
            _controlador = new DevolucionesController(_servicio);
            ContextoControlador.Configurar(_controlador, 2, NombresRol.Encargado);
        }

        [Fact]
        [Trait("HU", "HU-05")]
        [Trait("CA", "CA-14")]
        public async Task Index_ConBusqueda_ConsultaElServicio()
        {
            var modelo = new DevolucionesViewModel();
            _servicio.ListarActivasAsync("OS-0112", 1, Arg.Any<CancellationToken>()).Returns(modelo);

            var vista = Assert.IsType<ViewResult>(await _controlador.Index("OS-0112"));

            Assert.Same(modelo, vista.Model);
        }

        [Fact]
        [Trait("HU", "HU-05")]
        [Trait("CA", "CA-14")]
        public async Task Index_SinEstudianteElegido_MuestraLaFichaDelPrimero()
        {
            var ficha = new FichaEstudianteViewModel { IdUsuario = 7 };
            _servicio.ListarActivasAsync(null, 1, Arg.Any<CancellationToken>())
                .Returns(new DevolucionesViewModel { Reservas = [new ReservaActivaViewModel { IdUsuario = 7 }] });
            _servicio.ObtenerFichaAsync(7, Arg.Any<CancellationToken>()).Returns(ficha);

            var vista = Assert.IsType<ViewResult>(await _controlador.Index(null));

            Assert.Same(ficha, Assert.IsType<DevolucionesViewModel>(vista.Model).Ficha);
        }

        [Fact]
        [Trait("HU", "HU-05")]
        [Trait("CA", "CA-14")]
        public async Task Index_ConEstudianteElegido_MuestraSuFicha()
        {
            _servicio.ListarActivasAsync(null, 1, Arg.Any<CancellationToken>()).Returns(new DevolucionesViewModel());

            await _controlador.Index(null, idUsuario: 9);

            await _servicio.Received(1).ObtenerFichaAsync(9, Arg.Any<CancellationToken>());
        }

        [Fact]
        [Trait("HU", "HU-05")]
        [Trait("CA", "CA-14")]
        public async Task Index_SinReservasNiEstudiante_NoPideFicha()
        {
            _servicio.ListarActivasAsync(null, 1, Arg.Any<CancellationToken>()).Returns(new DevolucionesViewModel());

            await _controlador.Index(null);

            await _servicio.DidNotReceiveWithAnyArgs().ObtenerFichaAsync(default, default);
        }

        [Fact]
        [Trait("HU", "HU-05")]
        [Trait("CA", "CA-15")]
        public async Task Marcar_ATiempo_RedirigeConExitoSinAviso()
        {
            _servicio.MarcarDevolucionAsync(5, Arg.Any<CancellationToken>()).Returns(ResultadoOperacion.Ok("Devolución registrada"));

            Assert.IsType<RedirectToActionResult>(await _controlador.Marcar(5));

            Assert.Equal("Devolución registrada", _controlador.TempData["Success"]);
            Assert.False(_controlador.TempData.ContainsKey("Error"));
        }

        [Fact]
        [Trait("HU", "HU-05")]
        [Trait("CA", "CA-16")]
        public async Task Marcar_Tardia_MuestraExitoYAvisoDePenalizacion()
        {
            _servicio.MarcarDevolucionAsync(5, Arg.Any<CancellationToken>())
                .Returns(ResultadoOperacion.Ok("Devolución registrada", "Diego quedó Penalizado"));

            await _controlador.Marcar(5);

            Assert.Equal("Devolución registrada", _controlador.TempData["Success"]);
            Assert.Equal("Diego quedó Penalizado", _controlador.TempData["Error"]);
        }

        [Fact]
        [Trait("HU", "HU-05")]
        [Trait("CA", "CA-14")]
        public async Task Marcar_ReservaNoAprobada_RedirigeConError()
        {
            _servicio.MarcarDevolucionAsync(5, Arg.Any<CancellationToken>())
                .Returns(ResultadoOperacion.Fallo(TipoResultado.EstadoInvalido, Mensajes.SoloReservasAprobadas));

            Assert.IsType<RedirectToActionResult>(await _controlador.Marcar(5));

            Assert.Equal(Mensajes.SoloReservasAprobadas, _controlador.TempData["Error"]);
        }

        [Fact]
        [Trait("HU", "HU-05")]
        [Trait("CA", "CA-14")]
        public async Task Marcar_Inexistente_DevuelveNotFound()
        {
            _servicio.MarcarDevolucionAsync(5, Arg.Any<CancellationToken>())
                .Returns(ResultadoOperacion.Fallo(TipoResultado.NoEncontrado, Mensajes.ReservaNoEncontrada));

            Assert.IsType<NotFoundResult>(await _controlador.Marcar(5));
        }
    }
}
