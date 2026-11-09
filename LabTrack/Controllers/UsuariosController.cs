using LabTrack.Reglas;
using LabTrack.Services;
using LabTrack.Services.Interfaces;
using LabTrack.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LabTrack.Controllers
{
    [Authorize(Roles = NombresRol.Administrador)]
    public class UsuariosController : Controller
    {
        private const string VistaFormulario = "Formulario";

        private readonly IUsuarioService _usuarios;

        public UsuariosController(IUsuarioService usuarios)
        {
            _usuarios = usuarios;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? busqueda, string? rol, int pagina = 1, CancellationToken cancellationToken = default)
        {
            return View(await _usuarios.ListarAsync(busqueda, rol, pagina, cancellationToken));
        }

        // Nuevo y Editar son la alternativa sin JavaScript al modal de la lista.
        [HttpGet]
        public IActionResult Nuevo()
        {
            return View(VistaFormulario, new FormularioUsuarioViewModel());
        }

        [HttpGet]
        public async Task<IActionResult> Editar(int id, CancellationToken cancellationToken = default)
        {
            var formulario = await _usuarios.ObtenerFormularioEdicionAsync(id, cancellationToken);
            return formulario is null ? NotFound() : View(VistaFormulario, formulario);
        }

        [HttpPost]
        public async Task<IActionResult> Guardar(FormularioUsuarioViewModel modelo, CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid)
            {
                return View(VistaFormulario, modelo);
            }

            var resultado = await _usuarios.GuardarAsync(new DatosUsuario
            {
                IdUsuario = modelo.IdUsuario,
                Codigo = modelo.Codigo,
                Nombre = modelo.Nombre,
                Rol = modelo.Rol,
                Contrasena = modelo.Contrasena
            }, cancellationToken);

            if (resultado.Exitoso)
            {
                TempData["Success"] = resultado.Mensaje;
                return RedirectToAction(nameof(Index));
            }

            if (resultado.Tipo == TipoResultado.NoEncontrado)
            {
                return NotFound();
            }

            var campo = resultado.Mensaje == Mensajes.CodigoUsuarioRepetido ? nameof(modelo.Codigo) : string.Empty;
            ModelState.AddModelError(campo, TextoCompleto(resultado));
            return View(VistaFormulario, modelo);
        }

        [HttpPost]
        public async Task<IActionResult> DarDeBaja(int id, CancellationToken cancellationToken = default)
        {
            return Finalizar(await _usuarios.DarDeBajaAsync(id, cancellationToken));
        }

        [HttpPost]
        public async Task<IActionResult> Reactivar(int id, CancellationToken cancellationToken = default)
        {
            return Finalizar(await _usuarios.ReactivarAsync(id, cancellationToken));
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
