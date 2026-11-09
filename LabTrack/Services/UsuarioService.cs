using LabTrack.Data;
using LabTrack.Models;
using LabTrack.Reglas;
using LabTrack.Services.Interfaces;
using LabTrack.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace LabTrack.Services
{
    // Gestiona estudiantes y encargados. Los administradores quedan fuera a propósito:
    // así nadie puede dejar el sistema sin administrador desde esta pantalla.
    public sealed class UsuarioService : IUsuarioService
    {
        public const int TamanoPagina = 20;
        private const int ErrorClaveDuplicada = 2601;
        private const int ErrorRestriccionUnica = 2627;

        private readonly AppDbContext _context;
        private readonly IPasswordHasher<Usuario> _hasher;

        public UsuarioService(AppDbContext context, IPasswordHasher<Usuario> hasher)
        {
            _context = context;
            _hasher = hasher;
        }

        public async Task<GestionUsuariosViewModel> ListarAsync(
            string? busqueda, string? rol, int pagina, CancellationToken cancellationToken = default)
        {
            busqueda = string.IsNullOrWhiteSpace(busqueda) ? null : busqueda.Trim();
            rol = NombresRol.Asignables.Contains(rol) ? rol : null;

            var gestionables = _context.Usuarios.AsNoTracking()
                .Where(u => u.Rol.Nombre != NombresRol.Administrador);

            var conteos = await gestionables
                .GroupBy(u => u.Rol.Nombre)
                .Select(g => new { Rol = g.Key, Total = g.Count() })
                .ToDictionaryAsync(x => x.Rol, x => x.Total, cancellationToken);

            var usuarios = gestionables;
            if (rol is not null)
            {
                usuarios = usuarios.Where(u => u.Rol.Nombre == rol);
            }

            if (busqueda is not null)
            {
                usuarios = usuarios.Where(u => u.Nombre.Contains(busqueda) || u.Codigo.Contains(busqueda));
            }

            var total = await usuarios.CountAsync(cancellationToken);
            var deBaja = await usuarios.CountAsync(u => u.DeBaja, cancellationToken);
            pagina = PaginacionViewModel.Normalizar(pagina, total, TamanoPagina);

            var filas = await usuarios
                .OrderBy(u => u.Nombre)
                .ThenBy(u => u.IdUsuario)
                .Skip((pagina - 1) * TamanoPagina)
                .Take(TamanoPagina)
                .Select(u => new UsuarioGestionViewModel
                {
                    IdUsuario = u.IdUsuario,
                    Codigo = u.Codigo,
                    Nombre = u.Nombre,
                    Rol = u.Rol.Nombre,
                    Estado = u.Estado,
                    DeBaja = u.DeBaja,
                    TieneReservaActiva = u.Reservas.Any(r => r.Estado == EstadoReserva.Pendiente || r.Estado == EstadoReserva.Aprobada)
                })
                .ToListAsync(cancellationToken);

            return new GestionUsuariosViewModel
            {
                Usuarios = filas,
                RolSeleccionado = rol,
                Busqueda = busqueda,
                TotalEstudiantes = conteos.GetValueOrDefault(NombresRol.Estudiante),
                TotalEncargados = conteos.GetValueOrDefault(NombresRol.Encargado),
                TotalActivos = total - deBaja,
                TotalDeBaja = deBaja,
                Paginacion = new PaginacionViewModel
                {
                    Pagina = pagina,
                    Total = total,
                    TamanoPagina = TamanoPagina,
                    Etiqueta = "usuarios",
                    EtiquetaSingular = "usuario",
                    Parametros = new() { ["busqueda"] = busqueda, ["rol"] = rol }
                }
            };
        }

        public Task<FormularioUsuarioViewModel?> ObtenerFormularioEdicionAsync(int idUsuario, CancellationToken cancellationToken = default)
            => _context.Usuarios.AsNoTracking()
                .Where(u => u.IdUsuario == idUsuario && u.Rol.Nombre != NombresRol.Administrador)
                .Select(u => new FormularioUsuarioViewModel
                {
                    IdUsuario = u.IdUsuario,
                    Codigo = u.Codigo,
                    Nombre = u.Nombre,
                    Rol = u.Rol.Nombre
                })
                .FirstOrDefaultAsync(cancellationToken);

        public async Task<ResultadoOperacion> GuardarAsync(DatosUsuario datos, CancellationToken cancellationToken = default)
        {
            var codigo = datos.Codigo.Trim();
            var nombre = datos.Nombre.Trim();
            var esNuevo = datos.IdUsuario == 0;

            var titular = await _context.Usuarios.AsNoTracking()
                .Where(u => u.Codigo == codigo && u.IdUsuario != datos.IdUsuario)
                .Select(u => u.Nombre)
                .FirstOrDefaultAsync(cancellationToken);
            if (titular is not null)
            {
                return ResultadoOperacion.Fallo(TipoResultado.Conflicto, Mensajes.CodigoUsuarioRepetido, $"{codigo} pertenece a {titular}.");
            }

            Usuario? usuario;
            if (esNuevo)
            {
                if (!NombresRol.Asignables.Contains(datos.Rol))
                {
                    return ResultadoOperacion.Fallo(TipoResultado.Validacion, Mensajes.RolNoValido);
                }

                if (string.IsNullOrEmpty(datos.Contrasena))
                {
                    return ResultadoOperacion.Fallo(TipoResultado.Validacion, Mensajes.ContrasenaObligatoria);
                }

                var idRol = await _context.Roles
                    .Where(r => r.Nombre == datos.Rol)
                    .Select(r => r.IdRol)
                    .SingleAsync(cancellationToken);
                usuario = new Usuario { IdRol = idRol };
                _context.Usuarios.Add(usuario);
            }
            else
            {
                usuario = await BuscarGestionableAsync(datos.IdUsuario, cancellationToken);
                if (usuario is null)
                {
                    return ResultadoOperacion.Fallo(TipoResultado.NoEncontrado, Mensajes.UsuarioNoEncontrado);
                }
            }

            usuario.Codigo = codigo;
            usuario.Nombre = nombre;

            if (!string.IsNullOrEmpty(datos.Contrasena))
            {
                usuario.PasswordHash = _hasher.HashPassword(usuario, datos.Contrasena);

                // Una contraseña nueva también desbloquea la cuenta.
                usuario.IntentosFallidos = 0;
                usuario.BloqueadoHasta = null;
            }

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException ex) when (EsViolacionDeUnicidad(ex))
            {
                return ResultadoOperacion.Fallo(TipoResultado.Conflicto, Mensajes.CodigoUsuarioRepetido);
            }

            return ResultadoOperacion.Ok(esNuevo
                ? $"Usuario creado. {usuario.Nombre} ya puede iniciar sesión."
                : $"Usuario guardado: {usuario.Nombre}.");
        }

        public async Task<ResultadoOperacion> DarDeBajaAsync(int idUsuario, CancellationToken cancellationToken = default)
        {
            var usuario = await BuscarGestionableAsync(idUsuario, cancellationToken);
            if (usuario is null)
            {
                return ResultadoOperacion.Fallo(TipoResultado.NoEncontrado, Mensajes.UsuarioNoEncontrado);
            }

            if (usuario.DeBaja)
            {
                return ResultadoOperacion.Fallo(TipoResultado.EstadoInvalido, Mensajes.UsuarioYaDeBaja);
            }

            var equipoActivo = await _context.Reservas.AsNoTracking()
                .Where(r => r.IdUsuario == idUsuario
                    && (r.Estado == EstadoReserva.Pendiente || r.Estado == EstadoReserva.Aprobada))
                .Select(r => r.Equipo.Nombre)
                .FirstOrDefaultAsync(cancellationToken);
            if (equipoActivo is not null)
            {
                return ResultadoOperacion.Fallo(
                    TipoResultado.Conflicto,
                    Mensajes.NoSePuedeDarDeBaja,
                    $"{usuario.Nombre} tiene una reserva activa de «{equipoActivo}». Resuélvela primero.");
            }

            usuario.DeBaja = true;
            await _context.SaveChangesAsync(cancellationToken);

            return ResultadoOperacion.Ok($"{usuario.Nombre} se dio de baja y ya no puede iniciar sesión.");
        }

        public async Task<ResultadoOperacion> ReactivarAsync(int idUsuario, CancellationToken cancellationToken = default)
        {
            var usuario = await BuscarGestionableAsync(idUsuario, cancellationToken);
            if (usuario is null)
            {
                return ResultadoOperacion.Fallo(TipoResultado.NoEncontrado, Mensajes.UsuarioNoEncontrado);
            }

            if (!usuario.DeBaja)
            {
                return ResultadoOperacion.Fallo(TipoResultado.EstadoInvalido, Mensajes.UsuarioNoEstaDeBaja);
            }

            usuario.DeBaja = false;
            await _context.SaveChangesAsync(cancellationToken);

            return ResultadoOperacion.Ok($"{usuario.Nombre} se reactivó y ya puede iniciar sesión.");
        }

        private Task<Usuario?> BuscarGestionableAsync(int idUsuario, CancellationToken cancellationToken)
            => _context.Usuarios.FirstOrDefaultAsync(
                u => u.IdUsuario == idUsuario && u.Rol.Nombre != NombresRol.Administrador, cancellationToken);

        private static bool EsViolacionDeUnicidad(DbUpdateException ex)
            => ex.InnerException is SqlException { Number: ErrorClaveDuplicada or ErrorRestriccionUnica };
    }
}
