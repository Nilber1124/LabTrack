namespace LabTrack.Models
{
    public class Estudiante
    {
        public int Id { get; set; }

        public string CodigoEstudiante { get; set; } = string.Empty;

        public string Nombre { get; set; } = string.Empty;

        public string PasswordHash { get; set; } = string.Empty;

        public int IntentosFallidos { get; set; } = 0;

        public DateTime? BloqueadoHasta { get; set; }
    }
}