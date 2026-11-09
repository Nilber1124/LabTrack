using System.Globalization;

namespace LabTrack.ViewModels
{
    // Presentación de fechas según los mockups, independiente de la cultura del servidor.
    public static class Formato
    {
        private static readonly CultureInfo Peru = CultureInfo.GetCultureInfo("es-PE");

        public static string Fecha(DateTime fecha)
        {
            var texto = fecha.ToString("ddd d MMM yyyy", Peru).Replace(".", string.Empty);
            return char.ToUpper(texto[0], Peru) + texto[1..];
        }

        public static string Hora(DateTime fecha) => fecha.ToString("HH:mm", CultureInfo.InvariantCulture);

        public static string Horario(DateTime inicio, DateTime fin) => $"{Hora(inicio)} – {Hora(fin)}";

        public static string FechaYHorario(DateTime inicio, DateTime fin) => $"{Fecha(inicio)} · {Horario(inicio, fin)}";

        public static string Duracion(TimeSpan duracion)
        {
            var minutos = (int)Math.Ceiling(Math.Abs(duracion.TotalMinutes));
            return minutos < 60 ? $"{minutos} min" : $"{minutos / 60} h {minutos % 60} min";
        }
    }
}
