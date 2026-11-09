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
            Assert.Equal(24 * DbSeeder.UnidadesPorModelo, await contexto.Equipos.CountAsync());
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
