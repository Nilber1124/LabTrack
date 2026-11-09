using System.Globalization;

namespace LabTrack.ViewModels
{
    public class BloqueOcupadoViewModel
    {
        private const double HoraApertura = 7;
        private const double HorasVisibles = 14;

        public DateTime Inicio { get; set; }
        public DateTime Fin { get; set; }

        public string Horario => Formato.Horario(Inicio, Fin);
        public string Izquierda => Porcentaje(Inicio);
        public string Ancho => (Posicion(Fin) - Posicion(Inicio)).ToString("0.##", CultureInfo.InvariantCulture) + "%";

        private static string Porcentaje(DateTime momento)
            => Posicion(momento).ToString("0.##", CultureInfo.InvariantCulture) + "%";

        private static double Posicion(DateTime momento)
            => Math.Clamp((momento.TimeOfDay.TotalHours - HoraApertura) / HorasVisibles * 100, 0, 100);
    }
}
