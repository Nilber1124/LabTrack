using LabTrack.Models;
using LabTrack.Services;
using LabTrack.Tests.Soporte;
using LabTrack.ViewModels;

namespace LabTrack.Tests.Services
{
    [Collection(ColeccionBaseDatos.Nombre)]
    [Trait("BD", "SQLServer")]
    public class CatalogoServiceTests : PruebaConBaseDatos
    {
        public CatalogoServiceTests(BaseDatosFixture baseDatos) : base(baseDatos)
        {
        }

        private async Task<CatalogoViewModel> ListarAsync(string? categoria = null, int pagina = 1, string? busqueda = null)
        {
            await using var contexto = BaseDatos.CrearContexto();
            return await new CatalogoService(contexto, Reloj).ListarAsync(categoria, pagina, busqueda);
        }

        [Theory]
        [Trait("HU", "HU-02")]
        [Trait("CA", "CA-05")]
        [InlineData("Router")]
        [InlineData("RE-1")]
        public async Task ListarAsync_Busqueda_FiltraPorNombreOCodigo(string busqueda)
        {
            await CrearEquipoAsync("RE-1", IdRedes, "Router Cisco");
            await CrearEquipoAsync("CO-1", IdComputo, "Laptop");

            var equipo = Assert.Single((await ListarAsync(busqueda: busqueda)).Equipos);

            Assert.Equal("RE-1", equipo.CodigoInventario);
        }

        [Fact]
        [Trait("HU", "HU-02")]
        [Trait("CA", "CA-05")]
        public async Task ListarAsync_BusquedaYCategoria_AplicaAmbas()
        {
            await CrearEquipoAsync("RE-1", IdRedes, "Router Cisco");
            await CrearEquipoAsync("CO-1", IdComputo, "Router de prueba");

            Assert.Equal("RE-1", Assert.Single((await ListarAsync("Redes", busqueda: "Router")).Equipos).CodigoInventario);
        }

        private async Task CrearUnoPorCategoriaAsync()
        {
            await CrearEquipoAsync("CO-1", IdComputo);
            await CrearEquipoAsync("EL-1", IdElectronica);
            await CrearEquipoAsync("RE-1", IdRedes);
        }

        [Fact]
        [Trait("HU", "HU-02")]
        [Trait("CA", "CA-04")]
        public async Task ListarAsync_Equipo_ProyectaNombreCodigoCategoriaYEstado()
        {
            await CrearEquipoAsync("LT-0231", IdComputo, "Laptop Dell");

            var equipo = Assert.Single((await ListarAsync()).Equipos);

            Assert.Equal("Laptop Dell", equipo.Nombre);
            Assert.Equal("LT-0231", equipo.CodigoInventario);
            Assert.Equal("Cómputo", equipo.Categoria);
            Assert.Equal(EstadoEquipo.Disponible, equipo.Estado);
        }

        [Fact]
        [Trait("HU", "HU-02")]
        [Trait("CA", "CA-04")]
        public async Task ListarAsync_PrestamoEnCurso_MarcaPrestadoConHoraDeFin()
        {
            var estudiante = await CrearUsuarioAsync("E1");
            var equipo = await CrearEquipoAsync("LT-1");
            await CrearReservaAsync(estudiante.IdUsuario, equipo.IdEquipo, Ahora.AddHours(-1), Ahora.AddHours(2), EstadoReserva.Aprobada);

            var resultado = Assert.Single((await ListarAsync()).Equipos);

            Assert.Equal(EstadoEquipo.Prestado, resultado.Estado);
            Assert.Equal(Ahora.AddHours(2), resultado.PrestadoHasta);
        }

        [Fact]
        [Trait("HU", "HU-02")]
        [Trait("CA", "CA-04")]
        public async Task ListarAsync_AprobadaFutura_SigueDisponible()
        {
            var estudiante = await CrearUsuarioAsync("E1");
            var equipo = await CrearEquipoAsync("LT-1");
            await CrearReservaAsync(estudiante.IdUsuario, equipo.IdEquipo, HoraManana(14), HoraManana(16), EstadoReserva.Aprobada);

            Assert.Equal(EstadoEquipo.Disponible, Assert.Single((await ListarAsync()).Equipos).Estado);
        }

        [Fact]
        [Trait("HU", "HU-02")]
        [Trait("CA", "CA-04")]
        public async Task ListarAsync_MezclaDeEstados_CuentaLosDisponibles()
        {
            var estudiante = await CrearUsuarioAsync("E1");
            var prestado = await CrearEquipoAsync("LT-1");
            await CrearEquipoAsync("LT-2");
            await CrearReservaAsync(estudiante.IdUsuario, prestado.IdEquipo, Ahora.AddHours(-1), Ahora.AddHours(1), EstadoReserva.Aprobada);

            Assert.Equal(1, (await ListarAsync()).TotalDisponibles);
        }

        [Theory]
        [Trait("HU", "HU-02")]
        [Trait("CA", "CA-05")]
        [InlineData("Cómputo", "CO-1")]
        [InlineData("Electrónica", "EL-1")]
        [InlineData("Redes", "RE-1")]
        public async Task ListarAsync_FiltroPorCategoria_DevuelveSoloEsaCategoria(string categoria, string codigo)
        {
            await CrearUnoPorCategoriaAsync();

            var equipo = Assert.Single((await ListarAsync(categoria)).Equipos);

            Assert.Equal(codigo, equipo.CodigoInventario);
        }

        [Fact]
        [Trait("HU", "HU-02")]
        [Trait("CA", "CA-05")]
        public async Task ListarAsync_SinFiltro_DevuelveTodo()
        {
            await CrearUnoPorCategoriaAsync();

            Assert.Equal(3, (await ListarAsync()).Equipos.Count);
        }

        [Fact]
        [Trait("HU", "HU-02")]
        [Trait("CA", "CA-05")]
        public async Task ListarAsync_MasDeUnaPagina_TotalIncluyeTodosLosEquipos()
        {
            for (var i = 0; i <= CatalogoService.TamanoPagina; i++)
            {
                await CrearEquipoAsync($"LT-{i}");
            }

            var primera = await ListarAsync();
            var segunda = await ListarAsync(pagina: 2);

            Assert.Equal(CatalogoService.TamanoPagina + 1, primera.Paginacion.Total);
            Assert.Equal(CatalogoService.TamanoPagina, primera.Equipos.Count);
            Assert.Single(segunda.Equipos);
        }

        [Fact]
        [Trait("HU", "HU-02")]
        [Trait("CA", "CA-05")]
        public async Task ListarAsync_FiltroParcial_NoCoincide()
        {
            await CrearUnoPorCategoriaAsync();

            Assert.Empty((await ListarAsync("Red")).Equipos);
        }

        [Fact]
        [Trait("HU", "HU-02")]
        [Trait("CA", "CA-05")]
        public async Task ListarAsync_Categorias_DevuelveLasTresConSuTotal()
        {
            await CrearUnoPorCategoriaAsync();
            await CrearEquipoAsync("RE-2", IdRedes);

            var categorias = (await ListarAsync()).Categorias;

            Assert.Equal(new[] { "Cómputo", "Electrónica", "Redes" }, categorias.Select(c => c.Nombre));
            Assert.Equal(new[] { 1, 1, 2 }, categorias.Select(c => c.Total));
        }

        [Fact]
        [Trait("HU", "HU-02")]
        [Trait("CA", "CA-06")]
        public async Task ListarAsync_CategoriaSinEquipos_IndicaVacio()
        {
            await CrearEquipoAsync("CO-1", IdComputo);

            Assert.True((await ListarAsync("Redes")).SinResultados);
        }
    }
}
