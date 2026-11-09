using LabTrack.Models;
using LabTrack.Reglas;

namespace LabTrack.Tests.Reglas
{
    public class CruceHorariosTests
    {
        private static DateTime H(int hora, int minuto = 0) => new(2026, 11, 10, hora, minuto, 0);

        [Theory]
        [Trait("HU", "HU-03")]
        [Trait("CA", "CA-09")]
        [InlineData(13, 15, 14, 16)] // Parcial al inicio
        [InlineData(15, 17, 14, 16)] // Parcial al final
        [InlineData(13, 17, 14, 16)] // El nuevo contiene al existente
        [InlineData(14, 16, 14, 16)] // Idéntico
        public void SeCruzan_HorariosSolapados_DevuelveVerdadero(int inicioA, int finA, int inicioB, int finB)
        {
            Assert.True(CruceHorarios.SeCruzan(H(inicioA), H(finA), H(inicioB), H(finB)));
        }

        [Fact]
        [Trait("HU", "HU-03")]
        [Trait("CA", "CA-09")]
        public void SeCruzan_NuevoContenidoEnExistente_DevuelveVerdadero()
        {
            Assert.True(CruceHorarios.SeCruzan(H(14, 30), H(15, 30), H(14), H(16)));
        }

        [Theory]
        [Trait("HU", "HU-03")]
        [Trait("CA", "CA-09")]
        [InlineData(12, 14, 14, 16)] // Contiguo antes
        [InlineData(16, 18, 14, 16)] // Contiguo después
        [InlineData(8, 10, 14, 16)]  // Separado
        public void SeCruzan_HorariosContiguosOSeparados_DevuelveFalso(int inicioA, int finA, int inicioB, int finB)
        {
            Assert.False(CruceHorarios.SeCruzan(H(inicioA), H(finA), H(inicioB), H(finB)));
        }

        [Fact]
        [Trait("HU", "HU-03")]
        [Trait("CA", "CA-09")]
        public void SeCruzan_UnMinutoDeSolape_DevuelveVerdadero()
        {
            Assert.True(CruceHorarios.SeCruzan(H(13), H(14, 1), H(14), H(16)));
        }

        [Theory]
        [Trait("HU", "HU-03")]
        [Trait("CA", "CA-09")]
        [InlineData(13, 15, true)]
        [InlineData(16, 18, false)]
        public void ConHorario_ExpresionTraducible_CoincideConSeCruzan(int inicio, int fin, bool esperado)
        {
            var reserva = new Reserva { Inicio = H(14), Fin = H(16) };

            var resultado = CruceHorarios.ConHorario(H(inicio), H(fin)).Compile()(reserva);

            Assert.Equal(esperado, resultado);
        }
    }
}
