using LabTrack.Reglas;

namespace LabTrack.Tests.Reglas
{
    public class ValidacionHorarioTests
    {
        private static readonly DateTime Ahora = new(2026, 11, 9, 10, 0, 0);

        [Fact]
        [Trait("HU", "HU-03")]
        [Trait("CA", "CA-08")]
        public void Componer_FechaYHora_DevuelveInstanteCompleto()
        {
            var resultado = ValidacionHorario.Componer(new DateOnly(2026, 11, 10), new TimeOnly(14, 30));

            Assert.Equal(new DateTime(2026, 11, 10, 14, 30, 0), resultado);
        }

        [Fact]
        [Trait("HU", "HU-03")]
        [Trait("CA", "CA-08")]
        public void Validar_HorarioFuturoCorrecto_DevuelveNull()
        {
            Assert.Null(ValidacionHorario.Validar(Ahora.AddHours(1), Ahora.AddHours(2), Ahora));
        }

        [Fact]
        [Trait("HU", "HU-03")]
        [Trait("CA", "CA-08")]
        public void Validar_InicioExactamenteAhora_DevuelveNull()
        {
            Assert.Null(ValidacionHorario.Validar(Ahora, Ahora.AddHours(1), Ahora));
        }

        [Fact]
        [Trait("HU", "HU-03")]
        [Trait("CA", "CA-08")]
        public void Validar_FinIgualAInicio_DevuelveError()
        {
            var inicio = Ahora.AddHours(1);

            Assert.Equal(Mensajes.FinPosteriorAInicio, ValidacionHorario.Validar(inicio, inicio, Ahora));
        }

        [Fact]
        [Trait("HU", "HU-03")]
        [Trait("CA", "CA-08")]
        public void Validar_FinAnteriorAInicio_DevuelveError()
        {
            Assert.Equal(Mensajes.FinPosteriorAInicio, ValidacionHorario.Validar(Ahora.AddHours(2), Ahora.AddHours(1), Ahora));
        }

        [Fact]
        [Trait("HU", "HU-03")]
        [Trait("CA", "CA-08")]
        public void Validar_InicioUnMinutoEnElPasado_DevuelveError()
        {
            Assert.Equal(Mensajes.InicioEnPasado, ValidacionHorario.Validar(Ahora.AddMinutes(-1), Ahora.AddHours(1), Ahora));
        }
    }
}
