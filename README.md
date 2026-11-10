# LabTrack

Sistema web para gestionar el préstamo de equipos de laboratorio: los estudiantes consultan el catálogo y solicitan reservas, y el personal del laboratorio las aprueba, registra las devoluciones y aplica penalizaciones automáticas.

Proyecto desarrollado para el curso **Calidad y Pruebas de Software**.

## Funcionalidades

| Rol | Puede |
| --- | --- |
| **Estudiante** | Consultar y filtrar el catálogo, solicitar reservas y ver el estado de sus reservas |
| **Encargado** | Aprobar o rechazar solicitudes, registrar devoluciones y levantar penalizaciones |
| **Administrador** | Todo lo del encargado, además de gestionar equipos y usuarios |

Reglas principales:

- Tres intentos fallidos de inicio de sesión bloquean la cuenta temporalmente.
- Un equipo no admite reservas aprobadas con horarios cruzados.
- Un estudiante solo puede tener una reserva activa.
- Devolver un equipo después de la hora de fin penaliza al estudiante, y mientras siga penalizado no puede reservar.

## Tecnologías

- .NET 10, ASP.NET Core MVC y Razor
- Entity Framework Core 10 con SQL Server
- Autenticación por cookie
- Cloudinary para las fotos de los equipos
- xUnit, NSubstitute y coverlet para las pruebas

## Requisitos

- [SDK de .NET 10](https://dotnet.microsoft.com/download)
- SQL Server
- Una cuenta de [Cloudinary](https://cloudinary.com/), solo si se van a subir fotos

## Puesta en marcha

### 1. Conexión a la base de datos

Ajusta `ConnectionStrings:DefaultConnection` en `LabTrack/appsettings.json` con el nombre de tu servidor.

### 2. Datos sensibles

Copia `.env.example` como `.env` en la raíz del repositorio y llena los valores. Git ignora ese archivo. Sin las claves de Cloudinary la aplicación funciona, pero no permite subir fotos.

### 3. Datos de prueba

En entorno Development no hay que configurar nada: al arrancar se crean equipos con foto, reservas y estos usuarios, definidos en `LabTrack/appsettings.Development.json`, donde también están sus contraseñas de prueba.

| Código | Rol |
| --- | --- |
| `ADM-001` | Administrador |
| `ENC-001` | Encargado |
| `20231458` | Estudiante |

Estos datos nunca se crean fuera de Development.

### 4. Ejecutar

```bash
dotnet run --project LabTrack/LabTrack.csproj --launch-profile http
```

La aplicación aplica las migraciones al arrancar y queda disponible en <http://localhost:5140>.

## Pruebas

Las pruebas de servicios usan SQL Server real, en una base temporal que se crea y se elimina en cada ejecución. Necesitan una conexión autorizada para crear bases:

```bash
export LABTRACK_TEST_SQLSERVER="Data Source=TU_SERVIDOR;Integrated Security=True;TrustServerCertificate=True;"
dotnet test LabTrack.Tests/LabTrack.Tests.csproj
```

En PowerShell, la variable se define con `$env:LABTRACK_TEST_SQLSERVER="..."`.

Para ejecutar solo las pruebas que no usan base de datos:

```bash
dotnet test LabTrack.Tests/LabTrack.Tests.csproj --filter "BD!=SQLServer"
```

Para medir la cobertura de `Reglas/` y `Services/`, con un mínimo del 80 %:

```bash
dotnet test LabTrack.Tests/LabTrack.Tests.csproj /p:CollectCoverage=true /p:Threshold=80 /p:ThresholdType=line /p:Include="[LabTrack]LabTrack.Reglas.*%2c[LabTrack]LabTrack.Services.*"
```

## Estructura

```
LabTrack/         Aplicación web: controladores, servicios, reglas, modelos y vistas
LabTrack.Tests/   Pruebas, organizadas igual que la aplicación
AGENTS.md         Reglas y decisiones de desarrollo del proyecto
```

## Despliegue

Configura `ConnectionStrings__DefaultConnection` y las claves `Cloudinary__*` como variables de entorno, y no uses el entorno Development. Fuera de ese entorno no se crean datos de prueba, por lo que el primer administrador debe crearse directamente en la base de datos.

La aplicación está publicada en Azure App Service. Cada push a `master` ejecuta las pruebas que no necesitan base de datos y, si pasan, despliega con [`desplegar-azure.yml`](.github/workflows/desplegar-azure.yml). Las migraciones pendientes se aplican al arrancar.
