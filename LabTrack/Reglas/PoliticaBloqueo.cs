namespace LabTrack.Reglas
{
    public static class PoliticaBloqueo
    {
        public const int MaximoIntentos = 3;

        public static bool EstaBloqueada(DateTime? bloqueadoHasta, DateTime ahora)
            => bloqueadoHasta > ahora;

        public static bool BloqueoVencido(DateTime? bloqueadoHasta, DateTime ahora)
            => bloqueadoHasta <= ahora;

        public static bool AlcanzoLimite(int intentosFallidos)
            => intentosFallidos >= MaximoIntentos;
    }
}
