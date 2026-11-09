using LabTrack.Models;
using LabTrack.Reglas;

namespace LabTrack.Tests.Reglas
{
    public class EstadoPrestamoTests
    {
        private static readonly DateTime Ahora = new(2026, 11, 9, 10, 0, 0);

        private static bool EnCurso(EstadoReserva estado, DateTime inicio, DateTime fin)
            => EstadoPrestamo.EnCurso(Ahora).Compile()(new Reserva { Estado = estado, Inicio = inicio, Fin = fin });

        [Fact]
        [Trait("HU", "HU-02")]
        [Trait("CA", "CA-04")]
        public void Calcular_ConPrestamoEnCurso_DevuelvePrestado()
        {
            Assert.Equal(EstadoEquipo.Prestado, EstadoPrestamo.Calcular(true));
        }

        [Fact]
        [Trait("HU", "HU-02")]
        [Trait("CA", "CA-04")]
        public void Calcular_SinPrestamoEnCurso_DevuelveDisponible()
        {
            Assert.Equal(EstadoEquipo.Disponible, EstadoPrestamo.Calcular(false));
        }

        [Fact]
        [Trait("HU", "HU-04")]
        [Trait("CA", "CA-12")]
        public void EnCurso_AprobadaDentroDelBloque_DevuelveVerdadero()
        {
            Assert.True(EnCurso(EstadoReserva.Aprobada, Ahora.AddHours(-1), Ahora.AddHours(1)));
        }

        [Fact]
        [Trait("HU", "HU-04")]
        [Trait("CA", "CA-12")]
        public void EnCurso_AprobadaQueIniciaAhora_DevuelveVerdadero()
        {
            Assert.True(EnCurso(EstadoReserva.Aprobada, Ahora, Ahora.AddHours(1)));
        }

        [Fact]
        [Trait("HU", "HU-04")]
        [Trait("CA", "CA-12")]
        public void EnCurso_AprobadaFutura_DevuelveFalso()
        {
            Assert.False(EnCurso(EstadoReserva.Aprobada, Ahora.AddMinutes(1), Ahora.AddHours(1)));
        }

        [Fact]
        [Trait("HU", "HU-05")]
        [Trait("CA", "CA-15")]
        public void EnCurso_AprobadaVencidaSinDevolver_DevuelveVerdadero()
        {
            Assert.True(EnCurso(EstadoReserva.Aprobada, Ahora.AddHours(-3), Ahora.AddHours(-1)));
        }

        [Theory]
        [Trait("HU", "HU-05")]
        [Trait("CA", "CA-15")]
        [InlineData(EstadoReserva.Pendiente)]
        [InlineData(EstadoReserva.Rechazada)]
        [InlineData(EstadoReserva.Devuelta)]
        public void EnCurso_ReservaNoAprobada_DevuelveFalso(EstadoReserva estado)
        {
            Assert.False(EnCurso(estado, Ahora.AddHours(-1), Ahora.AddHours(1)));
        }
    }
}
