using LabTrack.Controllers;
using LabTrack.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LabTrack.ViewComponents
{
    public class UsuarioActualViewComponent : ViewComponent
    {
        private readonly IAutenticacionService _autenticacion;

        public UsuarioActualViewComponent(IAutenticacionService autenticacion)
        {
            _autenticacion = autenticacion;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var idUsuario = (User as ClaimsPrincipal)?.ObtenerIdUsuario();
            if (idUsuario is null)
            {
                return Content(string.Empty);
            }

            var resumen = await _autenticacion.ObtenerResumenAsync(idUsuario.Value, HttpContext.RequestAborted);
            return resumen is null ? Content(string.Empty) : View(resumen);
        }
    }
}
