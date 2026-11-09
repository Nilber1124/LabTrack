using System.Security.Claims;

namespace LabTrack.Controllers
{
    public static class ClaimsPrincipalExtensions
    {
        public static int? ObtenerIdUsuario(this ClaimsPrincipal usuario)
            => int.TryParse(usuario.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
    }
}
