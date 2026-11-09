using LabTrack.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace LabTrack.Tests.Soporte
{
    // Crea una base temporal con las migraciones reales y la elimina al terminar.
    // El nombre es aleatorio: nunca se toca la base LabTrack.
    public sealed class BaseDatosFixture : IAsyncLifetime
    {
        private const string VariableConexion = "LABTRACK_TEST_SQLSERVER";

        private DbContextOptions<AppDbContext>? _opciones;

        public AppDbContext CrearContexto() => new(_opciones!);

        public async Task InitializeAsync()
        {
            var conexion = Environment.GetEnvironmentVariable(VariableConexion);
            if (string.IsNullOrWhiteSpace(conexion))
            {
                throw new InvalidOperationException(
                    $"Define {VariableConexion} con una conexión a SQL Server autorizada para crear y eliminar bases.");
            }

            var cadena = new SqlConnectionStringBuilder(conexion)
            {
                InitialCatalog = $"LabTrack_Pruebas_{Guid.NewGuid():N}"
            };

            _opciones = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlServer(cadena.ConnectionString)
                .Options;

            await using var contexto = CrearContexto();
            await contexto.Database.MigrateAsync();
        }

        public async Task DisposeAsync()
        {
            if (_opciones is null)
            {
                return;
            }

            await using var contexto = CrearContexto();
            await contexto.Database.EnsureDeletedAsync();
        }

        // Conserva Roles y Categorias, que vienen de la migración.
        public async Task LimpiarAsync()
        {
            await using var contexto = CrearContexto();
            await contexto.Reservas.ExecuteDeleteAsync();
            await contexto.Equipos.ExecuteDeleteAsync();
            await contexto.Usuarios.ExecuteDeleteAsync();
        }
    }
}
