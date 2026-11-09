using LabTrack.Data;
using LabTrack.Models;
using LabTrack.Tests.Soporte;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace LabTrack.Tests.Data
{
    [Collection(ColeccionBaseDatos.Nombre)]
    [Trait("BD", "SQLServer")]
    public class DbSeederTests : PruebaConBaseDatos
    {
        public DbSeederTests(BaseDatosFixture baseDatos) : base(baseDatos)
        {
        }

        private async Task SembrarAsync()
        {
            var configuracion = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Semilla:ContrasenaDemostracion"] = "clave-simulacion",
                    ["Semilla:Encargado:Codigo"] = "ENC-001",
                    ["Semilla:Encargado:Nombre"] = "Encargado",
                    ["Semilla:Encargado:Contrasena"] = "clave-encargado",
                    ["Semilla:Estudiantes:0:Codigo"] = "20231458",
                    ["Semilla:Estudiantes:0:Nombre"] = "Estudiante",
                    ["Semilla:Estudiantes:0:Contrasena"] = "clave-estudiante"
                })
                .Build();

            await using var contexto = BaseDatos.CrearContexto();
            await DbSeeder.SembrarAsync(contexto, configuracion, new PasswordHasher<Usuario>(), Reloj, NullLogger.Instance);
        }

        private async Task<(int Usuarios, int Equipos, int Reservas)> ContarAsync()
        {
            await using var contexto = BaseDatos.CrearContexto();
            return (await contexto.Usuarios.CountAsync(), await contexto.Equipos.CountAsync(), await contexto.Reservas.CountAsync());
        }

        [Fact]
        [Trait("HU", "HU-02")]
        [Trait("CA", "CA-04")]
        public async Task SembrarAsync_BaseVacia_CreaTodasLasSituacionesDeReserva()
        {
            await SembrarAsync();

            await using var contexto = BaseDatos.CrearContexto();
            var estados = await contexto.Reservas.Select(r => r.Estado).Distinct().ToListAsync();
            Assert.Equal(Enum.GetValues<EstadoReserva>().Order(), estados.Order());
            Assert.Equal(30, await contexto.Equipos.CountAsync(e => !e.DeBaja));
            Assert.Equal(2, await contexto.Equipos.CountAsync(e => e.DeBaja));
        }

        [Fact]
        [Trait("HU", "HU-02")]
        [Trait("CA", "CA-04")]
        public async Task SembrarAsync_BaseVacia_TodosLosEquiposTienenFoto()
        {
            await SembrarAsync();

            await using var contexto = BaseDatos.CrearContexto();
            Assert.Equal(0, await contexto.Equipos.CountAsync(e => e.FotoUrl == null));
        }

        [Fact]
        [Trait("HU", "HU-02")]
        [Trait("CA", "CA-04")]
        public async Task SembrarAsync_EquipoConFotoPropia_NoLaReemplaza()
        {
            await SembrarAsync();
            const string fotoPropia = "https://res.cloudinary.com/demo/image/upload/v1/propia.jpg";
            await using (var contexto = BaseDatos.CrearContexto())
            {
                await contexto.Equipos.Where(e => e.CodigoInventario == "LT-0231")
                    .ExecuteUpdateAsync(s => s.SetProperty(e => e.FotoUrl, fotoPropia));
            }

            await SembrarAsync();

            await using var lectura = BaseDatos.CrearContexto();
            Assert.Equal(fotoPropia, await lectura.Equipos.Where(e => e.CodigoInventario == "LT-0231").Select(e => e.FotoUrl).SingleAsync());
        }

        [Fact]
        [Trait("HU", "HU-02")]
        [Trait("CA", "CA-04")]
        public async Task SembrarAsync_SegundaEjecucion_NoDuplicaDatos()
        {
            await SembrarAsync();
            var primera = await ContarAsync();

            await SembrarAsync();

            Assert.Equal(primera, await ContarAsync());
        }

        [Fact]
        [Trait("HU", "HU-05")]
        [Trait("CA", "CA-16")]
        public async Task SembrarAsync_ReservaVencida_QuedaPendienteDeDevolver()
        {
            await SembrarAsync();

            await using var contexto = BaseDatos.CrearContexto();
            Assert.True(await contexto.Reservas.AnyAsync(r => r.Estado == EstadoReserva.Aprobada && r.Fin < Ahora));
        }
    }
}
