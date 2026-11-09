namespace LabTrack.Services
{
    // Fija la hora local en America/Lima aunque el servidor esté en otra zona (Azure usa UTC).
    public sealed class ZonaLimaTimeProvider : TimeProvider
    {
        private static readonly TimeZoneInfo ZonaLima = TimeZoneInfo.FindSystemTimeZoneById("America/Lima");

        public override TimeZoneInfo LocalTimeZone => ZonaLima;
    }
}
