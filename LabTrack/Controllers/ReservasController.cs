using LabTrack.Models;
using LabTrack.Reglas;
using LabTrack.Services;
using LabTrack.Services.Interfaces;
using LabTrack.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LabTrack.Controllers
{
    [Authorize(Roles = NombresRol.Estudiante)]
    public class ReservasController : Controller
    {
        private readonly IReservaService _reservas;

        public ReservasController(IReservaService reservas)
        {
            _reservas = reservas;
        }

        [HttpGet]
        public async Task<IActionResult> Index(EstadoReserva? estado, int pagina = 1, CancellationToken cancellationToken = default)
        {
            var idUsuario = User.ObtenerIdUsuario();
            if (idUsuario is null)
            {
                return Forbid();
            }

            return View(await _reservas.ListarPropiasAsync(idUsuario.Value, estado, pagina, cancellationToken));
        }

        [HttpGet]
        public async Task<IActionResult> Crear(int idEquipo, CancellationToken cancellationToken = default)
        {
            var modelo = await _reservas.ObtenerFormularioAsync(idEquipo, cancellationToken);
            if (modelo is null)
            {
                return NotFound();
            }

            if (!modelo.EquipoDisponible)
            {
                TempData["Error"] = Mensajes.EquipoNoDisponible;
                return RedirectToAction("Index", "Catalogo");
            }

            modelo.Fecha = modelo.FechaMinima;
            return View(modelo);
        }

        [HttpPost]
        public async Task<IActionResult> Crear(CrearReservaViewModel modelo, CancellationToken cancellationToken = default)
        {
            var idUsuario = User.ObtenerIdUsuario();
            if (idUsuario is null)
            {
                return Forbid();
            }

            if (!ModelState.IsValid)
            {
                return await VolverAlFormularioAsync(modelo, cancellationToken);
            }

            var resultado = await _reservas.CrearAsync(
                idUsuario.Value, modelo.IdEquipo, modelo.Fecha!.Value, modelo.HoraInicio!.Value, modelo.HoraFin!.Value, cancellationToken);

            if (resultado.Exitoso)
            {
                TempData["Success"] = resultado.Mensaje;
                return RedirectToAction(nameof(Index));
            }

            if (resultado.Tipo == TipoResultado.NoEncontrado)
            {
                return NotFound();
            }

            ModelState.AddModelError(string.Empty, resultado.Mensaje);
            return await VolverAlFormularioAsync(modelo, cancellationToken);
        }

        private async Task<IActionResult> VolverAlFormularioAsync(CrearReservaViewModel modelo, CancellationToken cancellationToken)
        {
            var datos = await _reservas.ObtenerFormularioAsync(modelo.IdEquipo, cancellationToken);
            if (datos is null)
            {
                return NotFound();
            }

            modelo.NombreEquipo = datos.NombreEquipo;
            modelo.CodigoInventario = datos.CodigoInventario;
            modelo.Categoria = datos.Categoria;
            modelo.EquipoDisponible = datos.EquipoDisponible;
            modelo.FechaMinima = datos.FechaMinima;
            return View(modelo);
        }
    }
}
