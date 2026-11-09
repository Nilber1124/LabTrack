using LabTrack.Models;
using LabTrack.Reglas;
using LabTrack.Services;
using LabTrack.Tests.Soporte;
using LabTrack.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LabTrack.Tests.Services
{
    [Collection(ColeccionBaseDatos.Nombre)]
    [Trait("BD", "SQLServer")]
    public class UsuarioServiceTests : PruebaConBaseDatos
    {
        private const int IdRolAdministrador = 3;

        public UsuarioServiceTests(BaseDatosFixture baseDatos) : base(baseDatos)
        {
        }

        private static DatosUsuario Nuevo(string codigo = "20240001", string rol = NombresRol.Estudiante, string? contrasena = "clave-segura") => new()
        {
            Codigo = codigo,
            Nombre = "Ana Torres",
            Rol = rol,
            Contrasena = contrasena
        };

        private async Task<GestionUsuariosViewModel> ListarAsync(string? busqueda = null, string? rol = null, int pagina = 1)
        {
            await using var contexto = BaseDatos.CrearContexto();
            return await new UsuarioService(contexto, new PasswordHasher<Usuario>()).ListarAsync(busqueda, rol, pagina);
        }

        private async Task<ResultadoOperacion> GuardarAsync(DatosUsuario datos)
        {
            await using var contexto = BaseDatos.CrearContexto();
            return await new UsuarioService(contexto, new PasswordHasher<Usuario>()).GuardarAsync(datos);
        }

        private async Task<ResultadoOperacion> DarDeBajaAsync(int idUsuario)
        {
            await using var contexto = BaseDatos.CrearContexto();
            return await new UsuarioService(contexto, new PasswordHasher<Usuario>()).DarDeBajaAsync(idUsuario);
        }

        private async Task<ResultadoOperacion> ReactivarAsync(int idUsuario)
        {
            await using var contexto = BaseDatos.CrearContexto();
            return await new UsuarioService(contexto, new PasswordHasher<Usuario>()).ReactivarAsync(idUsuario);
        }

        private async Task<ResultadoAutenticacion> AutenticarAsync(string codigo, string clave)
        {
            await using var contexto = BaseDatos.CrearContexto();
            return await new AutenticacionService(contexto, new PasswordHasher<Usuario>(), Reloj, Options.Create(new SeguridadOpciones()))
                .AutenticarAsync(codigo, clave);
        }

        private async Task<(Usuario Usuario, string Rol)> LeerConRolAsync(string codigo)
        {
            await using var contexto = BaseDatos.CrearContexto();
            var usuario = await contexto.Usuarios.AsNoTracking().Include(u => u.Rol).SingleAsync(u => u.Codigo == codigo);
            return (usuario, usuario.Rol.Nombre);
        }

        [Fact]
        [Trait("HU", "ADM")]
        [Trait("CA", "ADM-USU")]
        public async Task ListarAsync_VariosRoles_NoMuestraAdministradores()
        {
            await CrearUsuarioAsync("E1");
            await CrearUsuarioAsync("ENC", idRol: IdRolEncargado);
            await CrearUsuarioAsync("ADM", idRol: IdRolAdministrador);

            var modelo = await ListarAsync();

            Assert.DoesNotContain(modelo.Usuarios, u => u.Codigo == "ADM");
            Assert.Equal((1, 1, 2), (modelo.TotalEstudiantes, modelo.TotalEncargados, modelo.TotalGeneral));
        }

        [Fact]
        [Trait("HU", "ADM")]
        [Trait("CA", "ADM-USU")]
        public async Task ListarAsync_FiltroPorRol_DevuelveSoloEseRol()
        {
            await CrearUsuarioAsync("E1");
            await CrearUsuarioAsync("ENC", idRol: IdRolEncargado);

            Assert.Equal("ENC", Assert.Single((await ListarAsync(rol: NombresRol.Encargado)).Usuarios).Codigo);
        }

        [Fact]
        [Trait("HU", "ADM")]
        [Trait("CA", "ADM-USU")]
        public async Task ListarAsync_RolNoGestionable_SeIgnora()
        {
            await CrearUsuarioAsync("E1");

            Assert.Single((await ListarAsync(rol: NombresRol.Administrador)).Usuarios);
        }

        [Theory]
        [Trait("HU", "ADM")]
        [Trait("CA", "ADM-USU")]
        [InlineData("Nombre E1")]
        [InlineData("E1")]
        public async Task ListarAsync_Busqueda_FiltraPorNombreOCodigo(string busqueda)
        {
            await CrearUsuarioAsync("E1");
            await CrearUsuarioAsync("X9");

            Assert.Equal("E1", Assert.Single((await ListarAsync(busqueda)).Usuarios).Codigo);
        }

        [Fact]
        [Trait("HU", "ADM")]
        [Trait("CA", "ADM-USU")]
        public async Task ListarAsync_Estados_CalculaSituacionYTotales()
        {
            var baja = await CrearUsuarioAsync("A-BAJA");
            await CrearUsuarioAsync("B-PENALIZADO", estado: EstadoUsuario.Penalizado);
            await CrearUsuarioAsync("C-ACTIVO");
            await DarDeBajaAsync(baja.IdUsuario);

            var modelo = await ListarAsync();

            Assert.Equal(new[] { "baja", "penalizado", "activo" }, modelo.Usuarios.Select(u => u.Situacion));
            Assert.Equal((2, 1), (modelo.TotalActivos, modelo.TotalDeBaja));
        }

        [Theory]
        [Trait("HU", "ADM")]
        [Trait("CA", "ADM-USU")]
        [InlineData(NombresRol.Estudiante)]
        [InlineData(NombresRol.Encargado)]
        public async Task GuardarAsync_UsuarioNuevo_LoCreaConSuRol(string rol)
        {
            var resultado = await GuardarAsync(Nuevo(rol: rol));

            Assert.True(resultado.Exitoso);
            Assert.Equal(rol, (await LeerConRolAsync("20240001")).Rol);
        }

        [Fact]
        [Trait("HU", "ADM")]
        [Trait("CA", "ADM-USU")]
        public async Task GuardarAsync_UsuarioNuevo_PuedeIniciarSesion()
        {
            await GuardarAsync(Nuevo());

            Assert.Equal(EstadoAutenticacion.Exitosa, (await AutenticarAsync("20240001", "clave-segura")).Estado);
        }

        [Fact]
        [Trait("HU", "ADM")]
        [Trait("CA", "ADM-USU")]
        public async Task GuardarAsync_UsuarioNuevo_NoGuardaLaContrasenaEnClaro()
        {
            await GuardarAsync(Nuevo());

            Assert.NotEqual("clave-segura", (await LeerConRolAsync("20240001")).Usuario.PasswordHash);
        }

        [Fact]
        [Trait("HU", "ADM")]
        [Trait("CA", "ADM-USU")]
        public async Task GuardarAsync_RolAdministrador_SeRechaza()
        {
            Assert.Equal(Mensajes.RolNoValido, (await GuardarAsync(Nuevo(rol: NombresRol.Administrador))).Mensaje);
        }

        [Fact]
        [Trait("HU", "ADM")]
        [Trait("CA", "ADM-USU")]
        public async Task GuardarAsync_NuevoSinContrasena_SeRechaza()
        {
            Assert.Equal(Mensajes.ContrasenaObligatoria, (await GuardarAsync(Nuevo(contrasena: null))).Mensaje);
        }

        [Fact]
        [Trait("HU", "ADM")]
        [Trait("CA", "ADM-USU")]
        public async Task GuardarAsync_CodigoRepetido_IndicaAQuienPertenece()
        {
            await CrearUsuarioAsync("E1");

            var resultado = await GuardarAsync(Nuevo("E1"));

            Assert.Equal(Mensajes.CodigoUsuarioRepetido, resultado.Mensaje);
            Assert.Equal("E1 pertenece a Nombre E1.", resultado.Aviso);
        }

        [Fact]
        [Trait("HU", "ADM")]
        [Trait("CA", "ADM-USU")]
        public async Task GuardarAsync_EdicionSinContrasena_ConservaLaActual()
        {
            var usuario = await CrearUsuarioAsync("E1", "clave-original");

            await GuardarAsync(new DatosUsuario { IdUsuario = usuario.IdUsuario, Codigo = "E1", Nombre = "Nombre nuevo" });

            Assert.Equal("Nombre nuevo", (await LeerUsuarioAsync(usuario.IdUsuario)).Nombre);
            Assert.Equal(EstadoAutenticacion.Exitosa, (await AutenticarAsync("E1", "clave-original")).Estado);
        }

        [Fact]
        [Trait("HU", "ADM")]
        [Trait("CA", "ADM-USU")]
        public async Task GuardarAsync_EdicionConRol_NoCambiaElRol()
        {
            var usuario = await CrearUsuarioAsync("E1");

            await GuardarAsync(new DatosUsuario { IdUsuario = usuario.IdUsuario, Codigo = "E1", Nombre = "Nombre", Rol = NombresRol.Encargado });

            Assert.Equal(NombresRol.Estudiante, (await LeerConRolAsync("E1")).Rol);
        }

        [Fact]
        [Trait("HU", "ADM")]
        [Trait("CA", "ADM-USU")]
        public async Task GuardarAsync_NuevaContrasena_DesbloqueaLaCuenta()
        {
            var usuario = await CrearUsuarioAsync("E1", "clave-original");
            for (var i = 0; i < 3; i++)
            {
                await AutenticarAsync("E1", "clave-erronea");
            }

            await GuardarAsync(new DatosUsuario { IdUsuario = usuario.IdUsuario, Codigo = "E1", Nombre = "Nombre", Contrasena = "clave-nueva-1" });

            Assert.Equal(EstadoAutenticacion.Exitosa, (await AutenticarAsync("E1", "clave-nueva-1")).Estado);
        }

        [Fact]
        [Trait("HU", "ADM")]
        [Trait("CA", "ADM-USU")]
        public async Task GuardarAsync_EdicionDeAdministrador_DevuelveNoEncontrado()
        {
            var administrador = await CrearUsuarioAsync("ADM", idRol: IdRolAdministrador);

            var resultado = await GuardarAsync(new DatosUsuario { IdUsuario = administrador.IdUsuario, Codigo = "ADM", Nombre = "X" });

            Assert.Equal(TipoResultado.NoEncontrado, resultado.Tipo);
        }

        [Fact]
        [Trait("HU", "ADM")]
        [Trait("CA", "ADM-USU")]
        public async Task DarDeBajaAsync_Usuario_ImpideIniciarSesion()
        {
            var usuario = await CrearUsuarioAsync("E1", "clave-original");

            var resultado = await DarDeBajaAsync(usuario.IdUsuario);

            var login = await AutenticarAsync("E1", "clave-original");
            Assert.True(resultado.Exitoso);
            Assert.Equal(EstadoAutenticacion.CredencialesInvalidas, login.Estado);
            Assert.Equal(Mensajes.CredencialesInvalidas, login.Mensaje);
        }

        [Theory]
        [Trait("HU", "ADM")]
        [Trait("CA", "ADM-USU")]
        [InlineData(EstadoReserva.Pendiente)]
        [InlineData(EstadoReserva.Aprobada)]
        public async Task DarDeBajaAsync_ConReservaActiva_SeBloquea(EstadoReserva estado)
        {
            var usuario = await CrearUsuarioAsync("E1");
            var equipo = await CrearEquipoAsync("LT-1", nombre: "Laptop");
            await CrearReservaAsync(usuario.IdUsuario, equipo.IdEquipo, HoraManana(8), HoraManana(9), estado);

            var resultado = await DarDeBajaAsync(usuario.IdUsuario);

            Assert.Equal(Mensajes.NoSePuedeDarDeBaja, resultado.Mensaje);
            Assert.Contains("«Laptop»", resultado.Aviso);
            Assert.False((await LeerUsuarioAsync(usuario.IdUsuario)).DeBaja);
        }

        [Fact]
        [Trait("HU", "ADM")]
        [Trait("CA", "ADM-USU")]
        public async Task DarDeBajaAsync_ConHistorial_ConservaLasReservas()
        {
            var usuario = await CrearUsuarioAsync("E1");
            var equipo = await CrearEquipoAsync("LT-1");
            await CrearReservaAsync(usuario.IdUsuario, equipo.IdEquipo, Ahora.AddDays(-2), Ahora.AddDays(-2).AddHours(1), EstadoReserva.Devuelta);

            await DarDeBajaAsync(usuario.IdUsuario);

            Assert.Equal(1, await ContarReservasAsync());
        }

        [Fact]
        [Trait("HU", "ADM")]
        [Trait("CA", "ADM-USU")]
        public async Task DarDeBajaAsync_Administrador_DevuelveNoEncontrado()
        {
            var administrador = await CrearUsuarioAsync("ADM", idRol: IdRolAdministrador);

            Assert.Equal(TipoResultado.NoEncontrado, (await DarDeBajaAsync(administrador.IdUsuario)).Tipo);
        }

        [Fact]
        [Trait("HU", "ADM")]
        [Trait("CA", "ADM-USU")]
        public async Task DarDeBajaAsync_YaDeBaja_DevuelveEstadoInvalido()
        {
            var usuario = await CrearUsuarioAsync("E1");
            await DarDeBajaAsync(usuario.IdUsuario);

            Assert.Equal(Mensajes.UsuarioYaDeBaja, (await DarDeBajaAsync(usuario.IdUsuario)).Mensaje);
        }

        [Fact]
        [Trait("HU", "ADM")]
        [Trait("CA", "ADM-USU")]
        public async Task ReactivarAsync_DeBaja_PermiteIniciarSesion()
        {
            var usuario = await CrearUsuarioAsync("E1", "clave-original");
            await DarDeBajaAsync(usuario.IdUsuario);

            await ReactivarAsync(usuario.IdUsuario);

            Assert.Equal(EstadoAutenticacion.Exitosa, (await AutenticarAsync("E1", "clave-original")).Estado);
        }

        [Fact]
        [Trait("HU", "ADM")]
        [Trait("CA", "ADM-USU")]
        public async Task ReactivarAsync_NoEstaDeBaja_DevuelveEstadoInvalido()
        {
            var usuario = await CrearUsuarioAsync("E1");

            Assert.Equal(Mensajes.UsuarioNoEstaDeBaja, (await ReactivarAsync(usuario.IdUsuario)).Mensaje);
        }

        [Fact]
        [Trait("HU", "ADM")]
        [Trait("CA", "ADM-USU")]
        public async Task EstaActivoAsync_SegunBaja_DevuelveEsperado()
        {
            var usuario = await CrearUsuarioAsync("E1");

            await using var contexto = BaseDatos.CrearContexto();
            var servicio = new AutenticacionService(contexto, new PasswordHasher<Usuario>(), Reloj, Options.Create(new SeguridadOpciones()));
            var antes = await servicio.EstaActivoAsync(usuario.IdUsuario);
            await DarDeBajaAsync(usuario.IdUsuario);
            var despues = await servicio.EstaActivoAsync(usuario.IdUsuario);

            Assert.Equal((true, false), (antes, despues));
        }

        [Fact]
        [Trait("HU", "ADM")]
        [Trait("CA", "ADM-USU")]
        public async Task ListarPenalizadosAsync_EstudianteDeBaja_NoAparece()
        {
            var usuario = await CrearUsuarioAsync("E1", estado: EstadoUsuario.Penalizado);
            await DarDeBajaAsync(usuario.IdUsuario);

            await using var contexto = BaseDatos.CrearContexto();
            var servicio = new DevolucionService(contexto, Reloj);

            Assert.Empty((await servicio.ListarPenalizadosAsync(null, 1)).Penalizados);
            Assert.Equal(0, await servicio.ContarPenalizadosAsync());
        }
    }
}
