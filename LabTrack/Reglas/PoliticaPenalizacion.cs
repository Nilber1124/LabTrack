namespace LabTrack.Reglas
{
    public static class PoliticaPenalizacion
    {
        // Devolver exactamente a la hora de fin no penaliza.
        public static bool DebePenalizar(DateTime fin, DateTime ahora) => ahora > fin;
    }
}
