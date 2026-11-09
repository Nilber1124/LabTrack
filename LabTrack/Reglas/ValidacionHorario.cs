namespace LabTrack.Reglas
{
    public static class ValidacionHorario
    {
        public static DateTime Componer(DateOnly fecha, TimeOnly hora) => fecha.ToDateTime(hora);

        // Devuelve el mensaje de error o null si el horario es válido.
        public static string? Validar(DateTime inicio, DateTime fin, DateTime ahora)
        {
            if (fin <= inicio)
            {
                return Mensajes.FinPosteriorAInicio;
            }

            return inicio < ahora ? Mensajes.InicioEnPasado : null;
        }
    }
}
