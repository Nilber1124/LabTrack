using LabTrack.Models;
using LabTrack.Reglas;
using Microsoft.EntityFrameworkCore;

namespace LabTrack.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Rol> Roles { get; set; }
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Categoria> Categorias { get; set; }
        public DbSet<Equipo> Equipos { get; set; }
        public DbSet<Reserva> Reservas { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            ConfigurarRelaciones(modelBuilder);
            ConfigurarIndices(modelBuilder);
            ConfigurarColumnas(modelBuilder);
            ConfigurarRestricciones(modelBuilder);
            ConfigurarSemilla(modelBuilder);
        }

        private static void ConfigurarRelaciones(ModelBuilder modelBuilder)
        {
            // Se restringe el borrado para conservar el historial de préstamos.
            modelBuilder.Entity<Usuario>()
                .HasOne(u => u.Rol)
                .WithMany(r => r.Usuarios)
                .HasForeignKey(u => u.IdRol)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Equipo>()
                .HasOne(e => e.Categoria)
                .WithMany(c => c.Equipos)
                .HasForeignKey(e => e.IdCategoria)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Reserva>()
                .HasOne(r => r.Usuario)
                .WithMany(u => u.Reservas)
                .HasForeignKey(r => r.IdUsuario)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Reserva>()
                .HasOne(r => r.Equipo)
                .WithMany(e => e.Reservas)
                .HasForeignKey(r => r.IdEquipo)
                .OnDelete(DeleteBehavior.Restrict);
        }

        private static void ConfigurarIndices(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Usuario>().HasIndex(u => u.Codigo).IsUnique();
            modelBuilder.Entity<Categoria>().HasIndex(c => c.Nombre).IsUnique();
            modelBuilder.Entity<Equipo>().HasIndex(e => e.CodigoInventario).IsUnique();
            modelBuilder.Entity<Equipo>().HasIndex(e => e.IdCategoria);

            // Consultas de cruce de horarios por equipo (CA-09).
            modelBuilder.Entity<Reserva>().HasIndex(r => new { r.IdEquipo, r.Inicio, r.Fin });

            modelBuilder.Entity<Reserva>().HasIndex(r => r.IdUsuario, "IX_Reservas_IdUsuario");

            // CA-10: impide en la base de datos una segunda reserva activa del mismo estudiante,
            // incluso si dos solicitudes llegan a la vez.
            modelBuilder.Entity<Reserva>()
                .HasIndex(r => r.IdUsuario, "UX_Reservas_IdUsuario_Activa")
                .IsUnique()
                .HasFilter("[Estado] IN ('Pendiente', 'Aprobada')");
        }

        private static void ConfigurarColumnas(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Usuario>()
                .Property(u => u.Estado)
                .HasConversion<string>()
                .HasMaxLength(20);

            modelBuilder.Entity<Reserva>()
                .Property(r => r.Estado)
                .HasConversion<string>()
                .HasMaxLength(20);

            modelBuilder.Entity<Reserva>().Property(r => r.Inicio).HasPrecision(0);
            modelBuilder.Entity<Reserva>().Property(r => r.Fin).HasPrecision(0);

            // Con milisegundos para distinguir una devolución posterior a la hora de fin (CA-16).
            modelBuilder.Entity<Reserva>().Property(r => r.FechaDevolucionReal).HasPrecision(3);
            modelBuilder.Entity<Usuario>().Property(u => u.BloqueadoHasta).HasPrecision(3);
        }

        private static void ConfigurarRestricciones(ModelBuilder modelBuilder)
        {
            // ToTable solo declara restricciones; el nombre de cada tabla sigue siendo el del DbSet.
            modelBuilder.Entity<Usuario>().ToTable(tabla =>
            {
                tabla.HasCheckConstraint("CK_Usuarios_Estado", "[Estado] IN ('Activo', 'Penalizado')");
                tabla.HasCheckConstraint("CK_Usuarios_IntentosFallidos", "[IntentosFallidos] >= 0");
            });

            modelBuilder.Entity<Reserva>().ToTable(tabla =>
            {
                tabla.HasCheckConstraint("CK_Reservas_Periodo", "[Fin] > [Inicio]");
                tabla.HasCheckConstraint("CK_Reservas_Estado",
                    "[Estado] IN ('Pendiente', 'Aprobada', 'Rechazada', 'Devuelta')");
            });
        }

        private static void ConfigurarSemilla(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Rol>().HasData(
                new Rol { IdRol = 1, Nombre = NombresRol.Estudiante },
                new Rol { IdRol = 2, Nombre = NombresRol.Encargado });

            modelBuilder.Entity<Categoria>().HasData(
                new Categoria { IdCategoria = 1, Nombre = "Cómputo" },
                new Categoria { IdCategoria = 2, Nombre = "Electrónica" },
                new Categoria { IdCategoria = 3, Nombre = "Redes" });
        }
    }
}
