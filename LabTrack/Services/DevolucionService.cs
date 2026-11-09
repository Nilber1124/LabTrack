using LabTrack.Data;
using LabTrack.Models;
using LabTrack.Reglas;
using LabTrack.Services.Interfaces;
using LabTrack.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace LabTrack.Services
{
    public sealed class DevolucionService : IDevolucionService
    {
        private const int TamanoPagina = 20;

        private readonly AppDbContext _context;
        private readonly TimeProvider _tiempo;

        public DevolucionService(AppDbContext context, TimeProvider tiempo)
        {
            _context = context;
            _tiempo = tiempo;
        }

        public async Task<DevolucionesViewModel> ListarActivasAsync(
            string? busqueda, int pagina, CancellationToken cancellationToken = default)
        {
            var ahora = _tiempo.GetLocalNow().DateTime;
            busqueda = string.IsNullOrWhiteSpace(busqueda) ? null : busqueda.Trim();

            var activas = _context.Reservas.AsNoTracking().Where(r => r.Estado == EstadoReserva.Aprobada);
            if (busqueda is not null)
            {
                activas = activas.Where(r => r.Usuario.Nombre.Contains(busqueda)
                    || r.Usuario.Codigo.Contains(busqueda)
                    || r.Equipo.Nombre.Contains(busqueda)
                    || r.Equipo.CodigoInventario.Contains(busqueda));
            }

            var total = await activas.CountAsync(cancellationToken);
            var vencidas = await activas.CountAsync(r => r.Fin < ahora, cancellationToken);
            pagina = PaginacionViewModel.Normalizar(pagina, total, TamanoPagina);

            var reservas = await activas
                .OrderBy(r => r.Fin)
                .ThenBy(r => r.IdReserva)
                .Skip((pagina - 1) * TamanoPagina)
                .Take(TamanoPagina)
                .Select(r => new ReservaActivaViewModel
                {
                    IdReserva = r.IdReserva,
                    Estudiante = r.Usuario.Nombre,
                    CodigoEstudiante = r.Usuario.Codigo,
                    Equipo = r.Equipo.Nombre,
                    CodigoInventario = r.Equipo.CodigoInventario,
                    Inicio = r.Inicio,
                    Fin = r.Fin
                })
                .ToListAsync(cancellationToken);

            reservas.ForEach(r => DescribirSituacion(r, ahora));

            return new DevolucionesViewModel
            {
                Reservas = reservas,
                Busqueda = busqueda,
                TotalVencidas = vencidas,
                Ahora = ahora,
                Paginacion = new PaginacionViewModel
                {
                    Pagina = pagina,
                    Total = total,
                    TamanoPagina = TamanoPagina,
                    Etiqueta = "reservas activas",
                    Parametros = new() { ["busqueda"] = busqueda }
                }
            };
        }

        public async Task<ResultadoOperacion> MarcarDevolucionAsync(int idReserva, CancellationToken cancellationToken = default)
        {
            var reserva = await _context.Reservas
                .Include(r => r.Usuario)
                .Include(r => r.Equipo)
                .FirstOrDefaultAsync(r => r.IdReserva == idReserva, cancellationToken);

            if (reserva is null)
            {
                return ResultadoOperacion.Fallo(TipoResultado.NoEncontrado, Mensajes.ReservaNoEncontrada);
            }

            if (reserva.Estado != EstadoReserva.Aprobada)
            {
                return ResultadoOperacion.Fallo(TipoResultado.EstadoInvalido, Mensajes.SoloReservasAprobadas);
            }

            var ahora = _tiempo.GetLocalNow().DateTime;
            var penalizar = PoliticaPenalizacion.DebePenalizar(reserva.Fin, ahora);

            reserva.Estado = EstadoReserva.Devuelta;
            reserva.FechaDevolucionReal = ahora;
            if (penalizar)
            {
                reserva.Usuario.Estado = EstadoUsuario.Penalizado;
            }

            try
            {
                // Un solo SaveChanges: devolución y penalización se guardan juntas o no se guardan.
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                return ResultadoOperacion.Fallo(TipoResultado.Conflicto, Mensajes.ConflictoConcurrencia);
            }

            return ResultadoOperacion.Ok(
                $"Devolución registrada. {reserva.Equipo.CodigoInventario} ya figura como Disponible.",
                penalizar ? $"{reserva.Usuario.Nombre} quedó Penalizado por devolución tardía." : null);
        }

        private static void DescribirSituacion(ReservaActivaViewModel reserva, DateTime ahora)
        {
            reserva.Vencida = PoliticaPenalizacion.DebePenalizar(reserva.Fin, ahora);

            if (reserva.Vencida)
            {
                reserva.Situacion = "vencida";
                reserva.Nota = $"Vencida hace {Formato.Duracion(ahora - reserva.Fin)}";
            }
            else if (reserva.Inicio <= ahora)
            {
                reserva.Situacion = "activa";
                reserva.Nota = $"Faltan {Formato.Duracion(reserva.Fin - ahora)}";
            }
            else
            {
                reserva.Situacion = "aprobada";
                reserva.Nota = $"Inicia {Formato.Fecha(reserva.Inicio)} a las {Formato.Hora(reserva.Inicio)}";
            }
        }
    }
}
