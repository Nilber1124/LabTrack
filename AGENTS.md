# AGENTS.md — Guía de desarrollo de LabTrack

## Alcance y propósito

Estas reglas se aplican a todo el repositorio. Su objetivo es mantener una estructura uniforme y una base sencilla para desarrollar la gestión de usuarios, categorías, equipos, reservas y penalizaciones.

Respetar el alcance de cada solicitud. No implementar módulos, pantallas, migraciones ni cambios de base de datos que no formen parte del trabajo pedido. Conservar los cambios existentes del usuario. Distinguir requisitos confirmados de propuestas; no convertir suposiciones en reglas de negocio.

## Tecnología y decisiones vigentes

- ASP.NET Core MVC, C# y .NET 10.
- Entity Framework Core 10 y SQL Server.
- Vistas Razor y recursos estáticos en wwwroot.
- Contexto único: LabTrack.Data.ApplicationDbContext.
- Cadena de conexión: ConnectionStrings:DefaultConnection.
- Registrar el contexto en Program.cs mediante AddDbContext y UseSqlServer.
- Mantener los paquetes de EF Core en versiones compatibles entre sí; revisar vulnerabilidades y usar versiones corregidas compatibles.
- PostgreSQL fue una propuesta inicial. No introducir Npgsql, timestamptz ni exclusiones de rangos de PostgreSQL.
- Trabajar en develop para el trabajo actual. No cambiar de rama, publicar ni crear commits salvo que la tarea lo requiera.

## Estado al crear esta guía

ApplicationDbContext y su registro en Program.cs están preparados. Las entidades y enums que utiliza el contexto siguen pendientes; Usuario existe como clase vacía. Esto impide compilar y generar migraciones hasta completar los modelos.

La estructura descrita a continuación es el destino del proyecto, no una afirmación de que todos los módulos estén implementados. Antes de empezar una tarea, verificar el estado real de los archivos.

## Estructura del repositorio

    LabTrack/
    ├── AGENTS.md
    ├── LabTrack.slnx
    ├── NuGet.Config
    └── LabTrack/
        ├── Controllers/
        ├── Models/
        ├── ViewModels/
        ├── Services/
        │   └── Interfaces/
        ├── Data/
        │   ├── ApplicationDbContext.cs
        │   └── Configurations/         # Opcional cuando crezca el mapeo
        ├── Migrations/                # Generada por Entity Framework
        ├── Views/
        │   ├── Shared/
        │   └── <Controlador>/
        ├── wwwroot/
        │   ├── css/
        │   ├── js/
        │   └── images/
        ├── Properties/
        ├── Program.cs
        ├── appsettings.json
        └── appsettings.Development.json

Crear carpetas al necesitarlas, no archivos vacíos para simular funcionalidades. Si se agregan pruebas, ubicarlas en un proyecto LabTrack.Tests junto a la solución.

## Responsabilidades por capa

### Models

- Representar entidades persistentes, claves, relaciones y enums del dominio.
- Una entidad por archivo; nombres singulares: Usuario, Categoria, Equipo, Reserva y Penalizacion.
- No acceder a la base de datos ni incluir lógica HTTP o presentación.
- Inicializar colecciones de navegación. Usar nullable solo para datos realmente opcionales.
- No persistir el estado Disponible/Prestado de un equipo: se calcula desde las reservas.
- Mantener PasswordHash; nunca una propiedad para guardar contraseñas en texto plano.

### ViewModels

- Definir los datos de entrada y salida de cada pantalla.
- Separar formularios de creación, edición y listados cuando tengan campos diferentes.
- Usar nombres como CrearReservaViewModel y EquipoListadoViewModel.
- Declarar validaciones de entrada con DataAnnotations y mensajes en español.
- No recibir entidades completas desde formularios para modificar campos sensibles.
- No exponer hashes, intentos de acceso u otros datos internos en las vistas.
- ErrorViewModel existe actualmente en Models; moverlo solo si la tarea incluye esa reorganización.

### Controllers

- Recibir dependencias por constructor.
- Coordinar la entrada, ModelState, el servicio y el resultado HTTP o la vista.
- Mantener las reglas de negocio fuera del controlador.
- Permitir consultas sencillas al contexto para lecturas; usar servicios para operaciones de negocio.
- Usar acciones GET para consultar y POST para modificar desde formularios MVC.
- Proteger acciones de modificación con ValidateAntiForgeryToken.
- Usar ViewModels tipados; evitar ViewBag para datos esenciales.
- Devolver errores adecuados y mensajes útiles sin revelar detalles internos.

### Services

- Implementar los casos de uso y las reglas de reservas, devoluciones y penalizaciones.
- Definir interfaces cuando faciliten la separación del controlador y las pruebas.
- Inyectar ApplicationDbContext; registrar los servicios que lo utilizan con alcance Scoped.
- Coordinar transacciones y tratar conflictos de concurrencia.
- No depender de vistas, TempData, HttpContext ni respuestas HTTP.
- No agregar repositorios genéricos ni capas adicionales sin una necesidad concreta.

### Data y Migrations

- ApplicationDbContext declara DbSet y configura el modelo mediante Fluent API.
- Usar Fluent API como fuente de verdad para tablas, tipos, longitudes, claves, relaciones, índices y valores predeterminados.
- No duplicar configuraciones contradictorias en atributos y Fluent API.
- Cuando el mapeo crezca, extraer clases IEntityTypeConfiguration<T> a Data/Configurations y registrarlas desde el contexto.
- No usar el contexto como un servicio de negocio ni construirlo con new en los controladores.
- No agregar otro DbContext vacío o con nombre ambiguo.
- Mantener el contexto Scoped; no compartir una instancia entre hilos ni ejecutar operaciones paralelas sobre ella.
- No cambiar el esquema automáticamente al iniciar la aplicación.

### Views y wwwroot

- Vistas Razor tipadas, organizadas por controlador; elementos comunes en Views/Shared.
- Usar Tag Helpers y los archivos compartidos de validación.
- Mantener las validaciones del servidor aunque exista validación del navegador.
- Separar CSS y JavaScript por funcionalidad cuando sea necesario.
- No consultar EF Core ni implementar reglas de negocio en Razor o JavaScript.
- Mantener la codificación de salida de Razor; evitar Html.Raw con contenido del usuario.

## Convenciones de C#

- Usar cuatro espacios, UTF-8 y formato consistente con el código existente.
- PascalCase para clases, propiedades y métodos; camelCase para parámetros y variables; _camelCase para campos privados.
- Nombres del dominio en español, sin tildes en identificadores. Mantener nombres de APIs del framework.
- Namespaces correspondientes a carpetas, por ejemplo LabTrack.Services.
- Usar namespaces de archivo en archivos nuevos y evitar reformateos ajenos a la tarea.
- Mantener nullable habilitado; no ocultar advertencias mediante desactivación global.
- Usar null! solo cuando exista una garantía documentable, como una navegación requerida que materializa EF.
- Métodos de acceso a datos asíncronos con sufijo Async y CancellationToken cuando corresponda.
- Usar await; no bloquear con .Result o .Wait().
- Preferir métodos pequeños y nombres claros. Comentar decisiones y motivos, no repetir lo evidente.
- Evitar catch vacíos, excepciones silenciadas y valores mágicos. Registrar errores con ILogger<T>.
- No usar async void salvo eventos que lo requieran.

## Contrato del modelo de datos

Mantener estos nombres y tipos salvo un cambio explícito de requisitos:

| Entidad / tabla | Clave | Campos y restricciones principales |
| --- | --- | --- |
| Usuario / usuarios | Id: long, IDENTITY | Codigo: 20, único; Nombre: 120; PasswordHash: 255; los tres requeridos |
| Categoria / categorias | Id: short, IDENTITY | Nombre: 50, requerido y único |
| Equipo / equipos | Id: long, IDENTITY | CodigoInventario: 30, único; Nombre: 120; ambos requeridos; CategoriaId: short |
| Reserva / reservas | Id: long, IDENTITY | EstudianteId y EquipoId: long; Inicio y Fin: DateTimeOffset; MotivoRechazo: string? de 255; ResueltaPor: long? |
| Penalizacion / penalizaciones | Id: long, IDENTITY | ReservaId: long, requerido y único; CreadaEn: DateTime; LevantadaEn: DateTime? |

- RolUsuario: Estudiante y Encargado; predeterminado Estudiante.
- EstadoUsuario: Activo y Penalizado; predeterminado Activo.
- EstadoReserva: Pendiente, Aprobada, Rechazada y Devuelta; predeterminado Pendiente.
- Persistir los enums como texto y restringir sus valores válidos.
- IntentosFallidos es short, no negativo y predeterminado 0.
- BloqueadoHasta es DateTimeOffset? y se mapea a datetimeoffset.
- Inicio y Fin se mapean a datetimeoffset; Fin debe ser posterior a Inicio.
- ResueltaEn y DevueltaEn son DateTime?; CreadaEn es DateTime.
- Los DateTime de auditoría se guardan en UTC como datetime2; usar SYSUTCDATETIME() para las fechas de creación generadas por SQL Server.
- datetime2 no conserva zona horaria ni DateTime.Kind: interpretar esas fechas como UTC al materializarlas y convertirlas para presentación.
- LevantadaEn se mapea expresamente a la columna Levantada_En.
- Usar índices en claves foráneas e índices adicionales justificados por consultas reales.

### Relaciones y navegaciones

- Categoria.Equipos ↔ Equipo.Categoria, FK CategoriaId.
- Usuario.ReservasComoEstudiante ↔ Reserva.Estudiante, FK EstudianteId.
- Usuario.ReservasResueltas ↔ Reserva.EncargadoResolutor, FK opcional ResueltaPor.
- Equipo.Reservas ↔ Reserva.Equipo, FK EquipoId.
- Reserva.Penalizacion ↔ Penalizacion.Reserva, FK única ReservaId.
- Configurar explícitamente ambas relaciones entre Usuario y Reserva.
- Mantener borrado restringido para preservar el historial; no activar cascadas sin analizar su efecto.

## Reglas de negocio y concurrencia

- Comprobar en el servicio que EstudianteId pertenece a un estudiante y que quien resuelve tiene rol Encargado. Una FK por sí sola no valida roles.
- No aceptar del formulario el usuario que ejecuta una operación; obtenerlo de la identidad autenticada.
- Autorizar tanto el rol como el acceso al recurso. Ocultar un botón no constituye autorización.
- Definir y validar las transiciones de reserva antes de implementarlas. No inventar duraciones de bloqueo, plazos de préstamo o políticas de penalización.
- La restricción Fin > Inicio no impide reservas superpuestas.
- Para intervalos [Inicio, Fin), hay solapamiento cuando existente.Inicio < nuevo.Fin y nuevo.Inicio < existente.Fin.
- Confirmar qué estados bloquean disponibilidad antes de implementar esa política.
- SQL Server no proporciona las exclusiones de rangos de PostgreSQL: al implementar la aprobación, proteger la comprobación y escritura con una estrategia transaccional que impida carreras entre solicitudes.
- Una consulta AnyAsync seguida de SaveChangesAsync sin protección de concurrencia no basta.
- Hacer atómicas las operaciones que deban persistirse juntas, como devolución y penalización.
- Tratar los conflictos con mensajes controlados; respaldar las reglas con restricciones e índices siempre que sea posible.

## Consultas y persistencia

- Usar AsNoTracking en lecturas que no se modificarán y Select para proyectar solo los datos necesarios.
- Evitar consultas N+1, cargas completas para filtrar en memoria y listados sin límites.
- Paginar los listados que puedan crecer.
- No concatenar entrada del usuario en SQL; usar LINQ o parámetros.
- Aplicar cambios a entidades cargadas y a campos permitidos; evitar Update indiscriminado de entidades recibidas.
- No duplicar SaveChangesAsync por cada propiedad ni persistir operaciones relacionadas parcialmente.

## Seguridad y configuración

- Usar PasswordHasher<Usuario> o una solución estándar equivalente para crear y verificar hashes.
- No crear usuarios con contraseñas fijas en migraciones o semillas.
- No registrar contraseñas, hashes ni cadenas con credenciales.
- Guardar secretos en User Secrets o variables de entorno; no agregarlos al repositorio.
- DefaultConnection puede sobrescribirse con ConnectionStrings__DefaultConnection.
- Integrated Security usa la identidad del proceso; verificar permisos al ejecutar o desplegar.
- TrustServerCertificate=True corresponde al entorno local actual; para producción configurar certificado válido.
- Revisar vulnerabilidades NuGet, incluidas dependencias transitivas, sin actualizar paquetes ajenos indiscriminadamente.
- Al implementar autenticación por cookies, registrar autenticación y colocar UseAuthentication antes de UseAuthorization.
- No afirmar que la autenticación ya existe por el solo hecho de tener una entidad Usuario.

## Flujo de migraciones

1. Completar entidades y mapeos; comprobar que el proyecto compile.
2. Revisar migraciones existentes y el snapshot antes de generar una nueva.
3. Generar con EF Core; usar la carpeta Migrations para nuevas migraciones.
4. Revisar Up, Down, índices, tipos y cualquier posible pérdida de datos.
5. Aplicar a la base de datos solo cuando esté dentro del alcance solicitado.
6. Entregar conjuntamente el modelo, la migración y el snapshot cuando la tarea incluya un cambio de esquema.

Consola del Administrador de paquetes de Visual Studio, con LabTrack como proyecto predeterminado y proyecto de inicio:

    Add-Migration NombreDescriptivo -Context ApplicationDbContext -OutputDir Migrations
    Update-Database -Context ApplicationDbContext

Equivalente desde la raíz del repositorio:

    dotnet ef migrations add NombreDescriptivo --project LabTrack/LabTrack.csproj --context ApplicationDbContext --output-dir Migrations
    dotnet ef database update --project LabTrack/LabTrack.csproj --context ApplicationDbContext

- No regenerar una migración inicial si ya existe una válida.
- No eliminar ni reescribir migraciones aplicadas o compartidas; crear una nueva.
- No editar manualmente el snapshot para simular cambios.
- No mezclar EnsureCreated con un flujo basado en migraciones.
- Generar una migración no necesita conexión; aplicarla sí.
- Si faltan modelos o acceso al servidor, informar la limitación con precisión.

## Validación y entrega

- Revisar los cambios existentes antes de editar y limitar el diff a la tarea.
- Ejecutar dotnet build sobre LabTrack/LabTrack.csproj para cambios de código cuando sea posible.
- Agregar pruebas para reglas de negocio, autorización y concurrencia al implementar esas funcionalidades.
- Probar restricciones, transacciones y migraciones con SQL Server; EF InMemory no demuestra su comportamiento.
- Cubrir especialmente reservas simultáneas, fechas inválidas, acceso a recursos ajenos y doble penalización.
- Los cambios documentales no requieren compilar ni generar migraciones.
- Informar qué se cambió, qué se verificó y qué falta; no afirmar que un comando pasó si no se ejecutó o falló.
- Mantener esta guía actualizada cuando cambien las decisiones arquitectónicas; conservar el historial en Git.
