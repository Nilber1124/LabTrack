using LabTrack.Models;
using System.Linq.Expressions;

namespace LabTrack.Reglas
{
    public static class CruceHorarios
    {
        // Los horarios contiguos (fin de uno = inicio del otro) no se cruzan.
        public static bool SeCruzan(DateTime inicioA, DateTime finA, DateTime inicioB, DateTime finB)
            => inicioA < finB && inicioB < finA;

        // Misma fórmula que SeCruzan, en forma traducible a SQL.
        public static Expression<Func<Reserva, bool>> ConHorario(DateTime inicio, DateTime fin)
            => r => r.Inicio < fin && inicio < r.Fin;
    }
}
