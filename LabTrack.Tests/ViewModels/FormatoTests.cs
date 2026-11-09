using LabTrack.ViewModels;

namespace LabTrack.Tests.ViewModels
{
    public class FormatoTests
    {
        [Fact]
        [Trait("HU", "HU-04")]
        [Trait("CA", "CA-11")]
        public void Horario_DosHoras_DevuelveIntervalo()
        {
            var resultado = Formato.Horario(new DateTime(2026, 11, 10, 10, 0, 0), new DateTime(2026, 11, 10, 12, 0, 0));

            Assert.Equal("10:00 – 12:00", resultado);
        }

        [Theory]
        [Trait("HU", "HU-05")]
        [Trait("CA", "CA-14")]
        [InlineData(45, "45 min")]
        [InlineData(75, "1 h 15 min")]
        [InlineData(-45, "45 min")]
        public void Duracion_SegunMinutos_DevuelveTexto(int minutos, string esperado)
        {
            Assert.Equal(esperado, Formato.Duracion(TimeSpan.FromMinutes(minutos)));
        }
    }
}
