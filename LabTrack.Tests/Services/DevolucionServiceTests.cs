using LabTrack.Models;
using LabTrack.Reglas;
using LabTrack.Services;
using LabTrack.Tests.Soporte;
using LabTrack.ViewModels;

namespace LabTrack.Tests.Services
{
    [Collection(ColeccionBaseDatos.Nombre)]
    [Trait("BD", "SQLServer")]
    public class DevolucionServiceTests : PruebaConBaseDatos
    {
        public DevolucionServiceTests(BaseDatosFixture baseDatos) : base(baseDatos)
        {
        }

        private async Task<ResultadoOperacion> MarcarAsync(int idReserva)
        {
            await using var contexto = BaseDatos.CrearContexto();
            return await new DevolucionService(contexto, Reloj).MarcarDevolucionAsync(idReserva);
        }

        private async Task<DevolucionesViewModel> ListarAsync(string? busqueda = null)
        {
            await using var contexto = BaseDatos.CrearContexto();
            return await new DevolucionService(contexto, Reloj).ListarActivasAsync(busqueda, 1);
        }

        // Reserva aprobada de 08:00 a 12:00 del día actual, en curso a las 10:00.
        private async Task<(Usuario Estudiante, Equipo Equipo, Reserva Reserva)> PrestamoAsync(
            string codigo = "E1", string inventario = "LT-1", EstadoReserva estado = EstadoReserva.Aprobada)
        {
            var estudiante = await CrearUsuarioAsync(codigo);
            var equipo = await CrearEquipoAsync(inventario);
            var reserva = await CrearReservaAsync(estudiante.IdUsuario, equipo.IdEquipo, Ahora.AddHours(-2), Ahora.AddHours(2), estado);
            return (estudiante, equipo, reserva);
        }

        [Fact]
        [Trait("HU", "HU-05")]
        [Trait("CA", "CA-14")]
        public async Task ListarActivasAsync_VariosEstados_DevuelveSoloAprobadas()
        {
            var (_, _, activa) = await PrestamoAsync();
            await PrestamoAsync("E2", "LT-2", EstadoReserva.Pendiente);
            await PrestamoAsync("E3", "LT-3", EstadoReserva.Devuelta);

            var reserva = Assert.Single((await ListarAsync()).Reservas);

            Assert.Equal(activa.IdReserva, reserva.IdReserva);
        }

        [Theory]
        [Trait("HU", "HU-05")]
        [Trait("CA", "CA-14")]
        [InlineData("Nombre E1")]
        [InlineData("E1")]
        [InlineData("LT-1")]
        [InlineData("Equipo LT-1")]
        public async Task ListarActivasAsync_Busqueda_EncuentraLaReserva(string busqueda)
        {
            var (_, _, activa) = await PrestamoAsync();
            await PrestamoAsync("X9", "RT-9");

            var reserva = Assert.Single((await ListarAsync(busqueda)).Reservas);

            Assert.Equal(activa.IdReserva, reserva.IdReserva);
        }

        [Fact]
        [Trait("HU", "HU-05")]
        [Trait("CA", "CA-14")]
        public async Task ListarActivasAsync_BusquedaSinCoincidencias_DevuelveVacio()
        {
            await PrestamoAsync();

            Assert.Empty((await ListarAsync("LT-9999")).Reservas);
        }

        [Fact]
        [Trait("HU", "HU-05")]
        [Trait("CA", "CA-14")]
        public async Task ListarActivasAsync_FinYaPasado_LaMarcaVencida()
        {
            await PrestamoAsync();
            Reloj.Advance(TimeSpan.FromHours(2) + TimeSpan.FromMinutes(45));

            var modelo = await ListarAsync();

            var reserva = Assert.Single(modelo.Reservas);
            Assert.True(reserva.Vencida);
            Assert.Equal("vencida", reserva.Situacion);
            Assert.Equal(1, modelo.TotalVencidas);
        }

        [Fact]
        [Trait("HU", "HU-05")]
        [Trait("CA", "CA-14")]
        public async Task ListarActivasAsync_EnCurso_IndicaTiempoRestante()
        {
            await PrestamoAsync();

            var reserva = Assert.Single((await ListarAsync()).Reservas);

            Assert.Equal("activa", reserva.Situacion);
            Assert.Equal("Faltan 2 h 0 min", reserva.Nota);
        }

        [Fact]
        [Trait("HU", "HU-05")]
        [Trait("CA", "CA-15")]
        public async Task MarcarDevolucionAsync_Aprobada_PasaADevueltaConFechaReal()
        {
            var (_, _, reserva) = await PrestamoAsync();

            var resultado = await MarcarAsync(reserva.IdReserva);

            var guardada = await LeerReservaAsync(reserva.IdReserva);
            Assert.True(resultado.Exitoso);
            Assert.Equal(EstadoReserva.Devuelta, guardada.Estado);
            Assert.Equal(Ahora, guardada.FechaDevolucionReal);
        }

        [Fact]
        [Trait("HU", "HU-05")]
        [Trait("CA", "CA-15")]
        public async Task MarcarDevolucionAsync_EquipoPrestado_QuedaDisponibleInmediatamente()
        {
            var (_, equipo, reserva) = await PrestamoAsync();
            Assert.Equal(EstadoEquipo.Prestado, await EstadoDelEquipoAsync(equipo.IdEquipo));

            await MarcarAsync(reserva.IdReserva);

            Assert.Equal(EstadoEquipo.Disponible, await EstadoDelEquipoAsync(equipo.IdEquipo));
        }

        [Theory]
        [Trait("HU", "HU-05")]
        [Trait("CA", "CA-14")]
        [InlineData(EstadoReserva.Pendiente)]
        [InlineData(EstadoReserva.Rechazada)]
        [InlineData(EstadoReserva.Devuelta)]
        public async Task MarcarDevolucionAsync_ReservaNoAprobada_FallaSinCambios(EstadoReserva estado)
        {
            var (_, _, reserva) = await PrestamoAsync(estado: estado);

            var resultado = await MarcarAsync(reserva.IdReserva);

            Assert.Equal(Mensajes.SoloReservasAprobadas, resultado.Mensaje);
            Assert.Equal(estado, (await LeerReservaAsync(reserva.IdReserva)).Estado);
        }

        [Fact]
        [Trait("HU", "HU-05")]
        [Trait("CA", "CA-14")]
        public async Task MarcarDevolucionAsync_Inexistente_DevuelveNoEncontrado()
        {
            Assert.Equal(TipoResultado.NoEncontrado, (await MarcarAsync(999_999)).Tipo);
        }

        [Fact]
        [Trait("HU", "HU-05")]
        [Trait("CA", "CA-16")]
        public async Task MarcarDevolucionAsync_AntesDelFin_NoPenaliza()
        {
            var (estudiante, _, reserva) = await PrestamoAsync();

            var resultado = await MarcarAsync(reserva.IdReserva);

            Assert.Null(resultado.Aviso);
            Assert.Equal(EstadoUsuario.Activo, (await LeerUsuarioAsync(estudiante.IdUsuario)).Estado);
        }

        [Fact]
        [Trait("HU", "HU-05")]
        [Trait("CA", "CA-16")]
        public async Task MarcarDevolucionAsync_ExactamenteALaHoraDeFin_NoPenaliza()
        {
            var (estudiante, _, reserva) = await PrestamoAsync();
            Reloj.Advance(TimeSpan.FromHours(2));

            await MarcarAsync(reserva.IdReserva);

            Assert.Equal(EstadoUsuario.Activo, (await LeerUsuarioAsync(estudiante.IdUsuario)).Estado);
        }

        [Fact]
        [Trait("HU", "HU-05")]
        [Trait("CA", "CA-16")]
        public async Task MarcarDevolucionAsync_UnMinutoDespues_PersistePenalizado()
        {
            var (estudiante, _, reserva) = await PrestamoAsync();
            Reloj.Advance(TimeSpan.FromHours(2) + TimeSpan.FromMinutes(1));

            var resultado = await MarcarAsync(reserva.IdReserva);

            Assert.NotNull(resultado.Aviso);
            Assert.Equal(EstadoUsuario.Penalizado, (await LeerUsuarioAsync(estudiante.IdUsuario)).Estado);
        }

        [Fact]
        [Trait("HU", "HU-05")]
        [Trait("CA", "CA-16")]
        public async Task MarcarDevolucionAsync_Tardia_ElEquipoTambienQuedaDisponible()
        {
            var (_, equipo, reserva) = await PrestamoAsync();
            Reloj.Advance(TimeSpan.FromHours(3));

            await MarcarAsync(reserva.IdReserva);

            Assert.Equal(EstadoEquipo.Disponible, await EstadoDelEquipoAsync(equipo.IdEquipo));
        }
    }
}
