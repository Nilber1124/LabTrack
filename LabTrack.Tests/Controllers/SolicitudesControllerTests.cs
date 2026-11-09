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
    public class SolicitudesControllerTests
    {
        private readonly IReservaService _servicio = Substitute.For<IReservaService>();
        private readonly SolicitudesController _controlador;

        public SolicitudesControllerTests()
        {
            _controlador = new SolicitudesController(_servicio);
            ContextoControlador.Configurar(_controlador, 2, NombresRol.Encargado);
        }

        private static RechazarSolicitudViewModel Rechazo() => new() { IdReserva = 5, Motivo = "Equipo en mantenimiento" };

        [Fact]
        [Trait("HU", "HU-04")]
        [Trait("CA", "CA-11")]
        public async Task Index_Encargado_MuestraLasPendientes()
        {
            var modelo = new SolicitudesViewModel();
            _servicio.ListarPendientesAsync(1, Arg.Any<CancellationToken>()).Returns(modelo);

            var vista = Assert.IsType<ViewResult>(await _controlador.Index());

            Assert.Same(modelo, vista.Model);
        }

        [Fact]
        [Trait("HU", "HU-04")]
        [Trait("CA", "CA-12")]
        public async Task Aprobar_Correcta_RedirigeConExito()
        {
            _servicio.AprobarAsync(5, Arg.Any<CancellationToken>()).Returns(ResultadoOperacion.Ok("Reserva aprobada"));

            var resultado = Assert.IsType<RedirectToActionResult>(await _controlador.Aprobar(5));

            Assert.Equal(nameof(SolicitudesController.Index), resultado.ActionName);
            Assert.Equal("Reserva aprobada", _controlador.TempData["Success"]);
        }

        [Fact]
        [Trait("HU", "HU-04")]
        [Trait("CA", "CA-12")]
        public async Task Aprobar_ConCruce_RedirigeConError()
        {
            _servicio.AprobarAsync(5, Arg.Any<CancellationToken>())
                .Returns(ResultadoOperacion.Fallo(TipoResultado.Conflicto, Mensajes.ChoqueConAprobada));

            Assert.IsType<RedirectToActionResult>(await _controlador.Aprobar(5));

            Assert.Equal(Mensajes.ChoqueConAprobada, _controlador.TempData["Error"]);
        }

        [Fact]
        [Trait("HU", "HU-04")]
        [Trait("CA", "CA-12")]
        public async Task Aprobar_Inexistente_DevuelveNotFound()
        {
            _servicio.AprobarAsync(5, Arg.Any<CancellationToken>())
                .Returns(ResultadoOperacion.Fallo(TipoResultado.NoEncontrado, Mensajes.ReservaNoEncontrada));

            Assert.IsType<NotFoundResult>(await _controlador.Aprobar(5));
        }

        [Fact]
        [Trait("HU", "HU-04")]
        [Trait("CA", "CA-13")]
        public async Task Rechazar_Pendiente_MuestraFormulario()
        {
            _servicio.ObtenerPendienteAsync(5, Arg.Any<CancellationToken>()).Returns(new SolicitudPendienteViewModel { IdReserva = 5 });

            var vista = Assert.IsType<ViewResult>(await _controlador.Rechazar(5));

            Assert.Equal(5, Assert.IsType<RechazarSolicitudViewModel>(vista.Model).IdReserva);
        }

        [Fact]
        [Trait("HU", "HU-04")]
        [Trait("CA", "CA-13")]
        public async Task Rechazar_NoPendiente_DevuelveNotFound()
        {
            _servicio.ObtenerPendienteAsync(5, Arg.Any<CancellationToken>()).Returns((SolicitudPendienteViewModel?)null);

            Assert.IsType<NotFoundResult>(await _controlador.Rechazar(5));
        }

        [Fact]
        [Trait("HU", "HU-04")]
        [Trait("CA", "CA-13")]
        public async Task RechazarPost_ConMotivo_LlamaAlServicioYRedirige()
        {
            _servicio.RechazarAsync(5, "Equipo en mantenimiento", Arg.Any<CancellationToken>())
                .Returns(ResultadoOperacion.Ok("Solicitud rechazada"));

            Assert.IsType<RedirectToActionResult>(await _controlador.Rechazar(Rechazo()));

            Assert.Equal("Solicitud rechazada", _controlador.TempData["Success"]);
        }

        [Fact]
        [Trait("HU", "HU-04")]
        [Trait("CA", "CA-13")]
        public async Task RechazarPost_SinMotivo_VuelveAlFormularioSinLlamarAlServicio()
        {
            _servicio.ObtenerPendienteAsync(5, Arg.Any<CancellationToken>()).Returns(new SolicitudPendienteViewModel { IdReserva = 5 });
            _controlador.ModelState.AddModelError(nameof(RechazarSolicitudViewModel.Motivo), "Obligatorio");

            Assert.IsType<ViewResult>(await _controlador.Rechazar(new RechazarSolicitudViewModel { IdReserva = 5 }));

            await _servicio.DidNotReceiveWithAnyArgs().RechazarAsync(default, default, default);
        }

        [Fact]
        [Trait("HU", "HU-04")]
        [Trait("CA", "CA-13")]
        public async Task RechazarPost_MotivoRechazadoPorElServicio_MarcaElCampo()
        {
            _servicio.RechazarAsync(5, Arg.Any<string?>(), Arg.Any<CancellationToken>())
                .Returns(ResultadoOperacion.Fallo(TipoResultado.Validacion, Mensajes.MotivoObligatorio));
            _servicio.ObtenerPendienteAsync(5, Arg.Any<CancellationToken>()).Returns(new SolicitudPendienteViewModel { IdReserva = 5 });

            Assert.IsType<ViewResult>(await _controlador.Rechazar(Rechazo()));

            var errores = _controlador.ModelState[nameof(RechazarSolicitudViewModel.Motivo)]!.Errors;
            Assert.Equal(Mensajes.MotivoObligatorio, Assert.Single(errores).ErrorMessage);
        }

        [Fact]
        [Trait("HU", "HU-04")]
        [Trait("CA", "CA-13")]
        public async Task RechazarPost_YaResuelta_RedirigeConError()
        {
            _servicio.RechazarAsync(5, Arg.Any<string?>(), Arg.Any<CancellationToken>())
                .Returns(ResultadoOperacion.Fallo(TipoResultado.EstadoInvalido, Mensajes.SolicitudYaResuelta));

            Assert.IsType<RedirectToActionResult>(await _controlador.Rechazar(Rechazo()));

            Assert.Equal(Mensajes.SolicitudYaResuelta, _controlador.TempData["Error"]);
        }
    }
}
