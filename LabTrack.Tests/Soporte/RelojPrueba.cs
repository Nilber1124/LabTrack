using Microsoft.Extensions.Time.Testing;

namespace LabTrack.Tests.Soporte
{
    public static class RelojPrueba
    {
        private static readonly TimeSpan DesfaseLima = TimeSpan.FromHours(-5);

        public static FakeTimeProvider En(DateTime horaLima)
        {
            // TimeProvider exige un instante con desfase cero; si no, aplicaría la zona dos veces.
            var reloj = new FakeTimeProvider(EnUtc(horaLima));
            reloj.SetLocalTimeZone(TimeZoneInfo.FindSystemTimeZoneById("America/Lima"));
            return reloj;
        }

        public static void Fijar(FakeTimeProvider reloj, DateTime horaLima)
            => reloj.SetUtcNow(EnUtc(horaLima));

        private static DateTimeOffset EnUtc(DateTime horaLima)
            => new DateTimeOffset(horaLima, DesfaseLima).ToUniversalTime();
    }
}
