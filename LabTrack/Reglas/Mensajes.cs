namespace LabTrack.Reglas
{
    // Único lugar de los textos de negocio; las pruebas reutilizan estas constantes.
    public static class Mensajes
    {
        // HU-01
        public const string CredencialesInvalidas = "Credenciales inválidas";
        public const string CuentaBloqueada = "Cuenta bloqueada por seguridad";

        // HU-02
        public const string SinEquiposEnCategoria = "No hay equipos disponibles en esta categoría";

        // HU-03
        public const string FinPosteriorAInicio = "La hora de fin debe ser posterior a la de inicio";
        public const string InicioEnPasado = "El inicio de la reserva no puede estar en el pasado";
        public const string EquipoNoEncontrado = "El equipo solicitado no existe";
        public const string EquipoNoDisponible = "Este equipo no está disponible en este momento";
        public const string ChoqueConAprobada = "Este equipo ya tiene una reserva aprobada en ese horario";
        public const string UnaReservaALaVez = "Solo puedes tener un equipo reservado a la vez";
        public const string SoloEstudiantes = "Solo los estudiantes pueden solicitar reservas";

        // HU-04
        public const string ReservaNoEncontrada = "La reserva indicada no existe";
        public const string SolicitudYaResuelta = "La solicitud ya fue resuelta. Se actualizó el listado";
        public const string MotivoObligatorio = "Escribe el motivo del rechazo";
        public const string MotivoExcedeLongitud = "El motivo no puede exceder los 200 caracteres";
        public const string ConflictoConcurrencia = "Otra persona modificó esta reserva. Inténtalo de nuevo";

        // HU-05
        public const string SoloReservasAprobadas = "Solo se puede devolver una reserva aprobada";
    }
}
