using LabTrack.Models;
using LabTrack.Reglas;
using LabTrack.Services;
using LabTrack.Tests.Soporte;
using LabTrack.ViewModels;

namespace LabTrack.Tests.Services
{
    [Collection(ColeccionBaseDatos.Nombre)]
    [Trait("BD", "SQLServer")]
    public class PenalizacionesServiceTests : PruebaConBaseDatos
    {
        public PenalizacionesServiceTests(BaseDatosFixture baseDatos) : base(baseDatos)
        {
        }

        private async Task<PenalizadosViewModel> ListarAsync(string? busqueda = null)
        {
            await using var contexto = BaseDatos.CrearContexto();
            return await new DevolucionService(contexto, Reloj).ListarPenalizadosAsync(busqueda, 1);
        }

        private async Task<ResultadoOperacion> LevantarAsync(int idUsuario)
        {
            await using var contexto = BaseDatos.CrearContexto();
            return await new DevolucionService(contexto, Reloj).LevantarPenalizacionAsync(idUsuario);
        }

        // Recorrido real: préstamo de 08:00 a 12:00 devuelto 45 minutos tarde.
        private async Task<Usuario> PenalizarAsync(string codigo = "E1", string inventario = "LT-1")
        {
            var estudiante = await CrearUsuarioAsync(codigo);
            var equipo = await CrearEquipoAsync(inventario);
            var reserva = await CrearReservaAsync(estudiante.IdUsuario, equipo.IdEquipo, Ahora.AddHours(-2), Ahora.AddHours(2), EstadoReserva.Aprobada);
            RelojPrueba.Fijar(Reloj, Ahora.AddHours(2).AddMinutes(45));

            await using var contexto = BaseDatos.CrearContexto();
            await new DevolucionService(contexto, Reloj).MarcarDevolucionAsync(reserva.IdReserva);
            return estudiante;
        }

        [Fact]
        [Trait("HU", "HU-05")]
        [Trait("CA", "CA-16")]
        public async Task ListarPenalizadosAsync_VariosEstudiantes_DevuelveSoloPenalizados()
        {
            var penalizado = await PenalizarAsync();
            await CrearUsuarioAsync("ACTIVO");

            var fila = Assert.Single((await ListarAsync()).Penalizados);

            Assert.Equal(penalizado.IdUsuario, fila.IdUsuario);
        }

        [Fact]
        [Trait("HU", "HU-05")]
        [Trait("CA", "CA-16")]
        public async Task ListarPenalizadosAsync_DevolucionTardia_MuestraFechaYMotivo()
        {
            await PenalizarAsync();

            var fila = Assert.Single((await ListarAsync()).Penalizados);

            Assert.Equal(Ahora.AddHours(2).AddMinutes(45), fila.FechaPenalizacion);
            Assert.Equal("Devolución tardía · LT-1 · 45 min", fila.Motivo);
        }

        [Fact]
        [Trait("HU", "HU-05")]
        [Trait("CA", "CA-16")]
        public async Task ListarPenalizadosAsync_SinDevolucionTardia_UsaMotivoGenerico()
        {
            await CrearUsuarioAsync("E1", estado: EstadoUsuario.Penalizado);

            var fila = Assert.Single((await ListarAsync()).Penalizados);

            Assert.Null(fila.FechaPenalizacion);
            Assert.Equal("Sin devolución tardía registrada", fila.Motivo);
        }

        [Theory]
        [Trait("HU", "HU-05")]
        [Trait("CA", "CA-16")]
        [InlineData("Nombre E1")]
        [InlineData("E1")]
        public async Task ListarPenalizadosAsync_Busqueda_FiltraPorNombreOCodigo(string busqueda)
        {
            var buscado = await PenalizarAsync();
            await CrearUsuarioAsync("X9", estado: EstadoUsuario.Penalizado);

            Assert.Equal(buscado.IdUsuario, Assert.Single((await ListarAsync(busqueda)).Penalizados).IdUsuario);
        }

        [Fact]
        [Trait("HU", "HU-05")]
        [Trait("CA", "CA-16")]
        public async Task LevantarPenalizacionAsync_Penalizado_LoDejaActivo()
        {
            var estudiante = await PenalizarAsync();

            var resultado = await LevantarAsync(estudiante.IdUsuario);

            Assert.True(resultado.Exitoso);
            Assert.Equal(EstadoUsuario.Activo, (await LeerUsuarioAsync(estudiante.IdUsuario)).Estado);
            Assert.Empty((await ListarAsync()).Penalizados);
        }

        [Fact]
        [Trait("HU", "HU-05")]
        [Trait("CA", "CA-16")]
        public async Task LevantarPenalizacionAsync_EstudianteActivo_DevuelveEstadoInvalido()
        {
            var estudiante = await CrearUsuarioAsync("E1");

            var resultado = await LevantarAsync(estudiante.IdUsuario);

            Assert.Equal(TipoResultado.EstadoInvalido, resultado.Tipo);
            Assert.Equal(Mensajes.PenalizacionNoActiva, resultado.Mensaje);
        }

        [Fact]
        [Trait("HU", "HU-05")]
        [Trait("CA", "CA-16")]
        public async Task ContarPenalizadosAsync_ConPenalizados_LosCuenta()
        {
            await PenalizarAsync();
            await CrearUsuarioAsync("ACTIVO");

            await using var contexto = BaseDatos.CrearContexto();

            Assert.Equal(1, await new DevolucionService(contexto, Reloj).ContarPenalizadosAsync());
        }

        [Fact]
        [Trait("HU", "HU-04")]
        [Trait("CA", "CA-11")]
        public async Task ContarSolicitudesPendientesAsync_VariosEstados_CuentaSoloPendientes()
        {
            var estudiante = await CrearUsuarioAsync("E1");
            var equipo = await CrearEquipoAsync("LT-1");
            await CrearReservaAsync(estudiante.IdUsuario, equipo.IdEquipo, HoraManana(8), HoraManana(9));
            await CrearReservaAsync(estudiante.IdUsuario, equipo.IdEquipo, HoraManana(10), HoraManana(11), EstadoReserva.Rechazada);

            await using var contexto = BaseDatos.CrearContexto();

            Assert.Equal(1, await new DevolucionService(contexto, Reloj).ContarSolicitudesPendientesAsync());
        }

        [Fact]
        [Trait("HU", "HU-05")]
        [Trait("CA", "CA-16")]
        public async Task ObtenerFichaAsync_EstudiantePenalizado_MuestraTotalesYDetalle()
        {
            var estudiante = await PenalizarAsync();

            await using var contexto = BaseDatos.CrearContexto();
            var ficha = await new DevolucionService(contexto, Reloj).ObtenerFichaAsync(estudiante.IdUsuario);

            Assert.Equal((1, 1), (ficha!.ReservasTotales, ficha.DevolucionesTardias));
            Assert.Contains("Devolución tardía de LT-1 (45 min)", ficha.DetallePenalizacion);
            Assert.Null(ficha.ReservaActiva);
        }

        [Fact]
        [Trait("HU", "HU-05")]
        [Trait("CA", "CA-14")]
        public async Task ObtenerFichaAsync_ConReservaActiva_MuestraSuEquipo()
        {
            var estudiante = await CrearUsuarioAsync("E1");
            var equipo = await CrearEquipoAsync("OS-0112");
            await CrearReservaAsync(estudiante.IdUsuario, equipo.IdEquipo, HoraManana(8), HoraManana(9), EstadoReserva.Aprobada);

            await using var contexto = BaseDatos.CrearContexto();
            var ficha = await new DevolucionService(contexto, Reloj).ObtenerFichaAsync(estudiante.IdUsuario);

            Assert.Equal("OS-0112", ficha!.ReservaActiva);
            Assert.Null(ficha.DetallePenalizacion);
        }

        [Fact]
        [Trait("HU", "HU-05")]
        [Trait("CA", "CA-14")]
        public async Task ObtenerFichaAsync_NoEsEstudiante_DevuelveNull()
        {
            var encargado = await CrearUsuarioAsync("ENC", idRol: IdRolEncargado);

            await using var contexto = BaseDatos.CrearContexto();

            Assert.Null(await new DevolucionService(contexto, Reloj).ObtenerFichaAsync(encargado.IdUsuario));
        }

        [Fact]
        [Trait("HU", "HU-05")]
        [Trait("CA", "CA-16")]
        public async Task LevantarPenalizacionAsync_Inexistente_DevuelveNoEncontrado()
        {
            Assert.Equal(TipoResultado.NoEncontrado, (await LevantarAsync(999_999)).Tipo);
        }

        [Fact]
        [Trait("HU", "HU-05")]
        [Trait("CA", "CA-16")]
        public async Task CrearAsync_TrasLevantarLaPenalizacion_PermiteReservar()
        {
            var estudiante = await PenalizarAsync();
            var equipo = await CrearEquipoAsync("LT-2");
            await LevantarAsync(estudiante.IdUsuario);

            await using var contexto = BaseDatos.CrearContexto();
            var resultado = await new ReservaService(contexto, Reloj)
                .CrearAsync(estudiante.IdUsuario, equipo.IdEquipo, Manana, new TimeOnly(10, 0), new TimeOnly(12, 0));

            Assert.True(resultado.Exitoso);
        }
    }
}
