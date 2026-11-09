namespace LabTrack.Services
{
    public sealed class ResultadoOperacion
    {
        public TipoResultado Tipo { get; private init; }
        public string Mensaje { get; private init; } = string.Empty;

        // Detalle adicional del resultado, como una penalización aplicada o la causa de un error.
        public string? Aviso { get; private init; }

        public TimeOnly? SugerenciaInicio { get; private init; }
        public TimeOnly? SugerenciaFin { get; private init; }

        public bool Exitoso => Tipo == TipoResultado.Exito;

        public static ResultadoOperacion Ok(string mensaje, string? aviso = null)
            => new() { Tipo = TipoResultado.Exito, Mensaje = mensaje, Aviso = aviso };

        public static ResultadoOperacion FalloConSugerencia(
            TipoResultado tipo, string mensaje, string aviso, TimeOnly? sugerenciaInicio, TimeOnly? sugerenciaFin)
            => new() { Tipo = tipo, Mensaje = mensaje, Aviso = aviso, SugerenciaInicio = sugerenciaInicio, SugerenciaFin = sugerenciaFin };

        public static ResultadoOperacion Fallo(TipoResultado tipo, string mensaje, string? aviso = null)
            => new() { Tipo = tipo, Mensaje = mensaje, Aviso = aviso };
    }
}
