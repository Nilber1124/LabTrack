using LabTrack.Models;
using LabTrack.Reglas;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LabTrack.Data
{
    // Datos de demostración para Development. Las credenciales vienen de la configuración
    // (sección Semilla, en User Secrets); nunca del repositorio.
    public static class DbSeeder
    {
        private static readonly (string Nombre, string Codigo, string Categoria)[] EquiposDemostracion =
        [
            ("Laptop Dell Latitude 5440", "LT-0231", "Cómputo"),
            ("MacBook Air M2", "LT-0244", "Cómputo"),
            ("Raspberry Pi 4 · kit", "RP-0520", "Cómputo"),
            ("Osciloscopio Rigol DS1054Z", "OS-0112", "Electrónica"),
            ("Multímetro Fluke 117", "MM-0308", "Electrónica"),
            ("Fuente de poder GW Instek", "FP-0190", "Electrónica"),
            ("Router Cisco ISR 4331", "RT-0045", "Redes"),
            ("Switch Cisco Catalyst 2960", "SW-0078", "Redes")
        ];

        public static async Task SembrarAsync(
            AppDbContext context,
            IConfiguration configuracion,
            IPasswordHasher<Usuario> hasher,
            ILogger logger,
            CancellationToken cancellationToken = default)
        {
            await SembrarUsuarioAsync(context, configuracion.GetSection("Semilla:Encargado"),
                NombresRol.Encargado, hasher, logger, cancellationToken);

            foreach (var estudiante in configuracion.GetSection("Semilla:Estudiantes").GetChildren())
            {
                await SembrarUsuarioAsync(context, estudiante,
                    NombresRol.Estudiante, hasher, logger, cancellationToken);
            }

            await SembrarEquiposAsync(context, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
        }

        private static async Task SembrarUsuarioAsync(
            AppDbContext context,
            IConfiguration datos,
            string nombreRol,
            IPasswordHasher<Usuario> hasher,
            ILogger logger,
            CancellationToken cancellationToken)
        {
            var codigo = datos["Codigo"];
            var nombre = datos["Nombre"];
            var contrasena = datos["Contrasena"];

            if (string.IsNullOrWhiteSpace(codigo) || string.IsNullOrWhiteSpace(nombre) || string.IsNullOrWhiteSpace(contrasena))
            {
                logger.LogWarning("Falta un usuario de demostración con rol {Rol} en la sección Semilla.", nombreRol);
                return;
            }

            if (await context.Usuarios.AnyAsync(u => u.Codigo == codigo, cancellationToken))
            {
                return;
            }

            var idRol = await context.Roles
                .Where(r => r.Nombre == nombreRol)
                .Select(r => r.IdRol)
                .SingleAsync(cancellationToken);

            var usuario = new Usuario { Codigo = codigo, Nombre = nombre, IdRol = idRol };
            usuario.PasswordHash = hasher.HashPassword(usuario, contrasena);
            context.Usuarios.Add(usuario);
        }

        private static async Task SembrarEquiposAsync(AppDbContext context, CancellationToken cancellationToken)
        {
            if (await context.Equipos.AnyAsync(cancellationToken))
            {
                return;
            }

            var categorias = await context.Categorias
                .ToDictionaryAsync(c => c.Nombre, c => c.IdCategoria, cancellationToken);

            context.Equipos.AddRange(EquiposDemostracion.Select(e => new Equipo
            {
                Nombre = e.Nombre,
                CodigoInventario = e.Codigo,
                IdCategoria = categorias[e.Categoria]
            }));
        }
    }
}
