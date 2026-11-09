using System.Data.Common;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using LabTrack.Services.Interfaces;
using LabTrack.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LabTrack.Controllers
{
    [Authorize(Roles = "Encargado")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public sealed class SolicitudesController : Controller
    {
        private readonly ISolicitudesService _solicitudes;
        private readonly ILogger<SolicitudesController> _logger;

        public SolicitudesController(ISolicitudesService solicitudes, ILogger<SolicitudesController> logger)
        {
            _solicitudes = solicitudes;
            _logger = logger;
        }

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var executed = await next();
            if (executed.Exception is DbException or DbUpdateException)
            {
                _logger.LogError(executed.Exception, "No se pudo completar la gestión de solicitudes.");
                executed.ExceptionHandled = true;
                executed.Result = new ViewResult
                {
                    ViewName = "NoDisponible",
                    StatusCode = StatusCodes.Status503ServiceUnavailable
                };
            }
        }

        private bool TryGetEncargado(out long id)
            => long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out id) && id > 0;

        [HttpGet]
        public async Task<IActionResult> Index(int pagina = 1, string? busqueda = null, CancellationToken cancellationToken = default)
        {
            if (!TryGetEncargado(out var encargadoId))
                return Forbid();
            try
            {
                return View(await _solicitudes.ListarPendientesAsync(encargadoId, pagina, busqueda, cancellationToken));
            }
            catch (UnauthorizedAccessException) { return Forbid(); }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Aprobar(long id, CancellationToken cancellationToken = default)
        {
            if (!TryGetEncargado(out var encargadoId))
                return Forbid();
            try
            {
                var solicitud = await _solicitudes.ObtenerPendienteAsync(encargadoId, id, cancellationToken);
                var resultado = await _solicitudes.AprobarAsync(encargadoId, id, cancellationToken);
                var mensaje = solicitud is null ? "Reserva aprobada. El equipo figura como prestado."
                    : $"Reserva aprobada. {solicitud.Equipo} ({solicitud.CodigoInventario}) figura como prestado del {solicitud.Inicio:dd/MM/yyyy HH:mm zzz} al {solicitud.Fin:dd/MM/yyyy HH:mm zzz}.";
                return Finalizar(resultado, mensaje);
            }
            catch (UnauthorizedAccessException) { return Forbid(); }
        }

        [HttpGet]
        public async Task<IActionResult> Rechazar(long id, CancellationToken cancellationToken = default)
        {
            if (!TryGetEncargado(out var encargadoId))
                return Forbid();
            try
            {
                var solicitud = await _solicitudes.ObtenerPendienteAsync(encargadoId, id, cancellationToken);
                if (solicitud is null)
                    return NotFound();
                return View(new RechazarSolicitudViewModel { Id = id, Solicitud = solicitud });
            }
            catch (UnauthorizedAccessException) { return Forbid(); }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Rechazar(RechazarSolicitudViewModel model, CancellationToken cancellationToken = default)
        {
            if (!TryGetEncargado(out var encargadoId))
                return Forbid();
            try
            {
                if (ModelState.IsValid)
                {
                    var resultado = await _solicitudes.RechazarAsync(encargadoId, model.Id, model.Motivo, cancellationToken);
                    if (resultado != EstadoResolucion.MotivoInvalido)
                        return Finalizar(resultado, "Solicitud rechazada. Se guardó el motivo.");
                    ModelState.AddModelError(nameof(model.Motivo), "Escribe un motivo de entre 1 y 255 caracteres.");
                }

                model.Solicitud = await _solicitudes.ObtenerPendienteAsync(encargadoId, model.Id, cancellationToken);
                if (model.Solicitud is null)
                    return NotFound();
                return View(model);
            }
            catch (UnauthorizedAccessException) { return Forbid(); }
        }

        private IActionResult Finalizar(EstadoResolucion resultado, string mensaje)
        {
            if (resultado == EstadoResolucion.NoEncontrada)
                return NotFound();

            var error = resultado switch
            {
                EstadoResolucion.YaResuelta => "La solicitud ya fue resuelta. Se actualizó el listado.",
                EstadoResolucion.ConflictoHorario => "El equipo ya tiene una reserva aprobada que coincide con este horario.",
                EstadoResolucion.EstudianteInvalido => "La solicitud no corresponde a un estudiante válido.",
                EstadoResolucion.MotivoInvalido => "Escribe un motivo de entre 1 y 255 caracteres.",
                _ => null
            };
            TempData["TituloResultado"] = error is null
                ? (mensaje.Contains("aprobada", StringComparison.OrdinalIgnoreCase) ? "Reserva aprobada" : "Solicitud rechazada")
                : (resultado == EstadoResolucion.ConflictoHorario ? "No se pudo aprobar" : "No se pudo resolver");
            TempData[error is null ? "Exito" : "Error"] = error ?? mensaje;
            return RedirectToAction(nameof(Index));
        }
    }
}
