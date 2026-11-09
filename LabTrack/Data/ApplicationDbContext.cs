using LabTrack.Models;
using Microsoft.EntityFrameworkCore;

namespace LabTrack.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Equipo> Equipos => Set<Equipo>();
    public DbSet<Reserva> Reservas => Set<Reserva>();
    public DbSet<Penalizacion> Penalizaciones => Set<Penalizacion>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.ToTable("usuarios", table =>
            {
                table.HasCheckConstraint("CK_usuarios_Rol", "[Rol] IN ('Estudiante', 'Encargado')");
                table.HasCheckConstraint("CK_usuarios_Estado", "[Estado] IN ('Activo', 'Penalizado')");
                table.HasCheckConstraint("CK_usuarios_IntentosFallidos", "[IntentosFallidos] >= 0");
            });
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).UseIdentityColumn();
            entity.Property(x => x.Codigo).HasMaxLength(20).IsRequired();
            entity.HasIndex(x => x.Codigo).IsUnique();
            entity.Property(x => x.Nombre).HasMaxLength(120).IsRequired();
            entity.Property(x => x.PasswordHash).HasMaxLength(255).IsRequired();
            entity.Property(x => x.Rol).HasConversion<string>().HasMaxLength(20)
                .HasDefaultValue(RolUsuario.Estudiante).IsRequired();
            entity.Property(x => x.Estado).HasConversion<string>().HasMaxLength(20)
                .HasDefaultValue(EstadoUsuario.Activo).IsRequired();
            entity.Property(x => x.IntentosFallidos).HasDefaultValue((short)0);
            entity.Property(x => x.BloqueadoHasta).HasColumnType("datetimeoffset");
        });

        modelBuilder.Entity<Categoria>(entity =>
        {
            entity.ToTable("categorias");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).UseIdentityColumn();
            entity.Property(x => x.Nombre).HasMaxLength(50).IsRequired();
            entity.HasIndex(x => x.Nombre).IsUnique();
        });

        modelBuilder.Entity<Equipo>(entity =>
        {
            entity.ToTable("equipos");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).UseIdentityColumn();
            entity.Property(x => x.CodigoInventario).HasMaxLength(30).IsRequired();
            entity.HasIndex(x => x.CodigoInventario).IsUnique();
            entity.Property(x => x.Nombre).HasMaxLength(120).IsRequired();
            entity.HasIndex(x => x.CategoriaId);
            entity.HasOne(x => x.Categoria).WithMany(x => x.Equipos)
                .HasForeignKey(x => x.CategoriaId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Reserva>(entity =>
        {
            entity.ToTable("reservas", table =>
            {
                table.HasCheckConstraint("CK_reservas_Periodo", "[Fin] > [Inicio]");
                table.HasCheckConstraint("CK_reservas_Estado",
                    "[Estado] IN ('Pendiente', 'Aprobada', 'Rechazada', 'Devuelta')");
            });
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).UseIdentityColumn();
            entity.Property(x => x.Inicio).HasColumnType("datetimeoffset").IsRequired();
            entity.Property(x => x.Fin).HasColumnType("datetimeoffset").IsRequired();
            entity.Property(x => x.Estado).HasConversion<string>().HasMaxLength(20)
                .HasDefaultValue(EstadoReserva.Pendiente).IsRequired();
            entity.Property(x => x.MotivoRechazo).HasMaxLength(255);
            entity.Property(x => x.ResueltaEn).HasColumnType("datetime2");
            entity.Property(x => x.DevueltaEn).HasColumnType("datetime2");
            entity.Property(x => x.CreadaEn).HasColumnType("datetime2")
                .HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasIndex(x => new { x.EquipoId, x.Estado, x.Inicio, x.Fin });
            entity.HasOne(x => x.Estudiante).WithMany(x => x.ReservasComoEstudiante)
                .HasForeignKey(x => x.EstudianteId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.EncargadoResolutor).WithMany(x => x.ReservasResueltas)
                .HasForeignKey(x => x.ResueltaPor).IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Equipo).WithMany(x => x.Reservas)
                .HasForeignKey(x => x.EquipoId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Penalizacion>(entity =>
        {
            entity.ToTable("penalizaciones");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).UseIdentityColumn();
            entity.HasIndex(x => x.ReservaId).IsUnique();
            entity.Property(x => x.CreadaEn).HasColumnType("datetime2")
                .HasDefaultValueSql("SYSUTCDATETIME()");
            entity.Property(x => x.LevantadaEn).HasColumnName("Levantada_En")
                .HasColumnType("datetime2");
            entity.HasOne(x => x.Reserva).WithOne(x => x.Penalizacion)
                .HasForeignKey<Penalizacion>(x => x.ReservaId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
