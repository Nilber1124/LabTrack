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
        public const string EstudiantePenalizado = "No puedes reservar equipos por ahora";

        // HU-04
        public const string ReservaNoEncontrada = "La reserva indicada no existe";
        public const string SolicitudYaResuelta = "La solicitud ya fue resuelta. Se actualizó el listado";
        public const string MotivoObligatorio = "Escribe el motivo del rechazo";
        public const string MotivoExcedeLongitud = "El motivo no puede exceder los 200 caracteres";
        public const string ConflictoConcurrencia = "Otra persona modificó esta reserva. Inténtalo de nuevo";

        // HU-05
        public const string SoloReservasAprobadas = "Solo se puede devolver una reserva aprobada";
        public const string EstudianteNoEncontrado = "El estudiante indicado no existe";
        public const string PenalizacionNoActiva = "El estudiante no tiene una penalización activa";

        // Gestión de equipos
        public const string CodigoRepetido = "Ya existe un equipo con ese código";
        public const string LaboratorioNoValido = "Selecciona un laboratorio válido";
        public const string CategoriaNoValida = "Selecciona una categoría válida";
        public const string ImagenNoSubida = "No se pudo subir la imagen";
        public const string ImagenNoValida = "El archivo supera los 2 MB o no es JPG/PNG. Elige otra imagen o guarda el equipo sin foto.";
        public const string NoSePuedeDarDeBaja = "No se puede dar de baja";
        public const string EquipoYaDeBaja = "El equipo ya está de baja";
        public const string EquipoNoEstaDeBaja = "El equipo no está de baja";
        public const string ConflictoConcurrenciaEquipo = "Otra persona modificó este equipo. Inténtalo de nuevo";
    }
}
