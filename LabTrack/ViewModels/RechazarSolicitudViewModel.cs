using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.ComponentModel.DataAnnotations;

namespace LabTrack.ViewModels
{
    public class RechazarSolicitudViewModel
    {
        [Range(1, int.MaxValue, ErrorMessage = "Selecciona una solicitud válida")]
        public int IdReserva { get; set; }

        [Required(ErrorMessage = "Escribe el motivo del rechazo")]
        [StringLength(200, ErrorMessage = "El motivo no puede exceder los 200 caracteres")]
        [Display(Name = "Motivo del rechazo (obligatorio)")]
        public string Motivo { get; set; } = string.Empty;

        [BindNever]
        public SolicitudPendienteViewModel? Solicitud { get; set; }
    }
}
