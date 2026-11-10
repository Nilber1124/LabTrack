using LabTrack.Reglas;
using LabTrack.Services;
using LabTrack.Services.Interfaces;
using LabTrack.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Security.Claims;

namespace LabTrack.Controllers
{
    [AllowAnonymous]
    public class LoginController : Controller
    {
        private readonly IAutenticacionService _autenticacion;

        public LoginController(IAutenticacionService autenticacion)
        {
            _autenticacion = autenticacion;
        }

        [HttpGet]
        public IActionResult Index()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirigirSegunRol(User.IsInRole(NombresRol.Estudiante) ? NombresRol.Estudiante : NombresRol.Encargado);
            }

            return View(new LoginViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> Index(LoginViewModel model, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var resultado = await _autenticacion.AutenticarAsync(model.Codigo, model.Contrasena, cancellationToken);

            switch (resultado.Estado)
            {
                case EstadoAutenticacion.Exitosa:
                    await IniciarSesionAsync(resultado.Usuario!);
                    TempData["Success"] = $"Hola, {resultado.Usuario!.Nombre}";
                    return RedirigirSegunRol(resultado.Usuario.Rol);

                case EstadoAutenticacion.Bloqueada:
                    model.AlertaSeguridad = resultado.Mensaje;
                    model.BloqueadoHasta = resultado.BloqueadoHasta;
                    model.SegundosRestantes = resultado.SegundosRestantes;
                    return View(model);

                default:
                    ModelState.AddModelError(string.Empty, resultado.Mensaje);
                    return View(model);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Salir()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult Denegado()
        {
            return View();
        }

        // El manejador de excepciones redirige aquí con GET (ver Program.cs).
        [HttpGet]
        public IActionResult Error(string? id)
        {
            Response.StatusCode = StatusCodes.Status500InternalServerError;
            return View(new ErrorViewModel { RequestId = id ?? Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        private Task IniciarSesionAsync(UsuarioAutenticado usuario)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, usuario.IdUsuario.ToString()),
                new Claim(ClaimTypes.Name, usuario.Nombre),
                new Claim(ClaimTypes.Role, usuario.Rol)
            };
            var identidad = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            return HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identidad));
        }

        private IActionResult RedirigirSegunRol(string rol)
            => rol == NombresRol.Estudiante
                ? RedirectToAction("Index", "Catalogo")
                : RedirectToAction("Index", "Solicitudes");
    }
}
