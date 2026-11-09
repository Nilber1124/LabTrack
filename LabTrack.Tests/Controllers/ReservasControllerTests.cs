using LabTrack.Controllers;
using LabTrack.Models;
using LabTrack.Reglas;
using LabTrack.Services;
using LabTrack.Services.Interfaces;
using LabTrack.Tests.Soporte;
using LabTrack.ViewModels;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace LabTrack.Tests.Controllers
{
    public class ReservasControllerTests
    {
        private const int IdEstudiante = 7;

        private readonly IReservaService _servicio = Substitute.For<IReservaService>();
        private readonly ReservasController _controlador;

        public ReservasControllerTests()
        {
            _controlador = new ReservasController(_servicio);
            ContextoControlador.Configurar(_controlador, IdEstudiante, NombresRol.Estudiante);
        }

        private static CrearReservaViewModel Formulario() => new()
        {
            IdEquipo = 3,
            Fecha = new DateOnly(2026, 11, 10),
            HoraInicio = new TimeOnly(10, 0),
            HoraFin = new TimeOnly(12, 0)
        };

        private void ResponderCrear(ResultadoOperacion resultado)
            => _servicio.CrearAsync(IdEstudiante, 3, new DateOnly(2026, 11, 10), new TimeOnly(10, 0), new TimeOnly(12, 0), Arg.Any<CancellationToken>())
                .Returns(resultado);

        private void ResponderFormulario(bool disponible = true)
            => _servicio.ObtenerFormularioAsync(IdEstudiante, 3, Arg.Any<CancellationToken>())
                .Returns(new CrearReservaViewModel { IdEquipo = 3, NombreEquipo = "Laptop", EquipoDisponible = disponible });

        [Fact]
        [Trait("HU", "HU-03")]
        [Trait("CA", "CA-08")]
        public async Task Crear_EquipoDisponible_MuestraFormulario()
        {
            ResponderFormulario();

            var vista = Assert.IsType<ViewResult>(await _controlador.Crear(3));

            Assert.Equal("Laptop", Assert.IsType<CrearReservaViewModel>(vista.Model).NombreEquipo);
        }

        [Fact]
        [Trait("HU", "HU-03")]
        [Trait("CA", "CA-08")]
        public async Task Crear_EquipoPrestado_VuelveAlCatalogoConError()
        {
            ResponderFormulario(disponible: false);

            var resultado = Assert.IsType<RedirectToActionResult>(await _controlador.Crear(3));

            Assert.Equal("Catalogo", resultado.ControllerName);
            Assert.Equal(Mensajes.EquipoNoDisponible, _controlador.TempData["Error"]);
        }

        [Fact]
        [Trait("HU", "HU-05")]
        [Trait("CA", "CA-16")]
        public async Task Crear_EstudiantePenalizado_MuestraElAvisoAunqueElEquipoEstePrestado()
        {
            _servicio.ObtenerFormularioAsync(IdEstudiante, 3, Arg.Any<CancellationToken>())
                .Returns(new CrearReservaViewModel { IdEquipo = 3, EstudiantePenalizado = true, EquipoDisponible = false });

            var vista = Assert.IsType<ViewResult>(await _controlador.Crear(3));

            Assert.True(Assert.IsType<CrearReservaViewModel>(vista.Model).EstudiantePenalizado);
        }

        [Fact]
        [Trait("HU", "HU-03")]
        [Trait("CA", "CA-08")]
        public async Task Crear_SinIdentidad_DevuelveForbid()
        {
            ContextoControlador.Configurar(_controlador);

            Assert.IsType<ForbidResult>(await _controlador.Crear(3));
        }

        [Fact]
        [Trait("HU", "HU-03")]
        [Trait("CA", "CA-08")]
        public async Task Crear_EquipoInexistente_DevuelveNotFound()
        {
            _servicio.ObtenerFormularioAsync(IdEstudiante, 3, Arg.Any<CancellationToken>()).Returns((CrearReservaViewModel?)null);

            Assert.IsType<NotFoundResult>(await _controlador.Crear(3));
        }

        [Fact]
        [Trait("HU", "HU-03")]
        [Trait("CA", "CA-08")]
        public async Task CrearPost_SolicitudValida_RedirigeConExitoUsandoElUsuarioDeLosClaims()
        {
            ResponderCrear(ResultadoOperacion.Ok("Solicitud enviada"));

            var resultado = Assert.IsType<RedirectToActionResult>(await _controlador.Crear(Formulario()));

            Assert.Equal(nameof(ReservasController.Index), resultado.ActionName);
            Assert.Equal("Solicitud enviada", _controlador.TempData["Success"]);
        }

        [Fact]
        [Trait("HU", "HU-03")]
        [Trait("CA", "CA-08")]
        public async Task CrearPost_ModeloInvalido_NoLlamaAlServicio()
        {
            ResponderFormulario();
            _controlador.ModelState.AddModelError(nameof(CrearReservaViewModel.Fecha), "Obligatorio");

            Assert.IsType<ViewResult>(await _controlador.Crear(Formulario()));

            await _servicio.DidNotReceiveWithAnyArgs().CrearAsync(default, default, default, default, default, default);
        }

        [Fact]
        [Trait("HU", "HU-03")]
        [Trait("CA", "CA-09")]
        public async Task CrearPost_ChoqueConAprobada_MuestraElErrorEnElFormulario()
        {
            ResponderCrear(ResultadoOperacion.Fallo(TipoResultado.Conflicto, Mensajes.ChoqueConAprobada));
            ResponderFormulario();

            var vista = Assert.IsType<ViewResult>(await _controlador.Crear(Formulario()));

            Assert.Equal("Laptop", Assert.IsType<CrearReservaViewModel>(vista.Model).NombreEquipo);
            Assert.Equal(Mensajes.ChoqueConAprobada, Assert.Single(_controlador.ModelState[string.Empty]!.Errors).ErrorMessage);
        }

        [Fact]
        [Trait("HU", "HU-03")]
        [Trait("CA", "CA-10")]
        public async Task CrearPost_OtraReservaActiva_MuestraElError()
        {
            ResponderCrear(ResultadoOperacion.Fallo(TipoResultado.Conflicto, Mensajes.UnaReservaALaVez));
            ResponderFormulario();

            Assert.IsType<ViewResult>(await _controlador.Crear(Formulario()));

            Assert.Equal(Mensajes.UnaReservaALaVez, Assert.Single(_controlador.ModelState[string.Empty]!.Errors).ErrorMessage);
        }

        [Fact]
        [Trait("HU", "HU-03")]
        [Trait("CA", "CA-08")]
        public async Task CrearPost_EquipoInexistente_DevuelveNotFound()
        {
            ResponderCrear(ResultadoOperacion.Fallo(TipoResultado.NoEncontrado, Mensajes.EquipoNoEncontrado));

            Assert.IsType<NotFoundResult>(await _controlador.Crear(Formulario()));
        }

        [Fact]
        [Trait("HU", "HU-03")]
        [Trait("CA", "CA-08")]
        public async Task CrearPost_SinIdentidad_DevuelveForbid()
        {
            ContextoControlador.Configurar(_controlador);

            Assert.IsType<ForbidResult>(await _controlador.Crear(Formulario()));
        }

        [Fact]
        [Trait("HU", "HU-03")]
        [Trait("CA", "CA-08")]
        public async Task Index_Autenticado_ConsultaSoloLasReservasDelUsuario()
        {
            _servicio.ListarPropiasAsync(IdEstudiante, EstadoReserva.Pendiente, 1, Arg.Any<CancellationToken>())
                .Returns(new MisReservasViewModel());

            Assert.IsType<ViewResult>(await _controlador.Index(EstadoReserva.Pendiente));

            await _servicio.Received(1).ListarPropiasAsync(IdEstudiante, EstadoReserva.Pendiente, 1, Arg.Any<CancellationToken>());
        }

        [Fact]
        [Trait("HU", "HU-03")]
        [Trait("CA", "CA-08")]
        public async Task Index_SinIdentidad_DevuelveForbid()
        {
            ContextoControlador.Configurar(_controlador);

            Assert.IsType<ForbidResult>(await _controlador.Index(null));
        }
    }
}
