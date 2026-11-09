using LabTrack.Data;
using LabTrack.Models;
using LabTrack.Reglas;
using LabTrack.Services;
using LabTrack.Tests.Soporte;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace LabTrack.Tests.Services
{
    [Collection(ColeccionBaseDatos.Nombre)]
    [Trait("BD", "SQLServer")]
    public class AutenticacionServiceTests : PruebaConBaseDatos
    {
        private const string Codigo = "20231458";
        private const string Clave = "clave-correcta";

        public AutenticacionServiceTests(BaseDatosFixture baseDatos) : base(baseDatos)
        {
        }

        private AutenticacionService Servicio(AppDbContext contexto, int minutos = 15)
            => new(contexto, new PasswordHasher<Usuario>(), Reloj,
                Options.Create(new SeguridadOpciones { MinutosBloqueo = minutos }));

        private async Task<ResultadoAutenticacion> AutenticarAsync(string? codigo, string? clave)
        {
            await using var contexto = BaseDatos.CrearContexto();
            return await Servicio(contexto).AutenticarAsync(codigo, clave);
        }

        private async Task FallarAsync(int veces)
        {
            for (var i = 0; i < veces; i++)
            {
                await AutenticarAsync(Codigo, "clave-erronea");
            }
        }

        [Fact]
        [Trait("HU", "HU-01")]
        [Trait("CA", "CA-01")]
        public async Task AutenticarAsync_CredencialesValidas_DevuelveUsuarioYRol()
        {
            var usuario = await CrearUsuarioAsync(Codigo, Clave);

            var resultado = await AutenticarAsync(Codigo, Clave);

            Assert.Equal(EstadoAutenticacion.Exitosa, resultado.Estado);
            Assert.Equal(usuario.IdUsuario, resultado.Usuario!.IdUsuario);
            Assert.Equal(NombresRol.Estudiante, resultado.Usuario.Rol);
        }

        [Fact]
        [Trait("HU", "HU-01")]
        [Trait("CA", "CA-02")]
        public async Task AutenticarAsync_ContrasenaErronea_DevuelveMensajeExacto()
        {
            await CrearUsuarioAsync(Codigo, Clave);

            var resultado = await AutenticarAsync(Codigo, "clave-erronea");

            Assert.Equal(EstadoAutenticacion.CredencialesInvalidas, resultado.Estado);
            Assert.Equal("Credenciales inválidas", resultado.Mensaje);
        }

        [Fact]
        [Trait("HU", "HU-01")]
        [Trait("CA", "CA-02")]
        public async Task AutenticarAsync_CodigoInexistente_DevuelveElMismoMensaje()
        {
            var resultado = await AutenticarAsync("00000000", Clave);

            Assert.Equal(EstadoAutenticacion.CredencialesInvalidas, resultado.Estado);
            Assert.Equal("Credenciales inválidas", resultado.Mensaje);
        }

        [Theory]
        [Trait("HU", "HU-01")]
        [Trait("CA", "CA-02")]
        [InlineData("", Clave)]
        [InlineData("   ", Clave)]
        [InlineData(null, Clave)]
        [InlineData(Codigo, "")]
        [InlineData(Codigo, null)]
        public async Task AutenticarAsync_CamposVacios_DevuelveCredencialesInvalidas(string? codigo, string? clave)
        {
            await CrearUsuarioAsync(Codigo, Clave);

            var resultado = await AutenticarAsync(codigo, clave);

            Assert.Equal(EstadoAutenticacion.CredencialesInvalidas, resultado.Estado);
        }

        [Theory]
        [Trait("HU", "HU-01")]
        [Trait("CA", "CA-03")]
        [InlineData(1)]
        [InlineData(2)]
        public async Task AutenticarAsync_MenosDeTresFallos_CuentaSinBloquear(int fallos)
        {
            var usuario = await CrearUsuarioAsync(Codigo, Clave);

            await FallarAsync(fallos);

            var guardado = await LeerUsuarioAsync(usuario.IdUsuario);
            Assert.Equal(fallos, guardado.IntentosFallidos);
            Assert.Null(guardado.BloqueadoHasta);
        }

        [Fact]
        [Trait("HU", "HU-01")]
        [Trait("CA", "CA-03")]
        public async Task AutenticarAsync_TercerFallo_BloqueaYDevuelveAlerta()
        {
            var usuario = await CrearUsuarioAsync(Codigo, Clave);
            await FallarAsync(2);

            var resultado = await AutenticarAsync(Codigo, "clave-erronea");

            Assert.Equal(EstadoAutenticacion.Bloqueada, resultado.Estado);
            Assert.Equal(Mensajes.CuentaBloqueada, resultado.Mensaje);
            Assert.Equal(Ahora.AddMinutes(15), (await LeerUsuarioAsync(usuario.IdUsuario)).BloqueadoHasta);
        }

        [Fact]
        [Trait("HU", "HU-01")]
        [Trait("CA", "CA-03")]
        public async Task AutenticarAsync_MinutosConfigurados_SeRespetan()
        {
            var usuario = await CrearUsuarioAsync(Codigo, Clave);
            await FallarAsync(2);

            await using (var contexto = BaseDatos.CrearContexto())
            {
                await Servicio(contexto, minutos: 30).AutenticarAsync(Codigo, "clave-erronea");
            }

            Assert.Equal(Ahora.AddMinutes(30), (await LeerUsuarioAsync(usuario.IdUsuario)).BloqueadoHasta);
        }

        [Fact]
        [Trait("HU", "HU-01")]
        [Trait("CA", "CA-03")]
        public async Task AutenticarAsync_CuentaBloqueadaConClaveCorrecta_NoEntra()
        {
            await CrearUsuarioAsync(Codigo, Clave);
            await FallarAsync(3);

            var resultado = await AutenticarAsync(Codigo, Clave);

            Assert.Equal(EstadoAutenticacion.Bloqueada, resultado.Estado);
            Assert.Null(resultado.Usuario);
        }

        [Fact]
        [Trait("HU", "HU-01")]
        [Trait("CA", "CA-03")]
        public async Task AutenticarAsync_FalloDuranteElBloqueo_NoLoAlarga()
        {
            var usuario = await CrearUsuarioAsync(Codigo, Clave);
            await FallarAsync(3);
            Reloj.Advance(TimeSpan.FromMinutes(5));

            await FallarAsync(1);

            Assert.Equal(Ahora.AddMinutes(15), (await LeerUsuarioAsync(usuario.IdUsuario)).BloqueadoHasta);
        }

        [Fact]
        [Trait("HU", "HU-01")]
        [Trait("CA", "CA-03")]
        public async Task AutenticarAsync_TrasVencerElBloqueo_Entra()
        {
            await CrearUsuarioAsync(Codigo, Clave);
            await FallarAsync(3);
            Reloj.Advance(TimeSpan.FromMinutes(15));

            var resultado = await AutenticarAsync(Codigo, Clave);

            Assert.Equal(EstadoAutenticacion.Exitosa, resultado.Estado);
        }

        [Fact]
        [Trait("HU", "HU-01")]
        [Trait("CA", "CA-03")]
        public async Task AutenticarAsync_UnSegundoAntesDeVencer_SigueBloqueada()
        {
            await CrearUsuarioAsync(Codigo, Clave);
            await FallarAsync(3);
            Reloj.Advance(TimeSpan.FromMinutes(15) - TimeSpan.FromSeconds(1));

            var resultado = await AutenticarAsync(Codigo, Clave);

            Assert.Equal(EstadoAutenticacion.Bloqueada, resultado.Estado);
            Assert.Equal(1, resultado.SegundosRestantes);
        }

        [Fact]
        [Trait("HU", "HU-01")]
        [Trait("CA", "CA-03")]
        public async Task AutenticarAsync_FalloTrasVencerElBloqueo_ReiniciaElContador()
        {
            var usuario = await CrearUsuarioAsync(Codigo, Clave);
            await FallarAsync(3);
            Reloj.Advance(TimeSpan.FromMinutes(15));

            var resultado = await AutenticarAsync(Codigo, "clave-erronea");

            var guardado = await LeerUsuarioAsync(usuario.IdUsuario);
            Assert.Equal(EstadoAutenticacion.CredencialesInvalidas, resultado.Estado);
            Assert.Equal(1, guardado.IntentosFallidos);
            Assert.Null(guardado.BloqueadoHasta);
        }

        [Fact]
        [Trait("HU", "HU-01")]
        [Trait("CA", "CA-03")]
        public async Task AutenticarAsync_LoginExitoso_ReiniciaElContador()
        {
            var usuario = await CrearUsuarioAsync(Codigo, Clave);
            await FallarAsync(2);

            await AutenticarAsync(Codigo, Clave);

            Assert.Equal(0, (await LeerUsuarioAsync(usuario.IdUsuario)).IntentosFallidos);
        }

        [Fact]
        [Trait("HU", "HU-05")]
        [Trait("CA", "CA-16")]
        public async Task ObtenerResumenAsync_EstudiantePenalizado_MuestraSuEstado()
        {
            var usuario = await CrearUsuarioAsync(Codigo, Clave, estado: EstadoUsuario.Penalizado);

            await using var contexto = BaseDatos.CrearContexto();
            var resumen = await Servicio(contexto).ObtenerResumenAsync(usuario.IdUsuario);

            Assert.Equal(EstadoUsuario.Penalizado, resumen!.Estado);
        }
    }
}
