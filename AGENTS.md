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

Paquetes permitidos: EF Core (SqlServer), xUnit, NSubstitute, Microsoft.Extensions.TimeProvider.Testing y coverlet. Cualquier otro se justifica antes.

## Comandos

```bash
dotnet build LabTrack.slnx
dotnet test LabTrack.Tests/LabTrack.Tests.csproj
dotnet test LabTrack.Tests/LabTrack.Tests.csproj /p:CollectCoverage=true /p:Threshold=80 /p:ThresholdType=line /p:Include="[LabTrack]LabTrack.Reglas.*%2c[LabTrack]LabTrack.Services.*"
dotnet ef migrations add NombreDescriptivo --project LabTrack/LabTrack.csproj --context AppDbContext --output-dir Migrations
jmeter -n -t tests/performance/catalogo.jmx -l tests/performance/resultados/resultado.jtl -e -o tests/performance/resultados/reporte
```

Las pruebas de servicios leen la conexión de la variable `LABTRACK_TEST_SQLSERVER` (nunca en el repositorio) y fallan con un mensaje claro si no hay servidor.

`ConnectionStrings:DefaultConnection` está en `appsettings.json`; ajustar `Data Source` al servidor local. En Development, los usuarios de demostración van en User Secrets del proyecto web: `Semilla:Encargado:{Codigo,Nombre,Contrasena}` y `Semilla:Estudiantes:0:{Codigo,Nombre,Contrasena}`.

## Reglas de datos

- Un único `AppDbContext`, un `DbSet<T>` por entidad; el nombre del DbSet es el de la tabla (plural, sin `ToTable`). Sin modelos sin `DbSet`.
- Claves `Id<Entidad>` (int, identidad). Navegaciones en PascalCase.
- `Rol` (ESTUDIANTE, ENCARGADO); `Usuario` (Codigo único, Nombre, hash de contraseña, IdRol, Estado, IntentosFallidos, BloqueadoHasta); `Categoria` (Cómputo, Electrónica, Redes); `Equipo` (Nombre, CodigoInventario único, IdCategoria, Version); `Reserva` (IdUsuario, IdEquipo, Inicio, Fin, Estado, MotivoRechazo de 200, FechaDevolucionReal, Version).
- Enums como texto: `EstadoReserva {Pendiente, Aprobada, Rechazada, Devuelta}`, `EstadoUsuario {Activo, Penalizado}`.
- Relaciones y `OnDelete` explícitos en `OnModelCreating`: `Restrict` hacia equipos y usuarios con historial; `SetNull` en referencias opcionales.
- Índices: únicos en `Usuario.Codigo` y `Equipo.CodigoInventario`; compuesto `Reserva (IdEquipo, Inicio, Fin)`; `Equipo.IdCategoria`; único filtrado de `Reserva.IdUsuario` para estados Pendiente y Aprobada (CA-10).
- `HasData` solo para `Rol` y `Categoria`. Usuarios de demostración con `DbSeeder` en Development, con hash real y datos de configuración.
- Modelos con Data Annotations y mensajes en español; `[ValidateNever]` en navegaciones.
- Cada cambio de modelo genera una migración descriptiva. El arranque ejecuta `Database.Migrate()`.

## Capas

- Controladores delgados que dependen de interfaces inyectadas: `IAutenticacionService`, `ICatalogoService`, `IReservaService`, `IDevolucionService`. Sin capa de repositorios: los servicios usan `AppDbContext`.
- `LoginController` (HU-01), `CatalogoController` (HU-02), `ReservasController` (HU-03 y "Mis reservas"), `SolicitudesController` (HU-04), `DevolucionesController` (HU-05).
- `AutenticacionService` CA-01 a 03; `CatalogoService` CA-04 a 06; `ReservaService` CA-08 a 13; `DevolucionService` CA-14 a 16.
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
- Penalizado: solo se registra y se muestra; no impide reservar. Levantar la penalización y la pantalla 07 quedan fuera de alcance.
- El estado del equipo se calcula, no se guarda: `EstadoEquipo {Disponible, Prestado}` es Prestado si existe una reserva Aprobada sin devolver cuyo inicio ya pasó, incluidas las vencidas.
- Solo se puede elegir un equipo Disponible ahora; la validación de cruces siempre consulta las reservas aprobadas.
- Devolver exactamente a la hora de fin no penaliza; solo se penaliza si la hora actual es estrictamente posterior.
- Concurrencia: crear y aprobar revalidan dentro de una transacción que bloquea la fila del equipo con `UPDLOCK, HOLDLOCK`. `Reserva` y `Equipo` tienen token `rowversion` (`Version`). Los conflictos devuelven un resultado controlado.
- Aprobar es una sola operación atómica.
- Del mockup se implementa lo que sostiene un CA y lo que es solo presentación; se omite lo que exige datos o reglas nuevas.

## Seguridad

- `PasswordHasher<Usuario>`; nunca texto plano. Mismo mensaje para código inexistente y contraseña errónea.
- Política por defecto que exige usuario autenticado; solo `LoginController` usa `[AllowAnonymous]`. `[Authorize(Roles = "ESTUDIANTE")]` en catálogo y reservas; `[Authorize(Roles = "ENCARGADO")]` en solicitudes y devoluciones.
- `AutoValidateAntiforgeryTokenAttribute` y `ResponseCache(NoStore = true)` como filtros globales. Cookie `HttpOnly` con expiración deslizante.
- `UseDeveloperExceptionPage` solo en Development; página de error genérica en producción.
- La cadena de conexión solo puede estar en el repositorio si no lleva contraseña (`Integrated Security`). Cualquier cadena con credenciales y los datos semilla van fuera: User Secrets en local, variables de entorno en Azure.
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
