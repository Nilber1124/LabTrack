using System.Text.Json;
using LabTrack.Models;
using LabTrack.Services;
using LabTrack.Services.Interfaces;
using LabTrack.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace LabTrack.Controllers
{
    [AllowAnonymous]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public class VistaPreviaController : Controller
    {
        private const string ClaveSesion = "LabTrack.Demo.Solicitudes";
        private readonly IWebHostEnvironment _environment;
        private readonly VistaPreviaSolicitudesService _demo;

        public VistaPreviaController(IWebHostEnvironment environment, VistaPreviaSolicitudesService demo)
        {
            _environment = environment;
            _demo = demo;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (!_environment.IsDevelopment()) context.Result = NotFound();
        }

        private EscenarioVistaPreviaViewModel LeerEscenario()
        {
            var json = HttpContext.Session.GetString(ClaveSesion);
            var escenario = json is null ? _demo.CrearEscenario()
                : JsonSerializer.Deserialize<EscenarioVistaPreviaViewModel>(json)!;
            Guardar(escenario);
            return escenario;
        }

        private void Guardar(EscenarioVistaPreviaViewModel escenario)
            => HttpContext.Session.SetString(ClaveSesion, JsonSerializer.Serialize(escenario));

        [HttpGet]
        public IActionResult Solicitudes(string? busqueda = null)
            => View("~/Views/Solicitudes/Index.cshtml", _demo.Listar(LeerEscenario(), busqueda));

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Aprobar(long id)
        {
            var escenario = LeerEscenario();
            var resultado = _demo.Aprobar(escenario, id);
            Guardar(escenario);
            var solicitud = escenario.Solicitudes.SingleOrDefault(x => x.Solicitud.Id == id)?.Solicitud;
            var mensaje = solicitud is null ? "Demostración: reserva aprobada."
                : $"Demostración: reserva aprobada. {solicitud.Equipo} ({solicitud.CodigoInventario}) figura como prestado del {solicitud.Inicio:dd/MM/yyyy HH:mm zzz} al {solicitud.Fin:dd/MM/yyyy HH:mm zzz}.";
            return Finalizar(resultado, mensaje);
        }

        [HttpGet]
        public IActionResult Rechazar(long id)
        {
            var solicitud = LeerEscenario().Solicitudes.SingleOrDefault(x => x.Solicitud.Id == id && x.Estado == EstadoReserva.Pendiente);
            if (solicitud is null) return NotFound();
            return View("~/Views/Solicitudes/Rechazar.cshtml", new RechazarSolicitudViewModel
            {
                Id = id, Solicitud = solicitud.Solicitud, EsVistaPrevia = true
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Rechazar(RechazarSolicitudViewModel model)
        {
            var escenario = LeerEscenario();
            if (ModelState.IsValid)
            {
                var resultado = _demo.Rechazar(escenario, model.Id, model.Motivo);
                if (resultado != EstadoResolucion.MotivoInvalido)
                {
                    Guardar(escenario);
                    return Finalizar(resultado, "Demostración: solicitud rechazada y motivo guardado en esta sesión.");
                }
                ModelState.AddModelError(nameof(model.Motivo), "Escribe un motivo de entre 1 y 255 caracteres.");
            }
            model.Solicitud = escenario.Solicitudes.SingleOrDefault(x => x.Solicitud.Id == model.Id && x.Estado == EstadoReserva.Pendiente)?.Solicitud;
            if (model.Solicitud is null) return NotFound();
            model.EsVistaPrevia = true;
            return View("~/Views/Solicitudes/Rechazar.cshtml", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Reiniciar()
        {
            Guardar(_demo.CrearEscenario());
            TempData["Exito"] = "Se reiniciaron los ejemplos de demostración.";
            return RedirectToAction(nameof(Solicitudes));
        }

        private IActionResult Finalizar(EstadoResolucion resultado, string mensaje)
        {
            if (resultado == EstadoResolucion.NoEncontrada) return NotFound();
            var error = resultado switch
            {
                EstadoResolucion.YaResuelta => "La solicitud ya fue resuelta en esta demostración.",
                EstadoResolucion.ConflictoHorario => "El equipo ya está prestado en ese horario. Prueba con otra solicitud.",
                _ => null
            };
            TempData["TituloResultado"] = error is null
                ? (mensaje.Contains("aprobada", StringComparison.OrdinalIgnoreCase) ? "Reserva aprobada" : "Solicitud rechazada")
                : (resultado == EstadoResolucion.ConflictoHorario ? "No se pudo aprobar" : "No se pudo resolver");
            TempData[error is null ? "Exito" : "Error"] = error ?? mensaje;
            return RedirectToAction(nameof(Solicitudes));
        }
    }
}
