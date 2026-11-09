using LabTrack.Data;
using LabTrack.Models;
using LabTrack.Reglas;
using LabTrack.Services.Interfaces;
using LabTrack.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LabTrack.Services
{
    public sealed class AutenticacionService : IAutenticacionService
    {
        // Hash sin cuenta asociada: verificar contra él iguala el tiempo de respuesta
        // de un código inexistente con el de una contraseña errónea.
        private static readonly string HashSinCuenta =
            new PasswordHasher<Usuario>().HashPassword(new Usuario(), Guid.NewGuid().ToString());

        private readonly AppDbContext _context;
        private readonly IPasswordHasher<Usuario> _hasher;
        private readonly TimeProvider _tiempo;
        private readonly SeguridadOpciones _opciones;

        public AutenticacionService(
            AppDbContext context,
            IPasswordHasher<Usuario> hasher,
            TimeProvider tiempo,
            IOptions<SeguridadOpciones> opciones)
        {
            _context = context;
            _hasher = hasher;
            _tiempo = tiempo;
            _opciones = opciones.Value;
        }

        public async Task<ResultadoAutenticacion> AutenticarAsync(
            string? codigo, string? contrasena, CancellationToken cancellationToken = default)
        {
            codigo = codigo?.Trim();
            if (string.IsNullOrEmpty(codigo) || string.IsNullOrEmpty(contrasena))
            {
                return ResultadoAutenticacion.Invalida();
            }

            var usuario = await _context.Usuarios
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.Codigo == codigo, cancellationToken);

            if (usuario is null)
            {
                _hasher.VerifyHashedPassword(new Usuario(), HashSinCuenta, contrasena);
                return ResultadoAutenticacion.Invalida();
            }

            var ahora = _tiempo.GetLocalNow().DateTime;

            if (PoliticaBloqueo.EstaBloqueada(usuario.BloqueadoHasta, ahora))
            {
                return ResultadoAutenticacion.Bloqueada(usuario.BloqueadoHasta!.Value, ahora);
            }

            if (PoliticaBloqueo.BloqueoVencido(usuario.BloqueadoHasta, ahora))
            {
                usuario.BloqueadoHasta = null;
                usuario.IntentosFallidos = 0;
            }

            var verificacion = _hasher.VerifyHashedPassword(usuario, usuario.PasswordHash, contrasena);
            if (verificacion == PasswordVerificationResult.Failed)
            {
                return await RegistrarFalloAsync(usuario, ahora, cancellationToken);
            }

            if (verificacion == PasswordVerificationResult.SuccessRehashNeeded)
            {
                usuario.PasswordHash = _hasher.HashPassword(usuario, contrasena);
            }

            usuario.IntentosFallidos = 0;
            usuario.BloqueadoHasta = null;
            await _context.SaveChangesAsync(cancellationToken);

            return ResultadoAutenticacion.Exitosa(new UsuarioAutenticado
            {
                IdUsuario = usuario.IdUsuario,
                Codigo = usuario.Codigo,
                Nombre = usuario.Nombre,
                Rol = usuario.Rol.Nombre
            });
        }

        public Task<UsuarioBarraViewModel?> ObtenerResumenAsync(int idUsuario, CancellationToken cancellationToken = default)
            => _context.Usuarios
                .AsNoTracking()
                .Where(u => u.IdUsuario == idUsuario)
                .Select(u => new UsuarioBarraViewModel
                {
                    Nombre = u.Nombre,
                    Codigo = u.Codigo,
                    Rol = u.Rol.Nombre,
                    Estado = u.Estado
                })
                .FirstOrDefaultAsync(cancellationToken);

        private async Task<ResultadoAutenticacion> RegistrarFalloAsync(
            Usuario usuario, DateTime ahora, CancellationToken cancellationToken)
        {
            usuario.IntentosFallidos++;

            if (!PoliticaBloqueo.AlcanzoLimite(usuario.IntentosFallidos))
            {
                await _context.SaveChangesAsync(cancellationToken);
                return ResultadoAutenticacion.Invalida();
            }

            var bloqueadoHasta = ahora.AddMinutes(_opciones.MinutosBloqueo);
            usuario.BloqueadoHasta = bloqueadoHasta;
            await _context.SaveChangesAsync(cancellationToken);
            return ResultadoAutenticacion.Bloqueada(bloqueadoHasta, ahora);
        }
    }
}
