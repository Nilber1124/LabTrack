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
