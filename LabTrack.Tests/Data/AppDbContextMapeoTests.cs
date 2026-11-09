using LabTrack.Data;
using LabTrack.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace LabTrack.Tests.Data
{
    // Solo inspecciona metadatos: UseSqlServer no abre ninguna conexión.
    public class AppDbContextMapeoTests : IDisposable
    {
        private readonly AppDbContext _contexto = new(new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer("Server=sin-conexion;Database=Mapeo").Options);

        public void Dispose() => _contexto.Dispose();

        private IModel Modelo => _contexto.GetService<IDesignTimeModel>().Model;
        private IEntityType Entidad<T>() => Modelo.FindEntityType(typeof(T))!;

        private IForeignKey Relacion<THija, TPrincipal>()
            => Entidad<THija>().GetForeignKeys().Single(fk => fk.PrincipalEntityType.ClrType == typeof(TPrincipal));

        private IIndex Indice<T>(params string[] propiedades)
            => Entidad<T>().GetIndexes().First(i => i.Properties.Select(p => p.Name).SequenceEqual(propiedades) && i.GetFilter() is null);

        [Theory]
        [Trait("HU", "HU-01")]
        [Trait("CA", "CA-01")]
        [InlineData(typeof(Rol), "Roles")]
        [InlineData(typeof(Usuario), "Usuarios")]
        [InlineData(typeof(Categoria), "Categorias")]
        [InlineData(typeof(Equipo), "Equipos")]
        [InlineData(typeof(Reserva), "Reservas")]
        public void Tablas_CadaEntidad_UsaElNombreDelDbSet(Type entidad, string tabla)
        {
            Assert.Equal(tabla, Modelo.FindEntityType(entidad)!.GetTableName());
        }

        [Fact]
        [Trait("HU", "HU-01")]
        [Trait("CA", "CA-01")]
        public void Usuario_Codigo_TieneIndiceUnico()
        {
            Assert.True(Indice<Usuario>(nameof(Usuario.Codigo)).IsUnique);
        }

        [Fact]
        [Trait("HU", "HU-01")]
        [Trait("CA", "CA-01")]
        public void Usuario_Rol_BorradoRestringido()
        {
            Assert.Equal(DeleteBehavior.Restrict, Relacion<Usuario, Rol>().DeleteBehavior);
        }

        [Fact]
        [Trait("HU", "HU-01")]
        [Trait("CA", "CA-03")]
        public void Usuario_BloqueadoHasta_PrecisionDeMilisegundos()
        {
            Assert.Equal(3, Entidad<Usuario>().FindProperty(nameof(Usuario.BloqueadoHasta))!.GetPrecision());
        }

        [Fact]
        [Trait("HU", "HU-02")]
        [Trait("CA", "CA-04")]
        public void Equipo_CodigoInventario_TieneIndiceUnico()
        {
            Assert.True(Indice<Equipo>(nameof(Equipo.CodigoInventario)).IsUnique);
        }

        [Fact]
        [Trait("HU", "HU-02")]
        [Trait("CA", "CA-04")]
        public void Equipo_SinColumnaEstado_ElEstadoSeCalcula()
        {
            Assert.Null(Entidad<Equipo>().FindProperty("Estado"));
        }

        [Fact]
        [Trait("HU", "HU-02")]
        [Trait("CA", "CA-05")]
        public void Equipo_Categoria_BorradoRestringido()
        {
            Assert.Equal(DeleteBehavior.Restrict, Relacion<Equipo, Categoria>().DeleteBehavior);
        }

        [Fact]
        [Trait("HU", "HU-02")]
        [Trait("CA", "CA-05")]
        public void Categoria_Semilla_ContieneLasTresCategoriasExactas()
        {
            var nombres = Entidad<Categoria>().GetSeedData().Select(d => (string)d[nameof(Categoria.Nombre)]!);

            Assert.Equal(new[] { "Cómputo", "Electrónica", "Redes" }, nombres.Order());
        }

        [Fact]
        [Trait("HU", "HU-01")]
        [Trait("CA", "CA-01")]
        public void Rol_Semilla_ContieneLosTresRoles()
        {
            var nombres = Entidad<Rol>().GetSeedData().Select(d => (string)d[nameof(Rol.Nombre)]!);

            Assert.Equal(new[] { "ADMINISTRADOR", "ENCARGADO", "ESTUDIANTE" }, nombres.Order());
        }

        [Fact]
        [Trait("HU", "HU-02")]
        [Trait("CA", "CA-07")]
        public void Equipo_IdCategoria_TieneIndice()
        {
            Assert.NotNull(Indice<Equipo>(nameof(Equipo.IdCategoria)));
        }

        [Fact]
        [Trait("HU", "HU-03")]
        [Trait("CA", "CA-08")]
        public void Reserva_Periodo_TieneRestriccionFinPosteriorAInicio()
        {
            var restriccion = Entidad<Reserva>().GetCheckConstraints().Single(c => c.Name == "CK_Reservas_Periodo");

            Assert.Equal("[Fin] > [Inicio]", restriccion.Sql);
        }

        [Theory]
        [Trait("HU", "HU-03")]
        [Trait("CA", "CA-08")]
        [InlineData(nameof(Reserva.Inicio), 0)]
        [InlineData(nameof(Reserva.Fin), 0)]
        [InlineData(nameof(Reserva.FechaDevolucionReal), 3)]
        public void Reserva_Fechas_TienenPrecisionEsperada(string propiedad, int precision)
        {
            Assert.Equal(precision, Entidad<Reserva>().FindProperty(propiedad)!.GetPrecision());
        }

        [Fact]
        [Trait("HU", "HU-03")]
        [Trait("CA", "CA-09")]
        public void Reserva_IndiceDeCruce_CubreEquipoInicioYFin()
        {
            Assert.NotNull(Indice<Reserva>(nameof(Reserva.IdEquipo), nameof(Reserva.Inicio), nameof(Reserva.Fin)));
        }

        [Theory]
        [Trait("HU", "HU-03")]
        [Trait("CA", "CA-09")]
        [InlineData(typeof(Reserva))]
        [InlineData(typeof(Equipo))]
        public void Version_ReservaYEquipo_EsTokenDeConcurrencia(Type entidad)
        {
            var version = Modelo.FindEntityType(entidad)!.FindProperty("Version")!;

            Assert.True(version.IsConcurrencyToken);
            Assert.Equal("rowversion", version.GetColumnType());
        }

        [Fact]
        [Trait("HU", "HU-03")]
        [Trait("CA", "CA-10")]
        public void Reserva_IndiceFiltrado_ImpideDosReservasActivas()
        {
            var indice = Entidad<Reserva>().GetIndexes().Single(i => i.GetFilter() is not null);

            Assert.True(indice.IsUnique);
            Assert.Equal(nameof(Reserva.IdUsuario), indice.Properties.Single().Name);
            Assert.Equal("[Estado] IN ('Pendiente', 'Aprobada')", indice.GetFilter());
        }

        [Theory]
        [Trait("HU", "HU-04")]
        [Trait("CA", "CA-12")]
        [InlineData(typeof(Usuario))]
        [InlineData(typeof(Equipo))]
        public void Reserva_Relaciones_BorradoRestringido(Type principal)
        {
            var relacion = Entidad<Reserva>().GetForeignKeys().Single(fk => fk.PrincipalEntityType.ClrType == principal);

            Assert.Equal(DeleteBehavior.Restrict, relacion.DeleteBehavior);
        }

        [Fact]
        [Trait("HU", "HU-04")]
        [Trait("CA", "CA-12")]
        public void Reserva_Estado_SeGuardaComoTexto()
        {
            Assert.Equal(typeof(string), Entidad<Reserva>().FindProperty(nameof(Reserva.Estado))!.GetProviderClrType());
        }

        [Fact]
        [Trait("HU", "HU-05")]
        [Trait("CA", "CA-16")]
        public void Usuario_Estado_SeGuardaComoTexto()
        {
            Assert.Equal(typeof(string), Entidad<Usuario>().FindProperty(nameof(Usuario.Estado))!.GetProviderClrType());
        }
    }
}
