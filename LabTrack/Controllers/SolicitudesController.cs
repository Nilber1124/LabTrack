using LabTrack.Reglas;
using LabTrack.Services;
using LabTrack.Services.Interfaces;
using LabTrack.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LabTrack.Controllers
{
    [Authorize(Roles = NombresRol.Encargado)]
    public class SolicitudesController : Controller
    {
        private readonly IReservaService _reservas;

        public SolicitudesController(IReservaService reservas)
        {
            _reservas = reservas;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int pagina = 1, CancellationToken cancellationToken = default)
        {
            return View(await _reservas.ListarPendientesAsync(pagina, cancellationToken));
        }

        [HttpPost]
        public async Task<IActionResult> Aprobar(int id, CancellationToken cancellationToken = default)
        {
            var resultado = await _reservas.AprobarAsync(id, cancellationToken);
            return Finalizar(resultado);
        }

        [HttpGet]
        public async Task<IActionResult> Rechazar(int id, CancellationToken cancellationToken = default)
        {
            var solicitud = await _reservas.ObtenerPendienteAsync(id, cancellationToken);
            if (solicitud is null)
            {
                return NotFound();
            }

            return View(new RechazarSolicitudViewModel { IdReserva = id, Solicitud = solicitud });
        }

        [HttpPost]
        public async Task<IActionResult> Rechazar(RechazarSolicitudViewModel modelo, CancellationToken cancellationToken = default)
        {
            if (ModelState.IsValid)
            {
                var resultado = await _reservas.RechazarAsync(modelo.IdReserva, modelo.Motivo, cancellationToken);
                if (resultado.Tipo != TipoResultado.Validacion)
                {
                    return Finalizar(resultado);
                }

                ModelState.AddModelError(nameof(modelo.Motivo), resultado.Mensaje);
            }

            modelo.Solicitud = await _reservas.ObtenerPendienteAsync(modelo.IdReserva, cancellationToken);
            if (modelo.Solicitud is null)
            {
                return NotFound();
            }

            return View(modelo);
        }

        private IActionResult Finalizar(ResultadoOperacion resultado)
        {
            if (resultado.Tipo == TipoResultado.NoEncontrado)
            {
                return NotFound();
            }

            TempData[resultado.Exitoso ? "Success" : "Error"] = resultado.Mensaje;
            return RedirectToAction(nameof(Index));
        }
    }
}
