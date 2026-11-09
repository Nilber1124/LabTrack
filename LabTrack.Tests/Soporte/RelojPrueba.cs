using Microsoft.Extensions.Time.Testing;

namespace LabTrack.Tests.Soporte
{
    public static class RelojPrueba
    {
        private static readonly TimeSpan DesfaseLima = TimeSpan.FromHours(-5);

        public static FakeTimeProvider En(DateTime horaLima)
        {
            var reloj = new FakeTimeProvider(new DateTimeOffset(horaLima, DesfaseLima));
            reloj.SetLocalTimeZone(TimeZoneInfo.FindSystemTimeZoneById("America/Lima"));
            return reloj;
        }

        public static void Fijar(FakeTimeProvider reloj, DateTime horaLima)
            => reloj.SetUtcNow(new DateTimeOffset(horaLima, DesfaseLima));
    }
}
