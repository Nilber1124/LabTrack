using LabTrack.Reglas;

namespace LabTrack.ViewModels
{
    public class FotoEquipoViewModel
    {
        public FotoEquipoViewModel(string? url, string descripcion)
        {
            Url = url;
            Descripcion = descripcion;
        }

        public string? Url { get; }
        public string? Miniatura => Url is null ? null : RutaCloudinary.Miniatura(Url);
        public string Descripcion { get; }
    }
}
