namespace LabTrack.Services
{
    public sealed class ImagenSubida
    {
        public ImagenSubida(Stream contenido, long bytes)
        {
            Contenido = contenido;
            Bytes = bytes;
        }

        public Stream Contenido { get; }
        public long Bytes { get; }
    }
}
