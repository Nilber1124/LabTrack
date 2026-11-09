using System.Text.RegularExpressions;

namespace LabTrack.Reglas
{
    public static partial class RutaCloudinary
    {
        private const string Dominio = "res.cloudinary.com";
        private const string Marca = "/upload/";

        // Obtiene el identificador con el que Cloudinary borra una imagen a partir de su URL.
        // Devuelve null si la URL no es de Cloudinary.
        public static string? ObtenerPublicId(string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Host != Dominio)
            {
                return null;
            }

            var posicion = uri.AbsolutePath.IndexOf(Marca, StringComparison.Ordinal);
            if (posicion < 0)
            {
                return null;
            }

            var ruta = Version().Replace(uri.AbsolutePath[(posicion + Marca.Length)..], string.Empty);
            var punto = ruta.LastIndexOf('.');
            var publicId = punto > 0 ? ruta[..punto] : ruta;

            return publicId.Length == 0 ? null : publicId;
        }

        [GeneratedRegex(@"^v\d+/")]
        private static partial Regex Version();
    }
}
