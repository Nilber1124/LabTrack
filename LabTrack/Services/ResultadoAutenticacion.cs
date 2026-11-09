using LabTrack.Reglas;

namespace LabTrack.Services
{
    public sealed class ResultadoAutenticacion
    {
        public EstadoAutenticacion Estado { get; private init; }
        public string Mensaje { get; private init; } = string.Empty;
        public UsuarioAutenticado? Usuario { get; private init; }
        public DateTime? BloqueadoHasta { get; private init; }
        public int SegundosRestantes { get; private init; }

        public static ResultadoAutenticacion Exitosa(UsuarioAutenticado usuario)
            => new() { Estado = EstadoAutenticacion.Exitosa, Usuario = usuario };

        public static ResultadoAutenticacion Invalida()
            => new() { Estado = EstadoAutenticacion.CredencialesInvalidas, Mensaje = Mensajes.CredencialesInvalidas };

        public static ResultadoAutenticacion Bloqueada(DateTime bloqueadoHasta, DateTime ahora)
            => new()
            {
                Estado = EstadoAutenticacion.Bloqueada,
                Mensaje = Mensajes.CuentaBloqueada,
                BloqueadoHasta = bloqueadoHasta,
                SegundosRestantes = (int)Math.Ceiling((bloqueadoHasta - ahora).TotalSeconds)
            };
    }
}
