using System.Text.RegularExpressions;

namespace LabTrack.Reglas
{
    public static partial class RutaCloudinary
    {
        private const string Dominio = "res.cloudinary.com";
        private const string Marca = "/upload/";

        // Llena el recuadro de las tarjetas centrándose en el objeto y entrega un archivo liviano.
        private const string Transformacion = "c_fill,ar_2:1,w_900,g_auto/f_auto,q_auto/";

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

        // Devuelve la URL de la miniatura; las direcciones que no son de Cloudinary no cambian.
        public static string Miniatura(string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Host != Dominio)
            {
                return url;
            }

            var posicion = url.IndexOf(Marca, StringComparison.Ordinal);
            return posicion < 0 ? url : url.Insert(posicion + Marca.Length, Transformacion);
        }

        [GeneratedRegex(@"^v\d+/")]
        private static partial Regex Version();
    }
}
