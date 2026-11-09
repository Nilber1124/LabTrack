namespace LabTrack.Services
{
    public sealed class ResultadoOperacion
    {
        public TipoResultado Tipo { get; private init; }
        public string Mensaje { get; private init; } = string.Empty;

        // Información adicional de un resultado correcto, como una penalización aplicada.
        public string? Aviso { get; private init; }

        public bool Exitoso => Tipo == TipoResultado.Exito;

        public static ResultadoOperacion Ok(string mensaje, string? aviso = null)
            => new() { Tipo = TipoResultado.Exito, Mensaje = mensaje, Aviso = aviso };

        public static ResultadoOperacion Fallo(TipoResultado tipo, string mensaje)
            => new() { Tipo = tipo, Mensaje = mensaje };
    }
}
