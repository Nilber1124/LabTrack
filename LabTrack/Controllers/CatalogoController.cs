using LabTrack.Reglas;
using LabTrack.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LabTrack.Controllers
{
    [Authorize(Roles = NombresRol.Estudiante)]
    public class CatalogoController : Controller
    {
        private readonly ICatalogoService _catalogo;

        public CatalogoController(ICatalogoService catalogo)
        {
            _catalogo = catalogo;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? categoria, int pagina = 1, string? busqueda = null, CancellationToken cancellationToken = default)
        {
            var modelo = await _catalogo.ListarAsync(categoria, pagina, busqueda, cancellationToken);
            if (modelo.SinResultados)
            {
                modelo.MensajeVacio = Mensajes.SinEquiposEnCategoria;
            }

            return View(modelo);
        }
    }
}
