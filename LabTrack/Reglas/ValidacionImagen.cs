namespace LabTrack.Reglas
{
    public static class ValidacionImagen
    {
        public const long TamanoMaximoBytes = 2 * 1024 * 1024;
        public const int BytesDeCabecera = 8;

        private static readonly byte[] FirmaJpeg = [0xFF, 0xD8, 0xFF];
        private static readonly byte[] FirmaPng = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

        public static bool TamanoValido(long bytes) => bytes is > 0 and <= TamanoMaximoBytes;

        // Se decide por el contenido real del archivo: el nombre y el tipo los controla quien lo envía.
        public static string? DetectarExtension(ReadOnlySpan<byte> cabecera)
        {
            if (cabecera.StartsWith(FirmaJpeg))
            {
                return ".jpg";
            }

            return cabecera.StartsWith(FirmaPng) ? ".png" : null;
        }
    }
}
