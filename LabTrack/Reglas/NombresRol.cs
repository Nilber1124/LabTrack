namespace LabTrack.Reglas
{
    public static class NombresRol
    {
        public const string Estudiante = "ESTUDIANTE";
        public const string Encargado = "ENCARGADO";
        public const string Administrador = "ADMINISTRADOR";

        // El administrador tiene las mismas funciones del encargado.
        public const string GestionLaboratorio = Encargado + "," + Administrador;

        // Roles que se pueden crear desde la gestión de usuarios.
        public static readonly IReadOnlyList<string?> Asignables = [Estudiante, Encargado];
    }
}
