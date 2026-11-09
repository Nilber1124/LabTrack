# AGENTS.md — LabTrack

LabTrack gestiona el préstamo de equipos de laboratorio (curso Calidad y Pruebas de Software). Cada criterio de aceptación debe demostrarse con una prueba unitaria, salvo CA-07 (JMeter).

Fuentes de verdad, en orden: historias de usuario, mockups, este archivo. Si chocan o falta algo, preguntar; no inventar campos, estados ni reglas. Si un mockup contradice una HU, gana la HU. La estructura descrita es el destino: comprobar el estado real antes de cada tarea.

## Stack y estructura

.NET 10, ASP.NET Core MVC con Razor, EF Core 10 (SQL Server, Code-First), autenticación por cookie y sesión (TempData). Pruebas con xUnit.

```
LabTrack/        Controllers  Data/AppDbContext.cs  Migrations  Models  ViewModels
                 Reglas  Services (interfaces + implementaciones)  ViewComponents
                 Views/<Controlador>  Views/Shared  wwwroot/css  wwwroot/js
LabTrack.Tests/  xUnit, solo unitarias; espeja las carpetas del proyecto web
tests/performance/   plan JMeter (solo CA-07)
docs/            plan-de-pruebas.md  matriz-trazabilidad.md
.github/workflows/   CI
```

Paquetes permitidos: EF Core (SqlServer), CloudinaryDotNet, xUnit, NSubstitute, Microsoft.Extensions.TimeProvider.Testing y coverlet. Cualquier otro se justifica antes.

## Comandos

```bash
dotnet build LabTrack.slnx
dotnet test LabTrack.Tests/LabTrack.Tests.csproj
dotnet test LabTrack.Tests/LabTrack.Tests.csproj /p:CollectCoverage=true /p:Threshold=80 /p:ThresholdType=line /p:Include="[LabTrack]LabTrack.Reglas.*%2c[LabTrack]LabTrack.Services.*"
dotnet ef migrations add NombreDescriptivo --project LabTrack/LabTrack.csproj --context AppDbContext --output-dir Migrations
jmeter -n -t tests/performance/catalogo.jmx -l tests/performance/resultados/resultado.jtl -e -o tests/performance/resultados/reporte
```

Las pruebas de servicios leen la conexión de la variable `LABTRACK_TEST_SQLSERVER` (nunca en el repositorio) y fallan con un mensaje claro si no hay servidor.

`ConnectionStrings:DefaultConnection` está en `appsettings.json`; ajustar `Data Source` al servidor local. Los datos sensibles van en un archivo `.env` en la raíz (ignorado por Git; plantilla en `.env.example`), con `__` en lugar de `:`, y en Azure como variables de entorno. Las fotos necesitan `Cloudinary:{CloudName,ApiKey,ApiSecret}`. En Development, los usuarios de demostración van en User Secrets del proyecto web: `Semilla:Encargado:{Codigo,Nombre,Contrasena}`, `Semilla:Administrador:{Codigo,Nombre,Contrasena}`, `Semilla:Estudiantes:0:{Codigo,Nombre,Contrasena}` y `Semilla:ContrasenaDemostracion` (estudiantes de simulación).

## Reglas de datos

- Un único `AppDbContext`, un `DbSet<T>` por entidad; el nombre del DbSet es el de la tabla (plural, sin `ToTable`). Sin modelos sin `DbSet`.
- Claves `Id<Entidad>` (int, identidad). Navegaciones en PascalCase.
- `Rol` (ESTUDIANTE, ENCARGADO, ADMINISTRADOR); `Usuario` (Codigo único, Nombre, hash de contraseña, IdRol, Estado, IntentosFallidos, BloqueadoHasta, DeBaja); `Categoria` (Cómputo, Electrónica, Redes); `Equipo` (Nombre, CodigoInventario único, IdCategoria, Laboratorio, FotoUrl, DeBaja, Version); `Reserva` (IdUsuario, IdEquipo, Inicio, Fin, Estado, MotivoRechazo de 200, FechaDevolucionReal, FechaSolicitud, Version).
- Enums como texto: `EstadoReserva {Pendiente, Aprobada, Rechazada, Devuelta}`, `EstadoUsuario {Activo, Penalizado}`.
- Relaciones y `OnDelete` explícitos en `OnModelCreating`: `Restrict` hacia equipos y usuarios con historial; `SetNull` en referencias opcionales.
- Índices: únicos en `Usuario.Codigo` y `Equipo.CodigoInventario`; compuesto `Reserva (IdEquipo, Inicio, Fin)`; `Equipo.IdCategoria`; único filtrado de `Reserva.IdUsuario` para estados Pendiente y Aprobada (CA-10).
- `HasData` solo para `Rol` y `Categoria`. Datos de demostración con `DbSeeder` en Development, idempotente, con hash real y contraseñas de configuración.
- Modelos con Data Annotations y mensajes en español; `[ValidateNever]` en navegaciones.
- Cada cambio de modelo genera una migración descriptiva. El arranque ejecuta `Database.Migrate()`.

## Capas

- Controladores delgados que dependen de interfaces inyectadas: `IAutenticacionService`, `ICatalogoService`, `IReservaService`, `IDevolucionService`, `IEquipoService`, `IUsuarioService`. Sin capa de repositorios: los servicios usan `AppDbContext`.
- `LoginController` (HU-01), `CatalogoController` (HU-02), `ReservasController` (HU-03 y "Mis reservas"), `SolicitudesController` (HU-04), `DevolucionesController` y `PenalizadosController` (HU-05), `EquiposController` y `UsuariosController` (solo administrador).
- `AutenticacionService` CA-01 a 03; `CatalogoService` CA-04 a 06; `ReservaService` CA-08 a 13; `DevolucionService` CA-14 a 16 y penalizados; `EquipoService` gestión de equipos. Las fotos se guardan en Cloudinary mediante `IAlmacenImagenes`.
- `Reglas/`: clases pequeñas y deterministas, sin base de datos ni reloj propio (cruce de horarios, validación de fechas, política de bloqueo, política de penalización). Los servicios las orquestan con `AppDbContext` y `TimeProvider`.
- Los errores de negocio se devuelven como resultados tipados (`ResultadoOperacion`), no como excepciones de control de flujo.
- ViewModels en `ViewModels/`; no se envían entidades EF a vistas con lógica. Reciben Fecha + HoraInicio + HoraFin y el servicio compone `Inicio` y `Fin`.
- Post/Redirect/Get con `TempData["Success"]` y `TempData["Error"]` en los POST correctos. Errores de validación o de negocio en un formulario: volver a mostrarlo con el error.
- Lecturas con `AsNoTracking()` y proyección a ViewModel; filtros y agregaciones en la base de datos.
- `ViewComponent` para widgets compartidos del layout.
- Los mensajes exigidos ("Credenciales inválidas", "No hay equipos disponibles en esta categoría") son constantes en un solo lugar y se reutilizan en las pruebas.

## Decisiones de diseño

- Cruce: `a.Inicio < b.Fin && b.Inicio < a.Fin`. Horarios contiguos no se cruzan. Se exige `Fin > Inicio` e inicio no pasado. Solo las reservas Aprobada bloquean horario.
- La hora se obtiene solo de un `TimeProvider` inyectado con zona America/Lima, nunca de `DateTime.Now`. `Inicio`, `Fin` y demás fechas son `DateTime` en hora de Lima.
- Bloqueo: 3 fallos consecutivos bloquean 15 minutos (`Seguridad:MinutosBloqueo`). El contador se reinicia con un login exitoso y al vencer el bloqueo.
- CA-10: un estudiante tiene como máximo 1 reserva Pendiente o Aprobada sin devolver.
- Penalizado: no puede reservar hasta que un encargado levante la penalización en `PenalizadosController`. La fecha y el motivo salen de su devolución tardía más reciente; no se registra quién la levanta.
- El estado del equipo se calcula, no se guarda: `EstadoEquipo {Disponible, Prestado}` es Prestado si existe una reserva Aprobada sin devolver cuyo inicio ya pasó, incluidas las vencidas.
- Solo se puede elegir un equipo Disponible ahora; la validación de cruces siempre consulta las reservas aprobadas.
- Devolver exactamente a la hora de fin no penaliza; solo se penaliza si la hora actual es estrictamente posterior.
- Concurrencia: crear y aprobar revalidan dentro de una transacción que bloquea la fila del equipo con `UPDLOCK, HOLDLOCK`. `Reserva` y `Equipo` tienen token `rowversion` (`Version`). Los conflictos devuelven un resultado controlado.
- Aprobar es una sola operación atómica.
- La gestión de usuarios crea y da de baja estudiantes y encargados; los administradores no se gestionan ahí. El rol solo se elige al crear. Un usuario con reserva activa no se puede dar de baja y la baja cierra su sesión de inmediato.
- Un equipo Prestado no se puede dar de baja. La baja lo oculta del catálogo, conserva su historial y es reversible.
- Del mockup se implementa lo que sostiene un CA y lo que es solo presentación; se omite lo que exige datos o reglas nuevas.

## Seguridad

- `PasswordHasher<Usuario>`; nunca texto plano. Mismo mensaje para código inexistente y contraseña errónea.
- Política por defecto que exige usuario autenticado; solo `LoginController` usa `[AllowAnonymous]`. `[Authorize(Roles = "ESTUDIANTE")]` en catálogo y reservas; `[Authorize(Roles = "ENCARGADO,ADMINISTRADOR")]` en solicitudes, devoluciones y penalizados; `[Authorize(Roles = "ADMINISTRADOR")]` en equipos y usuarios.
- `AutoValidateAntiforgeryTokenAttribute` y `ResponseCache(NoStore = true)` como filtros globales. Cookie `HttpOnly` con expiración deslizante.
- `UseDeveloperExceptionPage` solo en Development; página de error genérica en producción.
- La cadena de conexión solo puede estar en el repositorio si no lleva contraseña (`Integrated Security`). Cualquier cadena con credenciales va en `.env` en local y los datos semilla en User Secrets; en Azure, variables de entorno.
- El usuario que ejecuta una operación se obtiene de los claims, nunca del formulario.

## Convenciones

- Código, comentarios y mensajes de usuario en español. Namespace raíz `LabTrack`; un archivo por clase.
- Cambios pequeños y revisables; no mezclar refactor y funcionalidad nueva en un paso.
- Ramas `feature/<modulo>` desde `develop`; commits en español.

## Pruebas (solo unitarias)

- **No avanzar de HU con pruebas en rojo:** al terminar cada HU ejecutar `dotnet build` y `dotnet test`.
- Una unidad aislada: sin servidor web, HTTP, `WebApplicationFactory`, red ni archivos; la única excepción es el SQL Server de las pruebas de servicios. Arrange-Act-Assert, un comportamiento por prueba, independientes y deterministas. Reloj siempre con `FakeTimeProvider`.
- Nombre `Metodo_Condicion_ResultadoEsperado`, con `[Trait("HU","HU-0X")]` y `[Trait("CA","CA-XX")]`.
- Reglas: directas, sin dobles; aquí van los valores límite.
- Servicios: `AppDbContext` sobre SQL Server real (nunca InMemory ni SQLite), en una base temporal `LabTrack_Pruebas_<GUID>` creada con las migraciones, limpiada antes de cada prueba y eliminada al terminar. Nunca sobre la base `LabTrack`.
- Controladores: servicios con NSubstitute, `DefaultHttpContext`, sesión y `TempData` falsos; se verifica el `IActionResult`. No se renderizan vistas.
- Modelos y ViewModels: `Validator.TryValidateObject`, casos válidos e inválidos.
- Mapeo: sobre `context.Model`, sin consultas.
- Seguridad: por reflexión, `[Authorize]` y roles de cada controlador; `LoginController` es el único anónimo.
- Las vistas se verifican a mano con la lista de `docs/plan-de-pruebas.md`.
- Meta: los 15 CA verificables con al menos una prueba y 80 % de líneas en `Reglas/` y `Services/`.
