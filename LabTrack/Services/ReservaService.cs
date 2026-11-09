using LabTrack.Data;
using LabTrack.Models;
using LabTrack.Reglas;
using LabTrack.Services.Interfaces;
using LabTrack.ViewModels;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace LabTrack.Services
{
    public sealed class ReservaService : IReservaService
    {
        private const int TamanoPaginaPropias = 10;
        private const int ErrorClaveDuplicada = 2601;
        private const int ErrorRestriccionUnica = 2627;

        private readonly AppDbContext _context;
        private readonly TimeProvider _tiempo;

        public ReservaService(AppDbContext context, TimeProvider tiempo)
        {
            _context = context;
            _tiempo = tiempo;
        }

        private DateTime Ahora => _tiempo.GetLocalNow().DateTime;

        public async Task<CrearReservaViewModel?> ObtenerFormularioAsync(int idEquipo, CancellationToken cancellationToken = default)
        {
            var ahora = Ahora;
            var enCurso = EstadoPrestamo.EnCurso(ahora);

            var modelo = await _context.Equipos.AsNoTracking()
                .Where(e => e.IdEquipo == idEquipo)
                .Select(e => new CrearReservaViewModel
                {
                    IdEquipo = e.IdEquipo,
                    NombreEquipo = e.Nombre,
                    CodigoInventario = e.CodigoInventario,
                    Categoria = e.Categoria.Nombre,
                    EquipoDisponible = !e.Reservas.AsQueryable().Any(enCurso)
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (modelo is not null)
            {
                modelo.FechaMinima = DateOnly.FromDateTime(ahora);
            }

            return modelo;
        }

        public async Task<ResultadoOperacion> CrearAsync(
            int idUsuario, int idEquipo, DateOnly fecha, TimeOnly horaInicio, TimeOnly horaFin,
            CancellationToken cancellationToken = default)
        {
            var ahora = Ahora;
            var inicio = ValidacionHorario.Componer(fecha, horaInicio);
            var fin = ValidacionHorario.Componer(fecha, horaFin);

            var errorHorario = ValidacionHorario.Validar(inicio, fin, ahora);
            if (errorHorario is not null)
            {
                return ResultadoOperacion.Fallo(TipoResultado.Validacion, errorHorario);
            }

            await using var transaccion = await _context.Database.BeginTransactionAsync(cancellationToken);

            var equipo = await BloquearEquipoAsync(idEquipo, cancellationToken);
            if (equipo is null)
            {
                return ResultadoOperacion.Fallo(TipoResultado.NoEncontrado, Mensajes.EquipoNoEncontrado);
            }

            var esEstudiante = await _context.Usuarios.AsNoTracking()
                .AnyAsync(u => u.IdUsuario == idUsuario && u.Rol.Nombre == NombresRol.Estudiante, cancellationToken);
            if (!esEstudiante)
            {
                return ResultadoOperacion.Fallo(TipoResultado.Validacion, Mensajes.SoloEstudiantes);
            }

            var prestadoAhora = await _context.Reservas.AsNoTracking()
                .Where(r => r.IdEquipo == idEquipo)
                .AnyAsync(EstadoPrestamo.EnCurso(ahora), cancellationToken);
            if (prestadoAhora)
            {
                return ResultadoOperacion.Fallo(TipoResultado.Conflicto, Mensajes.EquipoNoDisponible);
            }

            if (await ExisteCruceConAprobadaAsync(idEquipo, inicio, fin, null, cancellationToken))
            {
                return ResultadoOperacion.Fallo(TipoResultado.Conflicto, Mensajes.ChoqueConAprobada);
            }

            var tieneReservaActiva = await _context.Reservas.AsNoTracking()
                .AnyAsync(r => r.IdUsuario == idUsuario
                    && (r.Estado == EstadoReserva.Pendiente || r.Estado == EstadoReserva.Aprobada), cancellationToken);
            if (tieneReservaActiva)
            {
                return ResultadoOperacion.Fallo(TipoResultado.Conflicto, Mensajes.UnaReservaALaVez);
            }

            _context.Reservas.Add(new Reserva
            {
                IdUsuario = idUsuario,
                IdEquipo = idEquipo,
                Inicio = inicio,
                Fin = fin,
                Estado = EstadoReserva.Pendiente
            });

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException ex) when (EsViolacionDeUnicidad(ex))
            {
                // El índice único filtrado detuvo dos solicitudes simultáneas del mismo estudiante.
                return ResultadoOperacion.Fallo(TipoResultado.Conflicto, Mensajes.UnaReservaALaVez);
            }

            await transaccion.CommitAsync(cancellationToken);

            return ResultadoOperacion.Ok(
                $"Solicitud enviada: {equipo.Nombre}, {Formato.FechaYHorario(inicio, fin)}. Queda Pendiente hasta que el encargado la apruebe.");
        }

        public async Task<MisReservasViewModel> ListarPropiasAsync(
            int idUsuario, EstadoReserva? estado, int pagina, CancellationToken cancellationToken = default)
        {
            var propias = _context.Reservas.AsNoTracking().Where(r => r.IdUsuario == idUsuario);

            var conteos = await propias
                .GroupBy(r => r.Estado)
                .Select(g => new { Estado = g.Key, Total = g.Count() })
                .ToDictionaryAsync(x => x.Estado, x => x.Total, cancellationToken);

            var filtradas = estado is null ? propias : propias.Where(r => r.Estado == estado);
            var total = estado is null ? conteos.Values.Sum() : conteos.GetValueOrDefault(estado.Value);
            pagina = PaginacionViewModel.Normalizar(pagina, total, TamanoPaginaPropias);

            var reservas = await filtradas
                .OrderByDescending(r => r.Inicio)
                .ThenByDescending(r => r.IdReserva)
                .Skip((pagina - 1) * TamanoPaginaPropias)
                .Take(TamanoPaginaPropias)
                .Select(r => new ReservaListadoViewModel
                {
                    IdReserva = r.IdReserva,
                    Equipo = r.Equipo.Nombre,
                    CodigoInventario = r.Equipo.CodigoInventario,
                    Inicio = r.Inicio,
                    Fin = r.Fin,
                    Estado = r.Estado,
                    MotivoRechazo = r.MotivoRechazo
                })
                .ToListAsync(cancellationToken);

            var penalizado = await _context.Usuarios.AsNoTracking()
                .AnyAsync(u => u.IdUsuario == idUsuario && u.Estado == EstadoUsuario.Penalizado, cancellationToken);

            return new MisReservasViewModel
            {
                Reservas = reservas,
                Estados = Enum.GetValues<EstadoReserva>()
                    .Select(e => new EstadoFiltroViewModel { Estado = e, Total = conteos.GetValueOrDefault(e) })
                    .ToList(),
                EstadoSeleccionado = estado,
                EstudiantePenalizado = penalizado,
                Paginacion = new PaginacionViewModel
                {
                    Pagina = pagina,
                    Total = total,
                    TamanoPagina = TamanoPaginaPropias,
                    Etiqueta = "reservas",
                    Parametros = new() { ["estado"] = estado?.ToString() }
                }
            };
        }

        // Serializa las operaciones sobre un equipo hasta que termine la transacción actual.
        private Task<Equipo?> BloquearEquipoAsync(int idEquipo, CancellationToken cancellationToken)
            => _context.Equipos
                .FromSqlInterpolated($"SELECT * FROM [Equipos] WITH (UPDLOCK, HOLDLOCK) WHERE [IdEquipo] = {idEquipo}")
                .AsNoTracking()
                .FirstOrDefaultAsync(cancellationToken);

        private Task<bool> ExisteCruceConAprobadaAsync(
            int idEquipo, DateTime inicio, DateTime fin, int? idReservaExcluida, CancellationToken cancellationToken)
            => _context.Reservas.AsNoTracking()
                .Where(r => r.IdEquipo == idEquipo && r.Estado == EstadoReserva.Aprobada)
                .Where(r => idReservaExcluida == null || r.IdReserva != idReservaExcluida)
                .AnyAsync(CruceHorarios.ConHorario(inicio, fin), cancellationToken);

        private static bool EsViolacionDeUnicidad(DbUpdateException ex)
            => ex.InnerException is SqlException { Number: ErrorClaveDuplicada or ErrorRestriccionUnica };
    }
}
