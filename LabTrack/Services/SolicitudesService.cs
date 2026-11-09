using LabTrack.Data;
using LabTrack.Models;
using LabTrack.Services.Interfaces;
using LabTrack.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace LabTrack.Services
{
    public sealed class SolicitudesService : ISolicitudesService
    {
        private readonly ApplicationDbContext _context;
        private readonly TimeProvider _clock;

        public SolicitudesService(ApplicationDbContext context, TimeProvider clock)
        {
            _context = context;
            _clock = clock;
        }
        private const int TamanoPagina = 20;

        private async Task ValidarEncargadoAsync(long id, CancellationToken cancellationToken)
        {
            if (!await _context.Usuarios.AsNoTracking().AnyAsync(
                    x => x.Id == id && x.Rol == RolUsuario.Encargado, cancellationToken))
            {
                throw new UnauthorizedAccessException("La operación requiere un encargado registrado.");
            }
        }

        private IQueryable<SolicitudPendienteViewModel> Pendientes()
            => _context.Reservas.AsNoTracking()
                .Where(x => x.Estado == EstadoReserva.Pendiente)
                .Select(x => new SolicitudPendienteViewModel
                {
                    Id = x.Id,
                    Estudiante = x.Estudiante.Nombre,
                    CodigoEstudiante = x.Estudiante.Codigo,
                    Equipo = x.Equipo.Nombre,
                    CodigoInventario = x.Equipo.CodigoInventario,
                    Inicio = x.Inicio,
                    Fin = x.Fin,
                    EquipoPrestado = x.Equipo.Reservas.Any(r => r.Estado == EstadoReserva.Aprobada
                        && r.Inicio < x.Fin && x.Inicio < r.Fin)
                });

        public async Task<SolicitudesListadoViewModel> ListarPendientesAsync(
            long encargadoId, int pagina, string? busqueda = null, CancellationToken cancellationToken = default)
        {
            await ValidarEncargadoAsync(encargadoId, cancellationToken);
            busqueda = busqueda?.Trim() ?? string.Empty;
            var query = Pendientes();
            if (busqueda.Length > 0)
                query = query.Where(x => x.Estudiante.Contains(busqueda) || x.CodigoEstudiante.Contains(busqueda)
                    || x.Equipo.Contains(busqueda) || x.CodigoInventario.Contains(busqueda));

            var total = await query.CountAsync(cancellationToken);
            var ultimaPagina = Math.Max(1, (int)Math.Ceiling(total / (double)TamanoPagina));
            pagina = Math.Clamp(pagina, 1, ultimaPagina);
            var solicitudes = await query.OrderBy(x => x.Inicio).ThenBy(x => x.Id)
                .Skip((pagina - 1) * TamanoPagina).Take(TamanoPagina).ToListAsync(cancellationToken);
            return new SolicitudesListadoViewModel
            {
                Solicitudes = solicitudes, Pagina = pagina, Total = total,
                TamanoPagina = TamanoPagina, Busqueda = busqueda
            };
        }

        public async Task<SolicitudPendienteViewModel?> ObtenerPendienteAsync(
            long encargadoId, long id, CancellationToken cancellationToken = default)
        {
            await ValidarEncargadoAsync(encargadoId, cancellationToken);
            return await Pendientes().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        }

        public Task<EstadoResolucion> AprobarAsync(long encargadoId, long id, CancellationToken cancellationToken = default)
            => ResolverAsync(encargadoId, id, null, cancellationToken);

        public async Task<EstadoResolucion> RechazarAsync(
            long encargadoId, long id, string motivo, CancellationToken cancellationToken = default)
        {
            await ValidarEncargadoAsync(encargadoId, cancellationToken);
            motivo = motivo?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(motivo) || motivo.Length > 255)
                return EstadoResolucion.MotivoInvalido;

            return await ResolverAsync(encargadoId, id, motivo, cancellationToken);
        }

        private async Task<EstadoResolucion> ResolverAsync(
            long encargadoId, long id, string? motivo, CancellationToken cancellationToken)
        {
            await ValidarEncargadoAsync(encargadoId, cancellationToken);
            var equipoId = await _context.Reservas.AsNoTracking().Where(x => x.Id == id)
                .Select(x => (long?)x.EquipoId).SingleOrDefaultAsync(cancellationToken);
            if (equipoId is null)
                return EstadoResolucion.NoEncontrada;

            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            // Serializa las decisiones del mismo equipo, incluso desde procesos distintos.
            // Toda futura operación que cambie la disponibilidad debe tomar este mismo bloqueo.
            var equipo = await _context.Equipos
                .FromSqlInterpolated($"SELECT * FROM [equipos] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = {equipoId.Value}")
                .AsNoTracking().SingleOrDefaultAsync(cancellationToken);
            if (equipo is null)
                return EstadoResolucion.NoEncontrada;

            var reserva = await _context.Reservas.AsNoTracking()
                .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (reserva is null)
                return EstadoResolucion.NoEncontrada;
            if (reserva.Estado != EstadoReserva.Pendiente)
                return EstadoResolucion.YaResuelta;

            if (!await _context.Usuarios.AsNoTracking().AnyAsync(
                    x => x.Id == reserva.EstudianteId && x.Rol == RolUsuario.Estudiante, cancellationToken))
                return EstadoResolucion.EstudianteInvalido;

            if (motivo is null && await _context.Reservas.AsNoTracking().AnyAsync(
                    x => x.EquipoId == reserva.EquipoId && x.Estado == EstadoReserva.Aprobada
                        && x.Inicio < reserva.Fin && reserva.Inicio < x.Fin, cancellationToken))
            {
                return EstadoResolucion.ConflictoHorario;
            }

            var estado = motivo is null ? EstadoReserva.Aprobada : EstadoReserva.Rechazada;
            var ahora = _clock.GetUtcNow().UtcDateTime;
            // El filtro protege también de una resolución repetida desde un formulario antiguo.
            var actualizadas = await _context.Reservas
                .Where(x => x.Id == id && x.Estado == EstadoReserva.Pendiente)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.Estado, estado)
                    .SetProperty(x => x.MotivoRechazo, motivo)
                    .SetProperty(x => x.ResueltaPor, encargadoId)
                    .SetProperty(x => x.ResueltaEn, ahora), cancellationToken);
            if (actualizadas != 1)
                return EstadoResolucion.YaResuelta;

            await transaction.CommitAsync(cancellationToken);
            return EstadoResolucion.Completada;
        }
    }
}
