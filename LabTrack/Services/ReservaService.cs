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
        private const int TamanoPaginaSolicitudes = 20;
        private const int LongitudMaximaMotivo = 200;
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

        public async Task<CrearReservaViewModel?> ObtenerFormularioAsync(int idUsuario, int idEquipo, DateOnly? fecha = null, CancellationToken cancellationToken = default)
        {
            var ahora = Ahora;
            var enCurso = EstadoPrestamo.EnCurso(ahora);

            var modelo = await _context.Equipos.AsNoTracking()
                .Where(e => e.IdEquipo == idEquipo && !e.DeBaja)
                .Select(e => new CrearReservaViewModel
                {
                    IdEquipo = e.IdEquipo,
                    NombreEquipo = e.Nombre,
                    CodigoInventario = e.CodigoInventario,
                    Categoria = e.Categoria.Nombre,
                    Laboratorio = e.Laboratorio,
                    FotoUrl = e.FotoUrl,
                    EquipoDisponible = !e.Reservas.AsQueryable().Any(enCurso)
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (modelo is not null)
            {
                modelo.FechaMinima = DateOnly.FromDateTime(ahora);
                await CompletarPenalizacionAsync(modelo, idUsuario, cancellationToken);

                modelo.FechaOcupacion = fecha ?? modelo.FechaMinima;
                var bloques = await AprobadasDelDiaAsync(idEquipo, modelo.FechaOcupacion, cancellationToken);
                modelo.Ocupacion = bloques
                    .Select(b => new BloqueOcupadoViewModel { Inicio = b.Inicio, Fin = b.Fin })
                    .ToList();
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
            if (equipo is null || equipo.DeBaja)
            {
                return ResultadoOperacion.Fallo(TipoResultado.NoEncontrado, Mensajes.EquipoNoEncontrado);
            }

            var estadoEstudiante = await _context.Usuarios.AsNoTracking()
                .Where(u => u.IdUsuario == idUsuario && u.Rol.Nombre == NombresRol.Estudiante)
                .Select(u => (EstadoUsuario?)u.Estado)
                .FirstOrDefaultAsync(cancellationToken);
            if (estadoEstudiante is null)
            {
                return ResultadoOperacion.Fallo(TipoResultado.Validacion, Mensajes.SoloEstudiantes);
            }

            if (estadoEstudiante == EstadoUsuario.Penalizado)
            {
                return ResultadoOperacion.Fallo(TipoResultado.Conflicto, Mensajes.EstudiantePenalizado);
            }

            var prestadoAhora = await _context.Reservas.AsNoTracking()
                .Where(r => r.IdEquipo == idEquipo)
                .AnyAsync(EstadoPrestamo.EnCurso(ahora), cancellationToken);
            if (prestadoAhora)
            {
                return ResultadoOperacion.Fallo(TipoResultado.Conflicto, Mensajes.EquipoNoDisponible);
            }

            var cruce = await BuscarCruceConAprobadaAsync(idEquipo, inicio, fin, null, cancellationToken);
            if (cruce is not null)
            {
                return await ChoqueConSugerenciaAsync(idEquipo, fecha, cruce, fin - inicio, cancellationToken);
            }

            var activa = await _context.Reservas.AsNoTracking()
                .Where(r => r.IdUsuario == idUsuario
                    && (r.Estado == EstadoReserva.Pendiente || r.Estado == EstadoReserva.Aprobada))
                .Select(r => new { Equipo = r.Equipo.Nombre, r.Inicio, r.Fin })
                .FirstOrDefaultAsync(cancellationToken);
            if (activa is not null)
            {
                return ResultadoOperacion.Fallo(
                    TipoResultado.Conflicto,
                    Mensajes.UnaReservaALaVez,
                    $"Ya tienes «{activa.Equipo}» el {Formato.FechaYHorario(activa.Inicio, activa.Fin)}.");
            }

            _context.Reservas.Add(new Reserva
            {
                IdUsuario = idUsuario,
                IdEquipo = idEquipo,
                Inicio = inicio,
                Fin = fin,
                Estado = EstadoReserva.Pendiente,
                FechaSolicitud = ahora
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
                    EtiquetaSingular = "reserva",
                    Parametros = new() { ["estado"] = estado?.ToString() }
                }
            };
        }

        public async Task<SolicitudesViewModel> ListarPendientesAsync(int pagina, CancellationToken cancellationToken = default)
        {
            var pendientes = Pendientes();
            var total = await pendientes.CountAsync(cancellationToken);
            pagina = PaginacionViewModel.Normalizar(pagina, total, TamanoPaginaSolicitudes);

            var solicitudes = await pendientes
                .OrderBy(s => s.Inicio)
                .ThenBy(s => s.IdReserva)
                .Skip((pagina - 1) * TamanoPaginaSolicitudes)
                .Take(TamanoPaginaSolicitudes)
                .ToListAsync(cancellationToken);

            var ahora = Ahora;
            foreach (var solicitud in solicitudes.Where(s => s.FechaSolicitud is not null))
            {
                solicitud.Recibida = Formato.Antiguedad(ahora - solicitud.FechaSolicitud!.Value);
            }

            var masAntigua = await pendientes.MinAsync(s => s.FechaSolicitud, cancellationToken);

            return new SolicitudesViewModel
            {
                Solicitudes = solicitudes,
                AntiguedadMayor = masAntigua is null ? null : Formato.Antiguedad(ahora - masAntigua.Value),
                Paginacion = new PaginacionViewModel
                {
                    Pagina = pagina,
                    Total = total,
                    TamanoPagina = TamanoPaginaSolicitudes,
                    Etiqueta = "solicitudes",
                    EtiquetaSingular = "solicitud"
                }
            };
        }

        public Task<SolicitudPendienteViewModel?> ObtenerPendienteAsync(int idReserva, CancellationToken cancellationToken = default)
            => Pendientes().FirstOrDefaultAsync(s => s.IdReserva == idReserva, cancellationToken);

        public async Task<ResultadoOperacion> AprobarAsync(int idReserva, CancellationToken cancellationToken = default)
        {
            var idEquipo = await _context.Reservas.AsNoTracking()
                .Where(r => r.IdReserva == idReserva)
                .Select(r => (int?)r.IdEquipo)
                .FirstOrDefaultAsync(cancellationToken);
            if (idEquipo is null)
            {
                return ResultadoOperacion.Fallo(TipoResultado.NoEncontrado, Mensajes.ReservaNoEncontrada);
            }

            await using var transaccion = await _context.Database.BeginTransactionAsync(cancellationToken);

            // Con el equipo bloqueado, una aprobación simultánea espera y después ve esta reserva.
            var equipo = await BloquearEquipoAsync(idEquipo.Value, cancellationToken);
            var reserva = await _context.Reservas.FirstOrDefaultAsync(r => r.IdReserva == idReserva, cancellationToken);
            if (equipo is null || reserva is null)
            {
                return ResultadoOperacion.Fallo(TipoResultado.NoEncontrado, Mensajes.ReservaNoEncontrada);
            }

            if (reserva.Estado != EstadoReserva.Pendiente)
            {
                return ResultadoOperacion.Fallo(TipoResultado.EstadoInvalido, Mensajes.SolicitudYaResuelta);
            }

            if (equipo.DeBaja)
            {
                return ResultadoOperacion.Fallo(TipoResultado.Conflicto, Mensajes.EquipoNoDisponible);
            }

            if (await BuscarCruceConAprobadaAsync(reserva.IdEquipo, reserva.Inicio, reserva.Fin, reserva.IdReserva, cancellationToken) is not null)
            {
                return ResultadoOperacion.Fallo(TipoResultado.Conflicto, Mensajes.ChoqueConAprobada);
            }

            reserva.Estado = EstadoReserva.Aprobada;

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                return ResultadoOperacion.Fallo(TipoResultado.Conflicto, Mensajes.ConflictoConcurrencia);
            }

            await transaccion.CommitAsync(cancellationToken);

            return ResultadoOperacion.Ok(
                $"Reserva aprobada. {equipo.CodigoInventario} figura como Prestado el {Formato.FechaYHorario(reserva.Inicio, reserva.Fin)}.");
        }

        public async Task<ResultadoOperacion> RechazarAsync(int idReserva, string? motivo, CancellationToken cancellationToken = default)
        {
            motivo = motivo?.Trim();
            if (string.IsNullOrEmpty(motivo))
            {
                return ResultadoOperacion.Fallo(TipoResultado.Validacion, Mensajes.MotivoObligatorio);
            }

            if (motivo.Length > LongitudMaximaMotivo)
            {
                return ResultadoOperacion.Fallo(TipoResultado.Validacion, Mensajes.MotivoExcedeLongitud);
            }

            var reserva = await _context.Reservas.FirstOrDefaultAsync(r => r.IdReserva == idReserva, cancellationToken);
            if (reserva is null)
            {
                return ResultadoOperacion.Fallo(TipoResultado.NoEncontrado, Mensajes.ReservaNoEncontrada);
            }

            if (reserva.Estado != EstadoReserva.Pendiente)
            {
                return ResultadoOperacion.Fallo(TipoResultado.EstadoInvalido, Mensajes.SolicitudYaResuelta);
            }

            reserva.Estado = EstadoReserva.Rechazada;
            reserva.MotivoRechazo = motivo;

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                return ResultadoOperacion.Fallo(TipoResultado.Conflicto, Mensajes.ConflictoConcurrencia);
            }

            return ResultadoOperacion.Ok("Solicitud rechazada. El estudiante verá el motivo en Mis reservas.");
        }

        private IQueryable<SolicitudPendienteViewModel> Pendientes()
            => _context.Reservas.AsNoTracking()
                .Where(r => r.Estado == EstadoReserva.Pendiente)
                .Select(r => new SolicitudPendienteViewModel
                {
                    IdReserva = r.IdReserva,
                    Estudiante = r.Usuario.Nombre,
                    CodigoEstudiante = r.Usuario.Codigo,
                    Equipo = r.Equipo.Nombre,
                    CodigoInventario = r.Equipo.CodigoInventario,
                    Inicio = r.Inicio,
                    Fin = r.Fin,
                    FechaSolicitud = r.FechaSolicitud
                });

        private async Task CompletarPenalizacionAsync(
            CrearReservaViewModel modelo, int idUsuario, CancellationToken cancellationToken)
        {
            modelo.EstudiantePenalizado = await _context.Usuarios.AsNoTracking()
                .AnyAsync(u => u.IdUsuario == idUsuario && u.Estado == EstadoUsuario.Penalizado, cancellationToken);
            if (!modelo.EstudiantePenalizado)
            {
                return;
            }

            var tardia = await _context.Reservas.AsNoTracking()
                .Where(r => r.IdUsuario == idUsuario && r.Estado == EstadoReserva.Devuelta && r.FechaDevolucionReal > r.Fin)
                .OrderByDescending(r => r.FechaDevolucionReal)
                .Select(r => new { Equipo = r.Equipo.Nombre, r.Fin, Devolucion = r.FechaDevolucionReal!.Value })
                .FirstOrDefaultAsync(cancellationToken);

            if (tardia is not null)
            {
                modelo.MotivoPenalizacion =
                    $"Devolviste «{tardia.Equipo}» {Formato.Duracion(tardia.Devolucion - tardia.Fin)} después de la hora de fin ({tardia.Devolucion:dd/MM/yyyy}).";
            }
        }

        // Serializa las operaciones sobre un equipo hasta que termine la transacción actual.
        private Task<Equipo?> BloquearEquipoAsync(int idEquipo, CancellationToken cancellationToken)
            => _context.Equipos
                .FromSqlInterpolated($"SELECT * FROM [Equipos] WITH (UPDLOCK, HOLDLOCK) WHERE [IdEquipo] = {idEquipo}")
                .AsNoTracking()
                .FirstOrDefaultAsync(cancellationToken);

        private Task<Reserva?> BuscarCruceConAprobadaAsync(
            int idEquipo, DateTime inicio, DateTime fin, int? idReservaExcluida, CancellationToken cancellationToken)
            => _context.Reservas.AsNoTracking()
                .Where(r => r.IdEquipo == idEquipo && r.Estado == EstadoReserva.Aprobada)
                .Where(r => idReservaExcluida == null || r.IdReserva != idReservaExcluida)
                .Where(CruceHorarios.ConHorario(inicio, fin))
                .OrderBy(r => r.Inicio)
                .FirstOrDefaultAsync(cancellationToken);

        private async Task<ResultadoOperacion> ChoqueConSugerenciaAsync(
            int idEquipo, DateOnly fecha, Reserva cruce, TimeSpan duracion, CancellationToken cancellationToken)
        {
            var bloques = await AprobadasDelDiaAsync(idEquipo, fecha, cancellationToken);
            var libre = CruceHorarios.PrimeraFranjaLibre(bloques, cruce.Fin, duracion);
            var inicio = TimeOnly.FromDateTime(libre);
            var fin = TimeOnly.FromDateTime(libre + duracion);

            // Solo se ofrece si cabe el mismo día y dentro de las horas que muestra el formulario.
            var ofrecible = DateOnly.FromDateTime(libre + duracion) == fecha
                && CrearReservaViewModel.Horas.Contains(inicio)
                && CrearReservaViewModel.Horas.Contains(fin);

            var ocupado = $"Ocupado de {Formato.Hora(cruce.Inicio)} a {Formato.Hora(cruce.Fin)}.";
            return ofrecible
                ? ResultadoOperacion.FalloConSugerencia(
                    TipoResultado.Conflicto, Mensajes.ChoqueConAprobada, $"{ocupado} Primera franja libre: {Formato.Hora(libre)}.", inicio, fin)
                : ResultadoOperacion.Fallo(TipoResultado.Conflicto, Mensajes.ChoqueConAprobada, ocupado);
        }

        private async Task<List<(DateTime Inicio, DateTime Fin)>> AprobadasDelDiaAsync(
            int idEquipo, DateOnly fecha, CancellationToken cancellationToken)
        {
            var desde = fecha.ToDateTime(TimeOnly.MinValue);
            var hasta = desde.AddDays(1);

            var filas = await _context.Reservas.AsNoTracking()
                .Where(r => r.IdEquipo == idEquipo && r.Estado == EstadoReserva.Aprobada && r.Inicio >= desde && r.Inicio < hasta)
                .OrderBy(r => r.Inicio)
                .Select(r => new { r.Inicio, r.Fin })
                .ToListAsync(cancellationToken);

            return filas.Select(f => (f.Inicio, f.Fin)).ToList();
        }

        private static bool EsViolacionDeUnicidad(DbUpdateException ex)
            => ex.InnerException is SqlException { Number: ErrorClaveDuplicada or ErrorRestriccionUnica };
    }
}
