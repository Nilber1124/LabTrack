using LabTrack.Models;
using Microsoft.EntityFrameworkCore;

namespace LabTrack.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Categoria> Categorias { get; set; }
        public DbSet<Equipo> Equipos { get; set; }
        public DbSet<Reserva> Reservas { get; set; }
        public DbSet<Penalizacion> Penalizaciones { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Las claves, tablas, longitudes y tipos simples se declaran en los modelos.
            // Relación Categoria -> Equipos.
            modelBuilder.Entity<Equipo>()
                .HasOne(e => e.Categoria)
                .WithMany(c => c.Equipos)
                .HasForeignKey(e => e.CategoriaId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            // Relación Usuario -> Reservas como estudiante.
            modelBuilder.Entity<Reserva>()
                .HasOne(r => r.Estudiante)
                .WithMany(u => u.ReservasComoEstudiante)
                .HasForeignKey(r => r.EstudianteId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            // Relación opcional Usuario -> Reservas como encargado que resuelve.
            modelBuilder.Entity<Reserva>()
                .HasOne(r => r.EncargadoResolutor)
                .WithMany(u => u.ReservasResueltas)
                .HasForeignKey(r => r.ResueltaPor)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            // Relación Equipo -> Reservas.
            modelBuilder.Entity<Reserva>()
                .HasOne(r => r.Equipo)
                .WithMany(e => e.Reservas)
                .HasForeignKey(r => r.EquipoId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            // Relación Reserva -> Penalizacion (uno a cero o uno).
            modelBuilder.Entity<Penalizacion>()
                .HasOne(p => p.Reserva)
                .WithOne(r => r.Penalizacion)
                .HasForeignKey<Penalizacion>(p => p.ReservaId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            // Índices únicos y de búsqueda.
            modelBuilder.Entity<Usuario>().HasIndex(u => u.Codigo).IsUnique();
            modelBuilder.Entity<Categoria>().HasIndex(c => c.Nombre).IsUnique();
            modelBuilder.Entity<Equipo>().HasIndex(e => e.CodigoInventario).IsUnique();
            modelBuilder.Entity<Equipo>().HasIndex(e => e.CategoriaId);
            modelBuilder.Entity<Penalizacion>().HasIndex(p => p.ReservaId).IsUnique();
            modelBuilder.Entity<Reserva>()
                .HasIndex(r => new { r.EquipoId, r.Estado, r.Inicio, r.Fin });

            // Conversión de enums a texto y valores predeterminados de SQL Server.
            modelBuilder.Entity<Usuario>()
                .Property(u => u.Rol)
                .HasConversion<string>()
                .HasMaxLength(20)
                .HasDefaultValue(RolUsuario.Estudiante);

            modelBuilder.Entity<Usuario>()
                .Property(u => u.Estado)
                .HasConversion<string>()
                .HasMaxLength(20)
                .HasDefaultValue(EstadoUsuario.Activo);

            modelBuilder.Entity<Usuario>()
                .Property(u => u.IntentosFallidos)
                .HasDefaultValue((short)0);

            modelBuilder.Entity<Reserva>()
                .Property(r => r.Estado)
                .HasConversion<string>()
                .HasMaxLength(20)
                .HasDefaultValue(EstadoReserva.Pendiente);

            modelBuilder.Entity<Reserva>()
                .Property(r => r.CreadaEn)
                .HasDefaultValueSql("SYSUTCDATETIME()");

            modelBuilder.Entity<Penalizacion>()
                .Property(p => p.CreadaEn)
                .HasDefaultValueSql("SYSUTCDATETIME()");

            // Restricciones que deben cumplirse también fuera de los formularios MVC.
            modelBuilder.Entity<Usuario>().ToTable(table =>
            {
                table.HasCheckConstraint("CK_usuarios_Rol", "[Rol] IN ('Estudiante', 'Encargado')");
                table.HasCheckConstraint("CK_usuarios_Estado", "[Estado] IN ('Activo', 'Penalizado')");
                table.HasCheckConstraint("CK_usuarios_IntentosFallidos", "[IntentosFallidos] >= 0");
            });

            modelBuilder.Entity<Reserva>().ToTable(table =>
            {
                table.HasCheckConstraint("CK_reservas_Periodo", "[Fin] > [Inicio]");
                table.HasCheckConstraint("CK_reservas_Estado",
                    "[Estado] IN ('Pendiente', 'Aprobada', 'Rechazada', 'Devuelta')");
            });

            base.OnModelCreating(modelBuilder);
        }
    }
}
