using LabTrack.Models;
using LabTrack.Reglas;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LabTrack.Data
{
    // Datos de demostración para Development. Las contraseñas vienen de la configuración
    // (sección Semilla, en User Secrets); nunca del repositorio. Se puede ejecutar en cada
    // arranque: solo agrega lo que falta y no repite ni deshace lo que el usuario haya hecho.
    public static class DbSeeder
    {
        // Un laboratorio tiene varias unidades de cada modelo; también permite simular la paginación.
        public const int UnidadesPorModelo = 5;

        private static readonly (string Nombre, string Codigo, string Categoria)[] EquiposDemostracion =
        [
            ("Laptop Dell Latitude 5440", "LT-0231", "Cómputo"),
            ("MacBook Air M2", "LT-0244", "Cómputo"),
            ("Laptop HP ProBook 450", "LT-0219", "Cómputo"),
            ("Laptop Lenovo ThinkPad T14", "LT-0260", "Cómputo"),
            ("PC Dell OptiPlex 7010", "PC-0101", "Cómputo"),
            ("Raspberry Pi 4 · kit", "RP-0520", "Cómputo"),
            ("Arduino Uno · kit", "AR-0411", "Cómputo"),
            ("Tablet Samsung Galaxy Tab S9", "TB-0087", "Cómputo"),
            ("Osciloscopio Rigol DS1054Z", "OS-0112", "Electrónica"),
            ("Multímetro Fluke 117", "MM-0308", "Electrónica"),
            ("Multímetro UNI-T UT61E", "MM-0312", "Electrónica"),
            ("Fuente de poder GW Instek", "FP-0190", "Electrónica"),
            ("Generador de funciones Siglent", "GF-0215", "Electrónica"),
            ("Analizador lógico Saleae", "AN-0077", "Electrónica"),
            ("Estación de soldadura Hakko FX-888D", "CA-0118", "Electrónica"),
            ("Protoboard · kit de prácticas", "PR-0340", "Electrónica"),
            ("Router Cisco ISR 4331", "RT-0045", "Redes"),
            ("Router MikroTik hEX", "RT-0052", "Redes"),
            ("Switch Cisco Catalyst 2960", "SW-0078", "Redes"),
            ("Switch TP-Link TL-SG108", "SW-0071", "Redes"),
            ("Access Point Ubiquiti U6", "AP-0030", "Redes"),
            ("Firewall FortiGate 40F", "FW-0003", "Redes"),
            ("Tester de cable Fluke MicroScanner", "TS-0012", "Redes"),
            ("Crimpadora RJ45 · kit", "CR-0009", "Redes")
        ];

        private static readonly (string Nombre, string Codigo, string Categoria)[] EquiposDeBajaDemostracion =
        [
            ("Proyector Epson X05", "PY-0007", "Cómputo"),
            ("Switch Cisco Catalyst 2950", "SW-0011", "Redes")
        ];

        private static readonly (string Codigo, string Nombre, EstadoUsuario Estado)[] EstudiantesDemostracion =
        [
            ("20240312", "Camila Paredes León", EstadoUsuario.Activo),
            ("20219980", "Andrés Mendoza Salas", EstadoUsuario.Activo),
            ("20235521", "Lucía Fernández Díaz", EstadoUsuario.Activo),
            ("20230876", "Mateo Rivas Castro", EstadoUsuario.Activo),
            ("20241203", "Sofía Gutiérrez Vega", EstadoUsuario.Activo),
            ("20226641", "Joaquín Ramos Peña", EstadoUsuario.Activo),
            ("20220654", "Renata Salazar Ortiz", EstadoUsuario.Activo),
            ("20231190", "Bruno Castillo Arias", EstadoUsuario.Activo),
            ("20228813", "Ximena Torres Rojas", EstadoUsuario.Activo),
            ("20237745", "Hugo Vargas Medina", EstadoUsuario.Activo),
            ("20216602", "Nicolás Flores Ramos", EstadoUsuario.Penalizado)
        ];

        public static async Task SembrarAsync(
            AppDbContext context,
            IConfiguration configuracion,
            IPasswordHasher<Usuario> hasher,
            TimeProvider tiempo,
            ILogger logger,
            CancellationToken cancellationToken = default)
        {
            await SembrarUsuarioAsync(context, configuracion.GetSection("Semilla:Encargado"),
                NombresRol.Encargado, hasher, logger, cancellationToken);

            await SembrarUsuarioAsync(context, configuracion.GetSection("Semilla:Administrador"),
                NombresRol.Administrador, hasher, logger, cancellationToken);

            foreach (var estudiante in configuracion.GetSection("Semilla:Estudiantes").GetChildren())
            {
                await SembrarUsuarioAsync(context, estudiante,
                    NombresRol.Estudiante, hasher, logger, cancellationToken);
            }

            await SembrarEstudiantesDemostracionAsync(context, configuracion, hasher, logger, cancellationToken);
            await SembrarEquiposAsync(context, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            await SembrarReservasAsync(
                context, configuracion["Semilla:Estudiantes:0:Codigo"], tiempo.GetLocalNow().DateTime, cancellationToken);
            await CompletarFechasDeSolicitudAsync(context, tiempo.GetLocalNow().DateTime, cancellationToken);
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

            var usuario = new Usuario
            {
                Codigo = codigo,
                Nombre = nombre,
                IdRol = await ObtenerIdRolAsync(context, nombreRol, cancellationToken)
            };
            usuario.PasswordHash = hasher.HashPassword(usuario, contrasena);
            context.Usuarios.Add(usuario);
        }

        private static async Task SembrarEstudiantesDemostracionAsync(
            AppDbContext context,
            IConfiguration configuracion,
            IPasswordHasher<Usuario> hasher,
            ILogger logger,
            CancellationToken cancellationToken)
        {
            var contrasena = configuracion["Semilla:ContrasenaDemostracion"];
            if (string.IsNullOrWhiteSpace(contrasena))
            {
                logger.LogWarning("Sin Semilla:ContrasenaDemostracion no se crean los estudiantes de simulación.");
                return;
            }

            var idRol = await ObtenerIdRolAsync(context, NombresRol.Estudiante, cancellationToken);
            var existentes = await context.Usuarios.Select(u => u.Codigo).ToListAsync(cancellationToken);

            foreach (var estudiante in EstudiantesDemostracion.Where(e => !existentes.Contains(e.Codigo)))
            {
                var usuario = new Usuario
                {
                    Codigo = estudiante.Codigo,
                    Nombre = estudiante.Nombre,
                    IdRol = idRol,
                    Estado = estudiante.Estado
                };
                usuario.PasswordHash = hasher.HashPassword(usuario, contrasena);
                context.Usuarios.Add(usuario);
            }
        }

        private static async Task SembrarEquiposAsync(AppDbContext context, CancellationToken cancellationToken)
        {
            var existentes = await context.Equipos.Select(e => e.CodigoInventario).ToListAsync(cancellationToken);
            var categorias = await context.Categorias
                .ToDictionaryAsync(c => c.Nombre, c => c.IdCategoria, cancellationToken);
            var nombresCategoria = categorias.ToDictionary(c => c.Value, c => c.Key);

            // Completa los equipos creados antes de que existiera el laboratorio.
            var sinLaboratorio = await context.Equipos.Where(e => e.Laboratorio == string.Empty).ToListAsync(cancellationToken);
            sinLaboratorio.ForEach(e => e.Laboratorio = LaboratorioDe(nombresCategoria[e.IdCategoria]));

            var unidades = EquiposDemostracion.SelectMany(e => Enumerable.Range(1, UnidadesPorModelo).Select(unidad =>
                unidad == 1 ? e : (Nombre: $"{e.Nombre} · U{unidad}", Codigo: $"{e.Codigo}-{unidad}", e.Categoria)));

            context.Equipos.AddRange(unidades
                .Where(e => !existentes.Contains(e.Codigo))
                .Select(e => new Equipo
                {
                    Nombre = e.Nombre,
                    CodigoInventario = e.Codigo,
                    IdCategoria = categorias[e.Categoria],
                    Laboratorio = LaboratorioDe(e.Categoria)
                }));

            context.Equipos.AddRange(EquiposDeBajaDemostracion
                .Where(e => !existentes.Contains(e.Codigo))
                .Select(e => new Equipo
                {
                    Nombre = e.Nombre,
                    CodigoInventario = e.Codigo,
                    IdCategoria = categorias[e.Categoria],
                    Laboratorio = LaboratorioDe(e.Categoria),
                    DeBaja = true
                }));
        }

        private static async Task SembrarReservasAsync(
            AppDbContext context, string? codigoEstudiantePrincipal, DateTime ahora, CancellationToken cancellationToken)
        {
            var usuarios = await context.Usuarios.ToDictionaryAsync(u => u.Codigo, u => u.IdUsuario, cancellationToken);
            var equipos = await context.Equipos.ToDictionaryAsync(e => e.CodigoInventario, e => e.IdEquipo, cancellationToken);

            var minutosDesdeSolicitud = 0;

            foreach (var plan in PlanDeReservas(codigoEstudiantePrincipal, ahora))
            {
                minutosDesdeSolicitud += 23;
                if (plan.Estudiante is null
                    || !usuarios.TryGetValue(plan.Estudiante, out var idUsuario)
                    || !equipos.TryGetValue(plan.Equipo, out var idEquipo))
                {
                    continue;
                }

                var yaExiste = await context.Reservas
                    .AnyAsync(r => r.IdUsuario == idUsuario && r.IdEquipo == idEquipo, cancellationToken);

                // Respeta el índice de una reserva activa por estudiante.
                var esActiva = plan.Estado is EstadoReserva.Pendiente or EstadoReserva.Aprobada;
                var tieneActiva = esActiva && await context.Reservas.AnyAsync(r => r.IdUsuario == idUsuario
                    && (r.Estado == EstadoReserva.Pendiente || r.Estado == EstadoReserva.Aprobada), cancellationToken);

                if (yaExiste || tieneActiva)
                {
                    continue;
                }

                context.Reservas.Add(new Reserva
                {
                    IdUsuario = idUsuario,
                    IdEquipo = idEquipo,
                    Inicio = plan.Inicio,
                    Fin = plan.Fin,
                    Estado = plan.Estado,
                    MotivoRechazo = plan.Motivo,
                    FechaDevolucionReal = plan.Devolucion,
                    FechaSolicitud = ahora.AddMinutes(-minutosDesdeSolicitud)
                });
            }
        }

        // Completa las reservas creadas antes de que existiera la fecha de solicitud.
        private static async Task CompletarFechasDeSolicitudAsync(AppDbContext context, DateTime ahora, CancellationToken cancellationToken)
        {
            var sinFecha = await context.Reservas.Where(r => r.FechaSolicitud == null).OrderBy(r => r.IdReserva).ToListAsync(cancellationToken);
            for (var i = 0; i < sinFecha.Count; i++)
            {
                sinFecha[i].FechaSolicitud = ahora.AddMinutes(-(i + 1) * 17);
            }
        }

        // Fechas relativas al arranque para simular todas las situaciones de los paneles.
        private static IEnumerable<(string? Estudiante, string Equipo, DateTime Inicio, DateTime Fin, EstadoReserva Estado, string? Motivo, DateTime? Devolucion)>
            PlanDeReservas(string? codigoEstudiantePrincipal, DateTime ahora)
        {
            var hora = ahora.Date.AddHours(ahora.Hour);
            var manana = ahora.Date.AddDays(1);
            var pasado = ahora.Date.AddDays(2);

            // Préstamos en curso: el equipo figura Prestado en el catálogo.
            yield return ("20230876", "RP-0520", hora.AddHours(-1), hora.AddHours(2), EstadoReserva.Aprobada, null, null);
            yield return ("20241203", "LT-0244", hora.AddHours(-1), hora.AddHours(3), EstadoReserva.Aprobada, null, null);
            yield return ("20226641", "FP-0190", hora.AddHours(-2), hora.AddHours(1), EstadoReserva.Aprobada, null, null);

            // Vencidas: al marcar la devolución el estudiante queda Penalizado.
            yield return ("20231190", "OS-0112", hora.AddHours(-4), hora.AddHours(-1), EstadoReserva.Aprobada, null, null);
            yield return ("20220654", "SW-0078", hora.AddHours(-6), hora.AddHours(-2), EstadoReserva.Aprobada, null, null);

            // Aprobada futura: el equipo sigue Disponible hasta que empiece el bloque.
            yield return ("20240312", "MM-0308", manana.AddHours(14), manana.AddHours(16), EstadoReserva.Aprobada, null, null);

            // Pendientes del panel; la de Hugo choca con la aprobada de Camila y no se podrá aprobar.
            yield return ("20219980", "LT-0231", manana.AddHours(10), manana.AddHours(12), EstadoReserva.Pendiente, null, null);
            yield return ("20235521", "RT-0045", manana.AddHours(8), manana.AddHours(10), EstadoReserva.Pendiente, null, null);
            yield return ("20228813", "AN-0077", pasado.AddHours(9), pasado.AddHours(11), EstadoReserva.Pendiente, null, null);
            yield return ("20237745", "MM-0308", manana.AddHours(15), manana.AddHours(17), EstadoReserva.Pendiente, null, null);

            // Historial con devolución tardía del estudiante penalizado.
            yield return ("20216602", "LT-0219", hora.AddDays(-3), hora.AddDays(-3).AddHours(2), EstadoReserva.Devuelta, null, hora.AddDays(-3).AddHours(3));

            // Historial visible en «Mis reservas» del estudiante principal.
            yield return (codigoEstudiantePrincipal, "OS-0112", hora.AddDays(-4), hora.AddDays(-4).AddHours(2), EstadoReserva.Rechazada, "Equipo en mantenimiento hasta el fin de semana.", null);
            yield return (codigoEstudiantePrincipal, "RT-0045", hora.AddDays(-7), hora.AddDays(-7).AddHours(2), EstadoReserva.Devuelta, null, hora.AddDays(-7).AddHours(1));
            yield return (codigoEstudiantePrincipal, "RP-0520", hora.AddDays(-12), hora.AddDays(-12).AddHours(2), EstadoReserva.Rechazada, "Solicitud fuera del horario de atención.", null);
        }

        private static string LaboratorioDe(string categoria) => categoria switch
        {
            "Electrónica" => "Lab. B-110",
            "Redes" => "Lab. C-301",
            _ => "Lab. A-204"
        };

        private static Task<int> ObtenerIdRolAsync(AppDbContext context, string nombreRol, CancellationToken cancellationToken)
            => context.Roles
                .Where(r => r.Nombre == nombreRol)
                .Select(r => r.IdRol)
                .SingleAsync(cancellationToken);
    }
}
