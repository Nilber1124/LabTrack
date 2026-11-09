using LabTrack.Services;

namespace LabTrack.Tests.Services
{
    public class ZonaLimaTimeProviderTests
    {
        [Fact]
        [Trait("HU", "HU-05")]
        [Trait("CA", "CA-16")]
        public void GetLocalNow_RelojReal_UsaElDesfaseDeLima()
        {
            var ahora = new ZonaLimaTimeProvider().GetLocalNow();

            Assert.Equal(TimeSpan.FromHours(-5), ahora.Offset);
        }

        [Fact]
        [Trait("HU", "HU-05")]
        [Trait("CA", "CA-16")]
        public void GetLocalNow_RelojReal_CoincideConLaHoraUtcMenosCincoHoras()
        {
            var proveedor = new ZonaLimaTimeProvider();

            var diferencia = proveedor.GetUtcNow().UtcDateTime.AddHours(-5) - proveedor.GetLocalNow().DateTime;

            Assert.True(diferencia.Duration() < TimeSpan.FromMinutes(1));
        }
    }
}
