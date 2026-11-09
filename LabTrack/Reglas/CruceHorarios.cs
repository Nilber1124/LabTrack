using LabTrack.Models;
using System.Linq.Expressions;

namespace LabTrack.Reglas
{
    public static class CruceHorarios
    {
        // Los horarios contiguos (fin de uno = inicio del otro) no se cruzan.
        public static bool SeCruzan(DateTime inicioA, DateTime finA, DateTime inicioB, DateTime finB)
            => inicioA < finB && inicioB < finA;

        // Primer inicio, desde el indicado, en el que cabe la duración sin pisar ningún bloque.
        public static DateTime PrimeraFranjaLibre(
            IEnumerable<(DateTime Inicio, DateTime Fin)> bloques, DateTime desde, TimeSpan duracion)
        {
            var candidato = desde;

            foreach (var bloque in bloques.OrderBy(b => b.Inicio))
            {
                if (SeCruzan(candidato, candidato + duracion, bloque.Inicio, bloque.Fin))
                {
                    candidato = bloque.Fin;
                }
            }

            return candidato;
        }

        // Misma fórmula que SeCruzan, en forma traducible a SQL.
        public static Expression<Func<Reserva, bool>> ConHorario(DateTime inicio, DateTime fin)
            => r => r.Inicio < fin && inicio < r.Fin;
    }
}
