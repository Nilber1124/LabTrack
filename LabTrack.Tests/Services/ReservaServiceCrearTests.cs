using LabTrack.Models;
using LabTrack.Reglas;
using LabTrack.Services;
using LabTrack.Tests.Soporte;
using Microsoft.EntityFrameworkCore;

namespace LabTrack.Tests.Services
{
    [Collection(ColeccionBaseDatos.Nombre)]
    [Trait("BD", "SQLServer")]
    public class ReservaServiceCrearTests : PruebaConBaseDatos
    {
        public ReservaServiceCrearTests(BaseDatosFixture baseDatos) : base(baseDatos)
        {
        }

        private async Task<ResultadoOperacion> CrearAsync(int idUsuario, int idEquipo, string inicio = "10:00", string fin = "12:00", DateOnly? fecha = null)
        {
            await using var contexto = BaseDatos.CrearContexto();
            return await new ReservaService(contexto, Reloj)
                .CrearAsync(idUsuario, idEquipo, fecha ?? Manana, TimeOnly.Parse(inicio), TimeOnly.Parse(fin));
        }

        // Una reserva aprobada de otro estudiante, mañana de 14:00 a 16:00.
        private async Task<Equipo> EquipoConAprobadaAsync(EstadoReserva estado = EstadoReserva.Aprobada)
        {
            var otro = await CrearUsuarioAsync("OTRO");
            var equipo = await CrearEquipoAsync("LT-1");
            await CrearReservaAsync(otro.IdUsuario, equipo.IdEquipo, HoraManana(14), HoraManana(16), estado);
            return equipo;
        }

        [Fact]
        [Trait("HU", "HU-03")]
        [Trait("CA", "CA-08")]
        public async Task CrearAsync_DatosValidos_CreaReservaPendienteConFechaCompuesta()
        {
            var estudiante = await CrearUsuarioAsync("E1");
            var equipo = await CrearEquipoAsync("LT-1");

            var resultado = await CrearAsync(estudiante.IdUsuario, equipo.IdEquipo);

            await using var contexto = BaseDatos.CrearContexto();
            var reserva = await contexto.Reservas.AsNoTracking().SingleAsync();
            Assert.Equal(Ahora, reserva.FechaSolicitud);
            Assert.True(resultado.Exitoso);
            Assert.Equal(EstadoReserva.Pendiente, reserva.Estado);
            Assert.Equal((HoraManana(10), HoraManana(12)), (reserva.Inicio, reserva.Fin));
        }

        [Fact]
        [Trait("HU", "HU-03")]
        [Trait("CA", "CA-08")]
        public async Task CrearAsync_EquipoPrestadoAhora_SeRechaza()
        {
            var estudiante = await CrearUsuarioAsync("E1");
            var otro = await CrearUsuarioAsync("OTRO");
            var equipo = await CrearEquipoAsync("LT-1");
            await CrearReservaAsync(otro.IdUsuario, equipo.IdEquipo, Ahora.AddHours(-1), Ahora.AddHours(1), EstadoReserva.Aprobada);

            var resultado = await CrearAsync(estudiante.IdUsuario, equipo.IdEquipo);

            Assert.Equal(Mensajes.EquipoNoDisponible, resultado.Mensaje);
            Assert.Equal(1, await ContarReservasAsync());
        }

        [Theory]
        [Trait("HU", "HU-03")]
        [Trait("CA", "CA-08")]
        [InlineData("12:00", "12:00")]
        [InlineData("13:00", "12:00")]
        public async Task CrearAsync_FinNoPosteriorAInicio_SeRechaza(string inicio, string fin)
        {
            var estudiante = await CrearUsuarioAsync("E1");
            var equipo = await CrearEquipoAsync("LT-1");

            var resultado = await CrearAsync(estudiante.IdUsuario, equipo.IdEquipo, inicio, fin);

            Assert.Equal(TipoResultado.Validacion, resultado.Tipo);
            Assert.Equal(Mensajes.FinPosteriorAInicio, resultado.Mensaje);
            Assert.Equal(0, await ContarReservasAsync());
        }

        [Fact]
        [Trait("HU", "HU-03")]
        [Trait("CA", "CA-08")]
        public async Task CrearAsync_FechaPasada_SeRechaza()
        {
            var estudiante = await CrearUsuarioAsync("E1");
            var equipo = await CrearEquipoAsync("LT-1");

            var resultado = await CrearAsync(estudiante.IdUsuario, equipo.IdEquipo, fecha: new DateOnly(2026, 11, 8));

            Assert.Equal(Mensajes.InicioEnPasado, resultado.Mensaje);
        }

        [Fact]
        [Trait("HU", "HU-03")]
        [Trait("CA", "CA-08")]
        public async Task CrearAsync_EquipoInexistente_DevuelveNoEncontrado()
        {
            var estudiante = await CrearUsuarioAsync("E1");

            var resultado = await CrearAsync(estudiante.IdUsuario, 999_999);

            Assert.Equal(TipoResultado.NoEncontrado, resultado.Tipo);
        }

        [Fact]
        [Trait("HU", "HU-03")]
        [Trait("CA", "CA-08")]
        public async Task CrearAsync_UsuarioEncargado_SeRechaza()
        {
            var encargado = await CrearUsuarioAsync("ENC", idRol: IdRolEncargado);
            var equipo = await CrearEquipoAsync("LT-1");

            var resultado = await CrearAsync(encargado.IdUsuario, equipo.IdEquipo);

            Assert.Equal(Mensajes.SoloEstudiantes, resultado.Mensaje);
        }

        [Fact]
        [Trait("HU", "HU-05")]
        [Trait("CA", "CA-16")]
        public async Task CrearAsync_EstudiantePenalizado_SeRechazaSinCrearReserva()
        {
            var estudiante = await CrearUsuarioAsync("E1", estado: EstadoUsuario.Penalizado);
            var equipo = await CrearEquipoAsync("LT-1");

            var resultado = await CrearAsync(estudiante.IdUsuario, equipo.IdEquipo);

            Assert.Equal(Mensajes.EstudiantePenalizado, resultado.Mensaje);
            Assert.Equal(0, await ContarReservasAsync());
        }

        [Fact]
        [Trait("HU", "HU-05")]
        [Trait("CA", "CA-16")]
        public async Task ObtenerFormularioAsync_EstudiantePenalizado_LoIndica()
        {
            var estudiante = await CrearUsuarioAsync("E1", estado: EstadoUsuario.Penalizado);
            var equipo = await CrearEquipoAsync("LT-1");

            await using var contexto = BaseDatos.CrearContexto();
            var formulario = await new ReservaService(contexto, Reloj).ObtenerFormularioAsync(estudiante.IdUsuario, equipo.IdEquipo);

            Assert.True(formulario!.EstudiantePenalizado);
        }

        [Fact]
        [Trait("HU", "HU-03")]
        [Trait("CA", "CA-08")]
        public async Task ObtenerFormularioAsync_EquipoPrestado_IndicaNoDisponible()
        {
            var otro = await CrearUsuarioAsync("OTRO");
            var equipo = await CrearEquipoAsync("LT-1");
            await CrearReservaAsync(otro.IdUsuario, equipo.IdEquipo, Ahora.AddHours(-1), Ahora.AddHours(1), EstadoReserva.Aprobada);

            await using var contexto = BaseDatos.CrearContexto();
            var formulario = await new ReservaService(contexto, Reloj).ObtenerFormularioAsync(otro.IdUsuario, equipo.IdEquipo);

            Assert.False(formulario!.EquipoDisponible);
        }

        [Theory]
        [Trait("HU", "HU-03")]
        [Trait("CA", "CA-09")]
        [InlineData("13:00", "15:00")] // Parcial al inicio
        [InlineData("15:00", "17:00")] // Parcial al final
        [InlineData("13:00", "17:00")] // Total
        [InlineData("14:30", "15:30")] // Contenido
        public async Task CrearAsync_CruceConAprobada_SeBloquea(string inicio, string fin)
        {
            var equipo = await EquipoConAprobadaAsync();
            var estudiante = await CrearUsuarioAsync("E1");

            var resultado = await CrearAsync(estudiante.IdUsuario, equipo.IdEquipo, inicio, fin);

            Assert.Equal(TipoResultado.Conflicto, resultado.Tipo);
            Assert.Equal(Mensajes.ChoqueConAprobada, resultado.Mensaje);
            Assert.StartsWith("Ocupado de 14:00 a 16:00.", resultado.Aviso);
            Assert.Equal(1, await ContarReservasAsync());
        }

        [Fact]
        [Trait("HU", "HU-03")]
        [Trait("CA", "CA-09")]
        public async Task CrearAsync_Choque_SugierePrimeraFranjaLibre()
        {
            var equipo = await EquipoConAprobadaAsync();
            var estudiante = await CrearUsuarioAsync("E1");

            var resultado = await CrearAsync(estudiante.IdUsuario, equipo.IdEquipo, "14:00", "15:00");

            Assert.Equal("Ocupado de 14:00 a 16:00. Primera franja libre: 16:00.", resultado.Aviso);
            Assert.Equal((new TimeOnly(16, 0), new TimeOnly(17, 0)), (resultado.SugerenciaInicio!.Value, resultado.SugerenciaFin!.Value));
        }

        [Fact]
        [Trait("HU", "HU-03")]
        [Trait("CA", "CA-09")]
        public async Task CrearAsync_ChoqueSinFranjaDentroDelHorario_NoSugiere()
        {
            var otro = await CrearUsuarioAsync("OTRO");
            var equipo = await CrearEquipoAsync("LT-1");
            await CrearReservaAsync(otro.IdUsuario, equipo.IdEquipo, HoraManana(14), HoraManana(21), EstadoReserva.Aprobada);
            var estudiante = await CrearUsuarioAsync("E1");

            var resultado = await CrearAsync(estudiante.IdUsuario, equipo.IdEquipo, "15:00", "16:00");

            Assert.Equal("Ocupado de 14:00 a 21:00.", resultado.Aviso);
            Assert.Null(resultado.SugerenciaInicio);
        }

        [Fact]
        [Trait("HU", "HU-03")]
        [Trait("CA", "CA-09")]
        public async Task ObtenerFormularioAsync_ConFecha_DevuelveSoloAprobadasDeEseDia()
        {
            var equipo = await EquipoConAprobadaAsync();
            var estudiante = await CrearUsuarioAsync("E1");

            await using var contexto = BaseDatos.CrearContexto();
            var servicio = new ReservaService(contexto, Reloj);
            var conReserva = await servicio.ObtenerFormularioAsync(estudiante.IdUsuario, equipo.IdEquipo, Manana);
            var libre = await servicio.ObtenerFormularioAsync(estudiante.IdUsuario, equipo.IdEquipo, Manana.AddDays(1));

            Assert.Equal("14:00 – 16:00", Assert.Single(conReserva!.Ocupacion).Horario);
            Assert.Empty(libre!.Ocupacion);
        }

        [Theory]
        [Trait("HU", "HU-03")]
        [Trait("CA", "CA-09")]
        [InlineData("12:00", "14:00")]
        [InlineData("16:00", "18:00")]
        public async Task CrearAsync_HorarioContiguo_SePermite(string inicio, string fin)
        {
            var equipo = await EquipoConAprobadaAsync();
            var estudiante = await CrearUsuarioAsync("E1");

            Assert.True((await CrearAsync(estudiante.IdUsuario, equipo.IdEquipo, inicio, fin)).Exitoso);
        }

        [Theory]
        [Trait("HU", "HU-03")]
        [Trait("CA", "CA-09")]
        [InlineData(EstadoReserva.Pendiente)]
        [InlineData(EstadoReserva.Rechazada)]
        [InlineData(EstadoReserva.Devuelta)]
        public async Task CrearAsync_CruceConReservaNoAprobada_NoBloquea(EstadoReserva estado)
        {
            var equipo = await EquipoConAprobadaAsync(estado);
            var estudiante = await CrearUsuarioAsync("E1");

            Assert.True((await CrearAsync(estudiante.IdUsuario, equipo.IdEquipo, "14:00", "16:00")).Exitoso);
        }

        [Fact]
        [Trait("HU", "HU-03")]
        [Trait("CA", "CA-09")]
        public async Task CrearAsync_CruceEnOtroEquipo_NoBloquea()
        {
            await EquipoConAprobadaAsync();
            var otroEquipo = await CrearEquipoAsync("LT-2");
            var estudiante = await CrearUsuarioAsync("E1");

            Assert.True((await CrearAsync(estudiante.IdUsuario, otroEquipo.IdEquipo, "14:00", "16:00")).Exitoso);
        }

        [Theory]
        [Trait("HU", "HU-03")]
        [Trait("CA", "CA-10")]
        [InlineData(EstadoReserva.Pendiente)]
        [InlineData(EstadoReserva.Aprobada)]
        public async Task CrearAsync_SegundaReservaActiva_SeBloquea(EstadoReserva estado)
        {
            var estudiante = await CrearUsuarioAsync("E1");
            var primero = await CrearEquipoAsync("LT-1");
            var segundo = await CrearEquipoAsync("LT-2");
            await CrearReservaAsync(estudiante.IdUsuario, primero.IdEquipo, HoraManana(8), HoraManana(9), estado);

            var resultado = await CrearAsync(estudiante.IdUsuario, segundo.IdEquipo);

            Assert.Equal(Mensajes.UnaReservaALaVez, resultado.Mensaje);
            Assert.Equal(1, await ContarReservasAsync());
        }

        [Theory]
        [Trait("HU", "HU-03")]
        [Trait("CA", "CA-10")]
        [InlineData(EstadoReserva.Devuelta)]
        [InlineData(EstadoReserva.Rechazada)]
        public async Task CrearAsync_PrimeraYaTerminada_PermiteOtra(EstadoReserva estado)
        {
            var estudiante = await CrearUsuarioAsync("E1");
            var primero = await CrearEquipoAsync("LT-1");
            var segundo = await CrearEquipoAsync("LT-2");
            await CrearReservaAsync(estudiante.IdUsuario, primero.IdEquipo, HoraManana(8), HoraManana(9), estado);

            Assert.True((await CrearAsync(estudiante.IdUsuario, segundo.IdEquipo)).Exitoso);
        }

        [Fact]
        [Trait("HU", "HU-03")]
        [Trait("CA", "CA-10")]
        public async Task CrearAsync_OtroEstudianteConReserva_NoLoAfecta()
        {
            var otro = await CrearUsuarioAsync("OTRO");
            var estudiante = await CrearUsuarioAsync("E1");
            var primero = await CrearEquipoAsync("LT-1");
            var segundo = await CrearEquipoAsync("LT-2");
            await CrearReservaAsync(otro.IdUsuario, primero.IdEquipo, HoraManana(8), HoraManana(9));

            Assert.True((await CrearAsync(estudiante.IdUsuario, segundo.IdEquipo)).Exitoso);
        }

        [Fact]
        [Trait("HU", "HU-03")]
        [Trait("CA", "CA-10")]
        public async Task CrearAsync_DosSolicitudesSimultaneas_SoloUnaSeGuarda()
        {
            var estudiante = await CrearUsuarioAsync("E1");
            var primero = await CrearEquipoAsync("LT-1");
            var segundo = await CrearEquipoAsync("LT-2");

            var resultados = await Task.WhenAll(
                CrearAsync(estudiante.IdUsuario, primero.IdEquipo),
                CrearAsync(estudiante.IdUsuario, segundo.IdEquipo));

            Assert.Single(resultados, r => r.Exitoso);
            Assert.Single(resultados, r => r.Tipo == TipoResultado.Conflicto && r.Mensaje == Mensajes.UnaReservaALaVez);
            Assert.Equal(1, await ContarReservasAsync());
        }

        [Fact]
        [Trait("HU", "HU-03")]
        [Trait("CA", "CA-10")]
        public async Task IndiceFiltrado_InsercionDirectaDeSegundaActiva_LaRechazaSqlServer()
        {
            var estudiante = await CrearUsuarioAsync("E1");
            var primero = await CrearEquipoAsync("LT-1");
            var segundo = await CrearEquipoAsync("LT-2");
            await CrearReservaAsync(estudiante.IdUsuario, primero.IdEquipo, HoraManana(8), HoraManana(9));

            await Assert.ThrowsAsync<DbUpdateException>(
                () => CrearReservaAsync(estudiante.IdUsuario, segundo.IdEquipo, HoraManana(10), HoraManana(11)));
        }

        [Fact]
        [Trait("HU", "HU-03")]
        [Trait("CA", "CA-08")]
        public async Task RestriccionPeriodo_FinAnteriorAInicio_LaRechazaSqlServer()
        {
            var estudiante = await CrearUsuarioAsync("E1");
            var equipo = await CrearEquipoAsync("LT-1");

            await Assert.ThrowsAsync<DbUpdateException>(
                () => CrearReservaAsync(estudiante.IdUsuario, equipo.IdEquipo, HoraManana(12), HoraManana(10)));
        }

        [Fact]
        [Trait("HU", "HU-03")]
        [Trait("CA", "CA-08")]
        public async Task ListarPropiasAsync_VariosEstudiantes_DevuelveSoloLasPropias()
        {
            var estudiante = await CrearUsuarioAsync("E1");
            var otro = await CrearUsuarioAsync("OTRO");
            var equipo = await CrearEquipoAsync("LT-1");
            var propia = await CrearReservaAsync(estudiante.IdUsuario, equipo.IdEquipo, HoraManana(8), HoraManana(9));
            await CrearReservaAsync(otro.IdUsuario, equipo.IdEquipo, HoraManana(10), HoraManana(11));

            await using var contexto = BaseDatos.CrearContexto();
            var modelo = await new ReservaService(contexto, Reloj).ListarPropiasAsync(estudiante.IdUsuario, null, 1);

            Assert.Equal(propia.IdReserva, Assert.Single(modelo.Reservas).IdReserva);
        }

        [Fact]
        [Trait("HU", "HU-03")]
        [Trait("CA", "CA-08")]
        public async Task ListarPropiasAsync_FiltroPorEstado_AplicaYCuenta()
        {
            var estudiante = await CrearUsuarioAsync("E1");
            var equipo = await CrearEquipoAsync("LT-1");
            await CrearReservaAsync(estudiante.IdUsuario, equipo.IdEquipo, HoraManana(8), HoraManana(9), EstadoReserva.Rechazada);
            await CrearReservaAsync(estudiante.IdUsuario, equipo.IdEquipo, HoraManana(10), HoraManana(11));

            await using var contexto = BaseDatos.CrearContexto();
            var modelo = await new ReservaService(contexto, Reloj).ListarPropiasAsync(estudiante.IdUsuario, EstadoReserva.Rechazada, 1);

            Assert.Equal(EstadoReserva.Rechazada, Assert.Single(modelo.Reservas).Estado);
            Assert.Equal(2, modelo.TotalGeneral);
            Assert.Equal(1, modelo.TotalPendientes);
        }
    }
}
