using LabTrack.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LabTrack.ViewComponents
{
    public class ContadorGestionViewComponent : ViewComponent
    {
        public const string Solicitudes = "solicitudes";
        public const string Penalizados = "penalizados";

        private readonly IDevolucionService _devoluciones;

        public ContadorGestionViewComponent(IDevolucionService devoluciones)
        {
            _devoluciones = devoluciones;
        }

        public async Task<IViewComponentResult> InvokeAsync(string tipo)
        {
            var total = tipo == Penalizados
                ? await _devoluciones.ContarPenalizadosAsync(HttpContext.RequestAborted)
                : await _devoluciones.ContarSolicitudesPendientesAsync(HttpContext.RequestAborted);

            return Content(total > 0 ? $" · {total}" : string.Empty);
        }
    }
}
