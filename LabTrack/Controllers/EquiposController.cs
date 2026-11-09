using LabTrack.Reglas;
using LabTrack.Services;
using LabTrack.Services.Interfaces;
using LabTrack.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LabTrack.Controllers
{
    [Authorize(Roles = NombresRol.Administrador)]
    public class EquiposController : Controller
    {
        private const string VistaFormulario = "Formulario";

        private readonly IEquipoService _equipos;

        public EquiposController(IEquipoService equipos)
        {
            _equipos = equipos;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? busqueda, string? categoria, int pagina = 1, CancellationToken cancellationToken = default)
        {
            return View(await _equipos.ListarAsync(busqueda, categoria, pagina, cancellationToken));
        }

        // Nuevo y Editar son la alternativa sin JavaScript al modal de la lista.
        [HttpGet]
        public async Task<IActionResult> Nuevo(CancellationToken cancellationToken = default)
        {
            return View(VistaFormulario, await _equipos.ObtenerFormularioNuevoAsync(cancellationToken));
        }

        [HttpGet]
        public async Task<IActionResult> Editar(int id, CancellationToken cancellationToken = default)
        {
            var formulario = await _equipos.ObtenerFormularioEdicionAsync(id, cancellationToken);
            return formulario is null ? NotFound() : View(VistaFormulario, formulario);
        }

        [HttpPost]
        public async Task<IActionResult> Guardar(FormularioEquipoViewModel modelo, CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid)
            {
                return await VolverAlFormularioAsync(modelo, cancellationToken);
            }

            var datos = new DatosEquipo
            {
                IdEquipo = modelo.IdEquipo,
                Nombre = modelo.Nombre,
                CodigoInventario = modelo.CodigoInventario,
                IdCategoria = modelo.IdCategoria!.Value,
                Laboratorio = modelo.Laboratorio
            };

            ResultadoOperacion resultado;
            if (modelo.Foto is { Length: > 0 } foto)
            {
                await using var contenido = foto.OpenReadStream();
                resultado = await _equipos.GuardarAsync(datos, new ImagenSubida(contenido, foto.Length), cancellationToken);
            }
            else
            {
                resultado = await _equipos.GuardarAsync(datos, null, cancellationToken);
            }

            if (resultado.Exitoso)
            {
                TempData["Success"] = resultado.Mensaje;
                return RedirectToAction(nameof(Index));
            }

            if (resultado.Tipo == TipoResultado.NoEncontrado)
            {
                return NotFound();
            }

            var campo = resultado.Mensaje == Mensajes.CodigoRepetido ? nameof(modelo.CodigoInventario) : string.Empty;
            ModelState.AddModelError(campo, TextoCompleto(resultado));
            return await VolverAlFormularioAsync(modelo, cancellationToken);
        }

        [HttpPost]
        public async Task<IActionResult> DarDeBaja(int id, CancellationToken cancellationToken = default)
        {
            return Finalizar(await _equipos.DarDeBajaAsync(id, cancellationToken));
        }

        [HttpPost]
        public async Task<IActionResult> Reactivar(int id, CancellationToken cancellationToken = default)
        {
            return Finalizar(await _equipos.ReactivarAsync(id, cancellationToken));
        }

        private async Task<IActionResult> VolverAlFormularioAsync(FormularioEquipoViewModel modelo, CancellationToken cancellationToken)
        {
            var referencia = await _equipos.ObtenerFormularioNuevoAsync(cancellationToken);
            modelo.Categorias = referencia.Categorias;

            if (modelo.IdEquipo != 0)
            {
                var actual = await _equipos.ObtenerFormularioEdicionAsync(modelo.IdEquipo, cancellationToken);
                if (actual is null)
                {
                    return NotFound();
                }

                modelo.FotoActual = actual.FotoActual;
            }

            return View(VistaFormulario, modelo);
        }

        private IActionResult Finalizar(ResultadoOperacion resultado)
        {
            if (resultado.Tipo == TipoResultado.NoEncontrado)
            {
                return NotFound();
            }

            TempData[resultado.Exitoso ? "Success" : "Error"] = TextoCompleto(resultado);
            return RedirectToAction(nameof(Index));
        }

        private static string TextoCompleto(ResultadoOperacion resultado)
            => resultado.Aviso is null ? resultado.Mensaje : $"{resultado.Mensaje}. {resultado.Aviso}";
    }
}
