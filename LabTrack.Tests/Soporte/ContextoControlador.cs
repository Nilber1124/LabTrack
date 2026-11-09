using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using System.Security.Claims;

namespace LabTrack.Tests.Soporte
{
    public static class ContextoControlador
    {
        // Prepara HttpContext, sesión y TempData falsos. Devuelve el doble de autenticación.
        public static IAuthenticationService Configurar(Controller controlador, int? idUsuario = null, string? rol = null)
        {
            var autenticacion = Substitute.For<IAuthenticationService>();
            var http = new DefaultHttpContext
            {
                RequestServices = new ServiceCollection().AddSingleton(autenticacion).BuildServiceProvider(),
                Session = new SesionFalsa()
            };

            if (idUsuario is not null)
            {
                var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, idUsuario.Value.ToString()) };
                if (rol is not null)
                {
                    claims.Add(new Claim(ClaimTypes.Role, rol));
                }

                http.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Pruebas"));
            }

            controlador.ControllerContext = new ControllerContext { HttpContext = http };
            controlador.TempData = new TempDataDictionary(http, Substitute.For<ITempDataProvider>());
            controlador.Url = Substitute.For<IUrlHelper>();
            return autenticacion;
        }
    }
}
