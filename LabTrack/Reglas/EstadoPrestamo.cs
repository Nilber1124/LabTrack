using LabTrack.Models;
using System.Linq.Expressions;

namespace LabTrack.Reglas
{
    public static class EstadoPrestamo
    {
        // Un equipo está prestado desde que inicia una reserva aprobada hasta que se marca su
        // devolución, aunque ya haya pasado la hora de fin.
        public static Expression<Func<Reserva, bool>> EnCurso(DateTime ahora)
            => r => r.Estado == EstadoReserva.Aprobada && r.Inicio <= ahora;

        public static EstadoEquipo Calcular(bool tienePrestamoEnCurso)
            => tienePrestamoEnCurso ? EstadoEquipo.Prestado : EstadoEquipo.Disponible;
    }
}
