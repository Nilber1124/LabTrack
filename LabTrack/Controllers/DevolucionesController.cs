using LabTrack.Reglas;
using LabTrack.Services;
using LabTrack.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LabTrack.Controllers
{
    [Authorize(Roles = NombresRol.GestionLaboratorio)]
    public class DevolucionesController : Controller
    {
        private readonly IDevolucionService _devoluciones;

        public DevolucionesController(IDevolucionService devoluciones)
        {
            _devoluciones = devoluciones;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? busqueda, int pagina = 1, int? idUsuario = null, CancellationToken cancellationToken = default)
        {
            var modelo = await _devoluciones.ListarActivasAsync(busqueda, pagina, cancellationToken);

            var estudiante = idUsuario ?? modelo.Reservas.FirstOrDefault()?.IdUsuario;
            if (estudiante is not null)
            {
                modelo.Ficha = await _devoluciones.ObtenerFichaAsync(estudiante.Value, cancellationToken);
            }

            return View(modelo);
        }

        [HttpPost]
        public async Task<IActionResult> Marcar(int id, CancellationToken cancellationToken = default)
        {
            var resultado = await _devoluciones.MarcarDevolucionAsync(id, cancellationToken);

            if (resultado.Tipo == TipoResultado.NoEncontrado)
            {
                return NotFound();
            }

            TempData[resultado.Exitoso ? "Success" : "Error"] = resultado.Mensaje;
            if (resultado.Aviso is not null)
            {
                TempData["Error"] = resultado.Aviso;
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
