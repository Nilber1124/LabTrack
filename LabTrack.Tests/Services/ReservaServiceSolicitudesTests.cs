using LabTrack.Models;
using LabTrack.Reglas;
using LabTrack.Services;
using LabTrack.Tests.Soporte;

namespace LabTrack.Tests.Services
{
    [Collection(ColeccionBaseDatos.Nombre)]
    [Trait("BD", "SQLServer")]
    public class ReservaServiceSolicitudesTests : PruebaConBaseDatos
    {
        public ReservaServiceSolicitudesTests(BaseDatosFixture baseDatos) : base(baseDatos)
        {
        }

        private async Task<ResultadoOperacion> AprobarAsync(int idReserva)
        {
            await using var contexto = BaseDatos.CrearContexto();
            return await new ReservaService(contexto, Reloj).AprobarAsync(idReserva);
        }

        private async Task<ResultadoOperacion> RechazarAsync(int idReserva, string? motivo)
        {
            await using var contexto = BaseDatos.CrearContexto();
            return await new ReservaService(contexto, Reloj).RechazarAsync(idReserva, motivo);
        }

        private async Task<Reserva> PendienteAsync(string codigoEstudiante, int idEquipo, int horaInicio = 14, int horaFin = 16)
        {
            var estudiante = await CrearUsuarioAsync(codigoEstudiante);
            return await CrearReservaAsync(estudiante.IdUsuario, idEquipo, HoraManana(horaInicio), HoraManana(horaFin));
        }

        [Fact]
        [Trait("HU", "HU-04")]
        [Trait("CA", "CA-11")]
        public async Task ListarPendientesAsync_VariosEstados_DevuelveSoloPendientesConSusDatos()
        {
            var equipo = await CrearEquipoAsync("LT-1", nombre: "Laptop Dell");
            var pendiente = await PendienteAsync("E1", equipo.IdEquipo);
            var aprobado = await CrearUsuarioAsync("E2");
            await CrearReservaAsync(aprobado.IdUsuario, equipo.IdEquipo, HoraManana(8), HoraManana(9), EstadoReserva.Aprobada);

            await using var contexto = BaseDatos.CrearContexto();
            var modelo = await new ReservaService(contexto, Reloj).ListarPendientesAsync(1);

            var solicitud = Assert.Single(modelo.Solicitudes);
            Assert.Equal(pendiente.IdReserva, solicitud.IdReserva);
            Assert.Equal("Nombre E1", solicitud.Estudiante);
            Assert.Equal("Laptop Dell", solicitud.Equipo);
            Assert.Equal((HoraManana(14), HoraManana(16)), (solicitud.Inicio, solicitud.Fin));
        }

        [Fact]
        [Trait("HU", "HU-04")]
        [Trait("CA", "CA-11")]
        public async Task ListarPendientesAsync_VariasPendientes_OrdenaPorInicio()
        {
            var equipo = await CrearEquipoAsync("LT-1");
            var tarde = await PendienteAsync("E1", equipo.IdEquipo, 16, 17);
            var temprano = await PendienteAsync("E2", equipo.IdEquipo, 8, 9);

            await using var contexto = BaseDatos.CrearContexto();
            var modelo = await new ReservaService(contexto, Reloj).ListarPendientesAsync(1);

            Assert.Equal(new[] { temprano.IdReserva, tarde.IdReserva }, modelo.Solicitudes.Select(s => s.IdReserva));
        }

        [Fact]
        [Trait("HU", "HU-04")]
        [Trait("CA", "CA-11")]
        public async Task ListarPendientesAsync_ConFechaDeSolicitud_IndicaLaAntiguedad()
        {
            var estudiante = await CrearUsuarioAsync("E1");
            var equipo = await CrearEquipoAsync("LT-1");
            await using (var contexto = BaseDatos.CrearContexto())
            {
                await new ReservaService(contexto, Reloj).CrearAsync(
                    estudiante.IdUsuario, equipo.IdEquipo, Manana, new TimeOnly(10, 0), new TimeOnly(12, 0));
            }

            Reloj.Advance(TimeSpan.FromMinutes(12));

            await using var lectura = BaseDatos.CrearContexto();
            var modelo = await new ReservaService(lectura, Reloj).ListarPendientesAsync(1);

            Assert.Equal("hace 12 min", Assert.Single(modelo.Solicitudes).Recibida);
            Assert.Equal("hace 12 min", modelo.AntiguedadMayor);
        }

        [Fact]
        [Trait("HU", "HU-04")]
        [Trait("CA", "CA-12")]
        public async Task AprobarAsync_Pendiente_PasaAAprobada()
        {
            var equipo = await CrearEquipoAsync("LT-1");
            var reserva = await PendienteAsync("E1", equipo.IdEquipo);

            var resultado = await AprobarAsync(reserva.IdReserva);

            Assert.True(resultado.Exitoso);
            Assert.Equal(EstadoReserva.Aprobada, (await LeerReservaAsync(reserva.IdReserva)).Estado);
        }

        [Fact]
        [Trait("HU", "HU-04")]
        [Trait("CA", "CA-12")]
        public async Task AprobarAsync_DuranteElBloque_EquipoFiguraPrestado()
        {
            var equipo = await CrearEquipoAsync("LT-1");
            var reserva = await PendienteAsync("E1", equipo.IdEquipo);
            await AprobarAsync(reserva.IdReserva);

            RelojPrueba.Fijar(Reloj, HoraManana(15));

            Assert.Equal(EstadoEquipo.Prestado, await EstadoDelEquipoAsync(equipo.IdEquipo));
        }

        [Fact]
        [Trait("HU", "HU-04")]
        [Trait("CA", "CA-12")]
        public async Task AprobarAsync_AntesDelBloque_EquipoSigueDisponible()
        {
            var equipo = await CrearEquipoAsync("LT-1");
            var reserva = await PendienteAsync("E1", equipo.IdEquipo);

            await AprobarAsync(reserva.IdReserva);

            Assert.Equal(EstadoEquipo.Disponible, await EstadoDelEquipoAsync(equipo.IdEquipo));
        }

        [Fact]
        [Trait("HU", "HU-04")]
        [Trait("CA", "CA-09")]
        public async Task AprobarAsync_AprobarACruzadaConB_BFallaSinCambiosParciales()
        {
            var equipo = await CrearEquipoAsync("LT-1");
            var a = await PendienteAsync("E1", equipo.IdEquipo, 14, 16);
            var b = await PendienteAsync("E2", equipo.IdEquipo, 15, 17);

            await AprobarAsync(a.IdReserva);
            var resultado = await AprobarAsync(b.IdReserva);

            Assert.Equal(TipoResultado.Conflicto, resultado.Tipo);
            Assert.Equal(Mensajes.ChoqueConAprobada, resultado.Mensaje);
            Assert.Equal(EstadoReserva.Pendiente, (await LeerReservaAsync(b.IdReserva)).Estado);
            Assert.Equal(EstadoReserva.Aprobada, (await LeerReservaAsync(a.IdReserva)).Estado);
        }

        [Fact]
        [Trait("HU", "HU-04")]
        [Trait("CA", "CA-09")]
        public async Task AprobarAsync_DosAprobacionesSimultaneas_SoloUnaGana()
        {
            var equipo = await CrearEquipoAsync("LT-1");
            var a = await PendienteAsync("E1", equipo.IdEquipo, 14, 16);
            var b = await PendienteAsync("E2", equipo.IdEquipo, 15, 17);

            var resultados = await Task.WhenAll(AprobarAsync(a.IdReserva), AprobarAsync(b.IdReserva));

            var estados = new[] { (await LeerReservaAsync(a.IdReserva)).Estado, (await LeerReservaAsync(b.IdReserva)).Estado };
            Assert.Single(resultados, r => r.Exitoso);
            Assert.Single(estados, e => e == EstadoReserva.Aprobada);
            Assert.Single(estados, e => e == EstadoReserva.Pendiente);
        }

        [Fact]
        [Trait("HU", "HU-04")]
        [Trait("CA", "CA-09")]
        public async Task AprobarAsync_HorariosContiguos_ApruebaAmbos()
        {
            var equipo = await CrearEquipoAsync("LT-1");
            var a = await PendienteAsync("E1", equipo.IdEquipo, 14, 16);
            var b = await PendienteAsync("E2", equipo.IdEquipo, 16, 18);

            await AprobarAsync(a.IdReserva);

            Assert.True((await AprobarAsync(b.IdReserva)).Exitoso);
        }

        [Theory]
        [Trait("HU", "HU-04")]
        [Trait("CA", "CA-12")]
        [InlineData(EstadoReserva.Aprobada)]
        [InlineData(EstadoReserva.Rechazada)]
        [InlineData(EstadoReserva.Devuelta)]
        public async Task AprobarAsync_YaResuelta_NoCambia(EstadoReserva estado)
        {
            var estudiante = await CrearUsuarioAsync("E1");
            var equipo = await CrearEquipoAsync("LT-1");
            var reserva = await CrearReservaAsync(estudiante.IdUsuario, equipo.IdEquipo, HoraManana(14), HoraManana(16), estado);

            var resultado = await AprobarAsync(reserva.IdReserva);

            Assert.Equal(TipoResultado.EstadoInvalido, resultado.Tipo);
            Assert.Equal(estado, (await LeerReservaAsync(reserva.IdReserva)).Estado);
        }

        [Fact]
        [Trait("HU", "HU-04")]
        [Trait("CA", "CA-12")]
        public async Task AprobarAsync_Inexistente_DevuelveNoEncontrado()
        {
            Assert.Equal(TipoResultado.NoEncontrado, (await AprobarAsync(999_999)).Tipo);
        }

        [Theory]
        [Trait("HU", "HU-04")]
        [Trait("CA", "CA-13")]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("     ")]
        public async Task RechazarAsync_SinMotivo_FallaSinCambios(string? motivo)
        {
            var equipo = await CrearEquipoAsync("LT-1");
            var reserva = await PendienteAsync("E1", equipo.IdEquipo);

            var resultado = await RechazarAsync(reserva.IdReserva, motivo);

            Assert.Equal(Mensajes.MotivoObligatorio, resultado.Mensaje);
            Assert.Equal(EstadoReserva.Pendiente, (await LeerReservaAsync(reserva.IdReserva)).Estado);
        }

        [Fact]
        [Trait("HU", "HU-04")]
        [Trait("CA", "CA-13")]
        public async Task RechazarAsync_MotivoExcedido_FallaSinCambios()
        {
            var equipo = await CrearEquipoAsync("LT-1");
            var reserva = await PendienteAsync("E1", equipo.IdEquipo);

            var resultado = await RechazarAsync(reserva.IdReserva, new string('x', 201));

            Assert.Equal(Mensajes.MotivoExcedeLongitud, resultado.Mensaje);
            Assert.Equal(EstadoReserva.Pendiente, (await LeerReservaAsync(reserva.IdReserva)).Estado);
        }

        [Fact]
        [Trait("HU", "HU-04")]
        [Trait("CA", "CA-13")]
        public async Task RechazarAsync_ConMotivo_GuardaEstadoYMotivoSinEspacios()
        {
            var equipo = await CrearEquipoAsync("LT-1");
            var reserva = await PendienteAsync("E1", equipo.IdEquipo);

            var resultado = await RechazarAsync(reserva.IdReserva, "  Equipo en mantenimiento ");

            var guardada = await LeerReservaAsync(reserva.IdReserva);
            Assert.True(resultado.Exitoso);
            Assert.Equal(EstadoReserva.Rechazada, guardada.Estado);
            Assert.Equal("Equipo en mantenimiento", guardada.MotivoRechazo);
        }

        [Fact]
        [Trait("HU", "HU-04")]
        [Trait("CA", "CA-13")]
        public async Task RechazarAsync_ConMotivo_ElEquipoNoCambia()
        {
            var equipo = await CrearEquipoAsync("LT-1");
            var reserva = await PendienteAsync("E1", equipo.IdEquipo);

            await RechazarAsync(reserva.IdReserva, "Equipo en mantenimiento");
            RelojPrueba.Fijar(Reloj, HoraManana(15));

            Assert.Equal(EstadoEquipo.Disponible, await EstadoDelEquipoAsync(equipo.IdEquipo));
        }

        [Fact]
        [Trait("HU", "HU-04")]
        [Trait("CA", "CA-13")]
        public async Task RechazarAsync_YaResuelta_NoSobrescribe()
        {
            var estudiante = await CrearUsuarioAsync("E1");
            var equipo = await CrearEquipoAsync("LT-1");
            var reserva = await CrearReservaAsync(estudiante.IdUsuario, equipo.IdEquipo, HoraManana(14), HoraManana(16), EstadoReserva.Aprobada);

            var resultado = await RechazarAsync(reserva.IdReserva, "Equipo en mantenimiento");

            Assert.Equal(TipoResultado.EstadoInvalido, resultado.Tipo);
            Assert.Null((await LeerReservaAsync(reserva.IdReserva)).MotivoRechazo);
        }

        [Fact]
        [Trait("HU", "HU-04")]
        [Trait("CA", "CA-13")]
        public async Task RechazarAsync_Inexistente_DevuelveNoEncontrado()
        {
            Assert.Equal(TipoResultado.NoEncontrado, (await RechazarAsync(999_999, "Motivo")).Tipo);
        }

        [Fact]
        [Trait("HU", "HU-04")]
        [Trait("CA", "CA-13")]
        public async Task ListarPropiasAsync_ReservaRechazada_ElEstudianteVeElMotivo()
        {
            var estudiante = await CrearUsuarioAsync("E1");
            var equipo = await CrearEquipoAsync("LT-1");
            var reserva = await CrearReservaAsync(estudiante.IdUsuario, equipo.IdEquipo, HoraManana(14), HoraManana(16));
            await RechazarAsync(reserva.IdReserva, "Equipo en mantenimiento");

            await using var contexto = BaseDatos.CrearContexto();
            var modelo = await new ReservaService(contexto, Reloj).ListarPropiasAsync(estudiante.IdUsuario, null, 1);

            Assert.Equal("Equipo en mantenimiento", Assert.Single(modelo.Reservas).MotivoRechazo);
        }
    }
}
