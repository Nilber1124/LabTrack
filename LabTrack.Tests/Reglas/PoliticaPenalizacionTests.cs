using LabTrack.Reglas;

namespace LabTrack.Tests.Reglas
{
    public class PoliticaPenalizacionTests
    {
        private static readonly DateTime Fin = new(2026, 11, 9, 10, 0, 0);

        [Fact]
        [Trait("HU", "HU-05")]
        [Trait("CA", "CA-16")]
        public void DebePenalizar_AntesDelFin_DevuelveFalso()
        {
            Assert.False(PoliticaPenalizacion.DebePenalizar(Fin, Fin.AddMinutes(-1)));
        }

        [Fact]
        [Trait("HU", "HU-05")]
        [Trait("CA", "CA-16")]
        public void DebePenalizar_ExactamenteALaHoraDeFin_DevuelveFalso()
        {
            Assert.False(PoliticaPenalizacion.DebePenalizar(Fin, Fin));
        }

        [Fact]
        [Trait("HU", "HU-05")]
        [Trait("CA", "CA-16")]
        public void DebePenalizar_UnMinutoDespues_DevuelveVerdadero()
        {
            Assert.True(PoliticaPenalizacion.DebePenalizar(Fin, Fin.AddMinutes(1)));
        }

        [Fact]
        [Trait("HU", "HU-05")]
        [Trait("CA", "CA-16")]
        public void DebePenalizar_UnSegundoDespues_DevuelveVerdadero()
        {
            Assert.True(PoliticaPenalizacion.DebePenalizar(Fin, Fin.AddSeconds(1)));
        }
    }
}
