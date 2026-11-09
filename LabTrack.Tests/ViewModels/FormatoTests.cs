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
        [Trait("HU", "HU-04")]
        [Trait("CA", "CA-11")]
        [InlineData(0, "hace un momento")]
        [InlineData(12, "hace 12 min")]
        [InlineData(60, "hace 1 h")]
        [InlineData(1500, "hace 1 d")]
        public void Antiguedad_SegunMinutos_DevuelveTexto(int minutos, string esperado)
        {
            Assert.Equal(esperado, Formato.Antiguedad(TimeSpan.FromMinutes(minutos)));
        }

        [Theory]
        [Trait("HU", "HU-03")]
        [Trait("CA", "CA-09")]
        [InlineData(7, 9, "0%", "14.29%")]
        [InlineData(14, 16, "50%", "14.29%")]
        [InlineData(20, 23, "92.86%", "7.14%")]
        public void BloqueOcupado_Posicion_SeCalculaSobreLaJornada(int inicio, int fin, string izquierda, string ancho)
        {
            var bloque = new BloqueOcupadoViewModel
            {
                Inicio = new DateTime(2026, 11, 10, inicio, 0, 0),
                Fin = new DateTime(2026, 11, 10, fin, 0, 0)
            };

            Assert.Equal((izquierda, ancho), (bloque.Izquierda, bloque.Ancho));
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
