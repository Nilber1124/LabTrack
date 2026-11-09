using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.ComponentModel.DataAnnotations;

namespace LabTrack.ViewModels
{
    public class CrearReservaViewModel
    {
        // Opciones del formulario según el mockup; el servidor no impone franjas horarias.
        public static readonly IReadOnlyList<TimeOnly> Horas =
            Enumerable.Range(7, 15).Select(h => new TimeOnly(h, 0)).ToList();

        [Range(1, int.MaxValue, ErrorMessage = "Selecciona un equipo válido")]
        public int IdEquipo { get; set; }

        [Required(ErrorMessage = "Selecciona la fecha")]
        [Display(Name = "Fecha")]
        public DateOnly? Fecha { get; set; }

        [Required(ErrorMessage = "Selecciona la hora de inicio")]
        [Display(Name = "Hora de inicio")]
        public TimeOnly? HoraInicio { get; set; }

        [Required(ErrorMessage = "Selecciona la hora de fin")]
        [Display(Name = "Hora de fin")]
        public TimeOnly? HoraFin { get; set; }

        [BindNever]
        public string NombreEquipo { get; set; } = string.Empty;

        [BindNever]
        public string CodigoInventario { get; set; } = string.Empty;

        [BindNever]
        public string Categoria { get; set; } = string.Empty;

        [BindNever]
        public string Laboratorio { get; set; } = string.Empty;

        [BindNever]
        public string? FotoUrl { get; set; }

        [BindNever]
        public bool EquipoDisponible { get; set; }

        [BindNever]
        public DateOnly FechaMinima { get; set; }

        [BindNever]
        public DateOnly FechaOcupacion { get; set; }

        [BindNever]
        public IReadOnlyList<BloqueOcupadoViewModel> Ocupacion { get; set; } = [];

        [BindNever]
        public TimeOnly? SugerenciaInicio { get; set; }

        [BindNever]
        public TimeOnly? SugerenciaFin { get; set; }

        [BindNever]
        public bool EstudiantePenalizado { get; set; }

        [BindNever]
        public string? MotivoPenalizacion { get; set; }
    }
}
