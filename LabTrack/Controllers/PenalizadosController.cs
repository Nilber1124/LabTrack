using LabTrack.Reglas;
using LabTrack.Services;
using LabTrack.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LabTrack.Controllers
{
    [Authorize(Roles = NombresRol.Encargado)]
    public class PenalizadosController : Controller
    {
        private readonly IDevolucionService _devoluciones;

        public PenalizadosController(IDevolucionService devoluciones)
        {
            _devoluciones = devoluciones;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? busqueda, int pagina = 1, CancellationToken cancellationToken = default)
        {
            return View(await _devoluciones.ListarPenalizadosAsync(busqueda, pagina, cancellationToken));
        }

        [HttpPost]
        public async Task<IActionResult> Levantar(int id, CancellationToken cancellationToken = default)
        {
            var resultado = await _devoluciones.LevantarPenalizacionAsync(id, cancellationToken);

            if (resultado.Tipo == TipoResultado.NoEncontrado)
            {
                return NotFound();
            }

            TempData[resultado.Exitoso ? "Success" : "Error"] = resultado.Mensaje;
            return RedirectToAction(nameof(Index));
        }
    }
}
