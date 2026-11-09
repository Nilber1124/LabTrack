using System.ComponentModel.DataAnnotations;

namespace LabTrack.Tests.Soporte
{
    public static class Validacion
    {
        public static IReadOnlyList<ValidationResult> Errores(object instancia)
        {
            var errores = new List<ValidationResult>();
            Validator.TryValidateObject(instancia, new ValidationContext(instancia), errores, validateAllProperties: true);
            return errores;
        }
    }
}
