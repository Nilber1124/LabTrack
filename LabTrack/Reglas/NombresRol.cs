namespace LabTrack.Reglas
{
    public static class NombresRol
    {
        public const string Estudiante = "ESTUDIANTE";
        public const string Encargado = "ENCARGADO";
        public const string Administrador = "ADMINISTRADOR";

        // El administrador tiene las mismas funciones del encargado.
        public const string GestionLaboratorio = Encargado + "," + Administrador;
    }
}
