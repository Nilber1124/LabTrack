namespace LabTrack.Services
{
    public sealed class CloudinaryOpciones
    {
        public const string Seccion = "Cloudinary";

        public string? CloudName { get; set; }
        public string? ApiKey { get; set; }
        public string? ApiSecret { get; set; }

        public bool EstaConfigurado =>
            !string.IsNullOrWhiteSpace(CloudName)
            && !string.IsNullOrWhiteSpace(ApiKey)
            && !string.IsNullOrWhiteSpace(ApiSecret);
    }
}
