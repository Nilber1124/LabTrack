using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.ComponentModel.DataAnnotations;

namespace LabTrack.ViewModels
{
    public sealed class RechazarSolicitudViewModel
    {
        [BindNever]
        public bool EsVistaPrevia { get; set; }

        [Range(1, long.MaxValue)]
        public long Id { get; set; }

        [Required(ErrorMessage = "Escribe el motivo del rechazo.")]
        [StringLength(255, ErrorMessage = "El motivo debe tener como máximo 255 caracteres.")]
        [Display(Name = "Motivo del rechazo")]
        public string Motivo { get; set; } = string.Empty;

        [BindNever]
        public SolicitudPendienteViewModel? Solicitud { get; set; }
    }
}
