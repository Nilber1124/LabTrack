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
        public string Descripcion { get; }
    }
}
