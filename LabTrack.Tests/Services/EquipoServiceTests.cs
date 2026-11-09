using LabTrack.Data;
using LabTrack.Models;
using LabTrack.Reglas;
using LabTrack.Services;
using LabTrack.Services.Interfaces;
using LabTrack.Tests.Soporte;
using LabTrack.ViewModels;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace LabTrack.Tests.Services
{
    [Collection(ColeccionBaseDatos.Nombre)]
    [Trait("BD", "SQLServer")]
    public class EquipoServiceTests : PruebaConBaseDatos
    {
        private const string UrlNueva = "/uploads/equipos/nueva.jpg";

        private readonly IAlmacenImagenes _imagenes = Substitute.For<IAlmacenImagenes>();

        public EquipoServiceTests(BaseDatosFixture baseDatos) : base(baseDatos)
        {
            _imagenes.GuardarAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(UrlNueva);
        }

        private static DatosEquipo Datos(int id = 0, string codigo = "AE-0033") => new()
        {
            IdEquipo = id,
            Nombre = "Analizador",
            CodigoInventario = codigo,
            IdCategoria = IdElectronica,
            Laboratorio = "Lab. B-110"
        };

        private static ImagenSubida Imagen(params byte[] contenido) => new(new MemoryStream(contenido), contenido.Length);
        private static ImagenSubida ImagenJpeg() => Imagen(0xFF, 0xD8, 0xFF, 0xE0);

        private async Task<ResultadoOperacion> GuardarAsync(DatosEquipo datos, ImagenSubida? imagen = null)
        {
            await using var contexto = BaseDatos.CrearContexto();
            return await new EquipoService(contexto, Reloj, _imagenes).GuardarAsync(datos, imagen);
        }

        private async Task<GestionEquiposViewModel> ListarAsync(string? busqueda = null, string? categoria = null, int pagina = 1)
        {
            await using var contexto = BaseDatos.CrearContexto();
            return await new EquipoService(contexto, Reloj, _imagenes).ListarAsync(busqueda, categoria, pagina);
        }

        private async Task<ResultadoOperacion> DarDeBajaAsync(int idEquipo)
        {
            await using var contexto = BaseDatos.CrearContexto();
            return await new EquipoService(contexto, Reloj, _imagenes).DarDeBajaAsync(idEquipo);
        }

        private async Task<ResultadoOperacion> ReactivarAsync(int idEquipo)
        {
            await using var contexto = BaseDatos.CrearContexto();
            return await new EquipoService(contexto, Reloj, _imagenes).ReactivarAsync(idEquipo);
        }

        private async Task<Equipo> LeerEquipoAsync(int idEquipo)
        {
            await using var contexto = BaseDatos.CrearContexto();
            return await contexto.Equipos.AsNoTracking().SingleAsync(e => e.IdEquipo == idEquipo);
        }

        private async Task<Equipo> UnicoEquipoAsync()
        {
            await using var contexto = BaseDatos.CrearContexto();
            return await contexto.Equipos.AsNoTracking().SingleAsync();
        }

        [Fact]
        [Trait("HU", "ADM")]
        [Trait("CA", "ADM-CA2")]
        public async Task ListarAsync_MezclaDeEstados_CalculaSituacionYTotales()
        {
            var estudiante = await CrearUsuarioAsync("E1");
            var prestado = await CrearEquipoAsync("LT-1", nombre: "A prestado");
            await CrearEquipoAsync("LT-2", nombre: "B disponible");
            var baja = await CrearEquipoAsync("LT-3", nombre: "C baja");
            await CrearReservaAsync(estudiante.IdUsuario, prestado.IdEquipo, Ahora.AddHours(-1), Ahora.AddHours(1), EstadoReserva.Aprobada);
            await DarDeBajaAsync(baja.IdEquipo);

            var modelo = await ListarAsync();

            Assert.Equal(new[] { "prestado", "disponible", "baja" }, modelo.Equipos.Select(e => e.Situacion));
            Assert.Equal((1, 1), (modelo.TotalDisponibles, modelo.TotalDeBaja));
        }

        [Theory]
        [Trait("HU", "ADM")]
        [Trait("CA", "ADM-CA2")]
        [InlineData("Router")]
        [InlineData("RT-1")]
        public async Task ListarAsync_Busqueda_FiltraPorNombreOCodigo(string busqueda)
        {
            await CrearEquipoAsync("RT-1", IdRedes, "Router");
            await CrearEquipoAsync("LT-1", IdComputo, "Laptop");

            Assert.Equal("RT-1", Assert.Single((await ListarAsync(busqueda)).Equipos).CodigoInventario);
        }

        [Fact]
        [Trait("HU", "ADM")]
        [Trait("CA", "ADM-CA2")]
        public async Task ListarAsync_FiltroPorCategoria_DevuelveSoloEsaCategoria()
        {
            await CrearEquipoAsync("RT-1", IdRedes);
            await CrearEquipoAsync("LT-1", IdComputo);

            Assert.Equal("RT-1", Assert.Single((await ListarAsync(categoria: "Redes")).Equipos).CodigoInventario);
        }

        [Fact]
        [Trait("HU", "ADM")]
        [Trait("CA", "ADM-CA2")]
        public async Task ListarAsync_MasDeUnaPagina_Pagina()
        {
            for (var i = 0; i <= EquipoService.TamanoPagina; i++)
            {
                await CrearEquipoAsync($"LT-{i}");
            }

            Assert.Equal(EquipoService.TamanoPagina, (await ListarAsync()).Equipos.Count);
            Assert.Single((await ListarAsync(pagina: 2)).Equipos);
        }

        [Fact]
        [Trait("HU", "ADM")]
        [Trait("CA", "ADM-CA3")]
        public async Task GuardarAsync_EquipoNuevo_LoCreaConSusDatos()
        {
            var resultado = await GuardarAsync(Datos());

            var equipo = await UnicoEquipoAsync();
            Assert.True(resultado.Exitoso);
            Assert.Equal(("AE-0033", IdElectronica, "Lab. B-110"), (equipo.CodigoInventario, equipo.IdCategoria, equipo.Laboratorio));
            Assert.False(equipo.DeBaja);
        }

        [Fact]
        [Trait("HU", "ADM")]
        [Trait("CA", "ADM-CA3")]
        public async Task GuardarAsync_EquipoExistente_LoActualiza()
        {
            var equipo = await CrearEquipoAsync("LT-1");

            await GuardarAsync(Datos(equipo.IdEquipo, "LT-9"));

            Assert.Equal("LT-9", (await LeerEquipoAsync(equipo.IdEquipo)).CodigoInventario);
        }

        [Fact]
        [Trait("HU", "ADM")]
        [Trait("CA", "ADM-CA3")]
        public async Task GuardarAsync_MismoCodigoAlEditar_SePermite()
        {
            var equipo = await CrearEquipoAsync("LT-1");

            Assert.True((await GuardarAsync(Datos(equipo.IdEquipo, "LT-1"))).Exitoso);
        }

        [Fact]
        [Trait("HU", "ADM")]
        [Trait("CA", "ADM-CA3")]
        public async Task GuardarAsync_CodigoRepetido_IndicaAQuienPertenece()
        {
            await CrearEquipoAsync("LT-1", nombre: "Laptop Dell");

            var resultado = await GuardarAsync(Datos(codigo: "LT-1"));

            Assert.Equal(Mensajes.CodigoRepetido, resultado.Mensaje);
            Assert.Equal("LT-1 pertenece a Laptop Dell.", resultado.Aviso);
        }

        [Fact]
        [Trait("HU", "ADM")]
        [Trait("CA", "ADM-CA3")]
        public async Task GuardarAsync_EquipoInexistente_DevuelveNoEncontrado()
        {
            Assert.Equal(TipoResultado.NoEncontrado, (await GuardarAsync(Datos(999_999))).Tipo);
        }

        [Fact]
        [Trait("HU", "ADM")]
        [Trait("CA", "ADM-CA3")]
        public async Task GuardarAsync_LaboratorioDesconocido_SeRechaza()
        {
            var datos = new DatosEquipo { Nombre = "X", CodigoInventario = "X-1", IdCategoria = IdRedes, Laboratorio = "Otro" };

            Assert.Equal(Mensajes.LaboratorioNoValido, (await GuardarAsync(datos)).Mensaje);
        }

        [Fact]
        [Trait("HU", "ADM")]
        [Trait("CA", "ADM-CA3")]
        public async Task GuardarAsync_CategoriaInexistente_SeRechaza()
        {
            var datos = new DatosEquipo { Nombre = "X", CodigoInventario = "X-1", IdCategoria = 999, Laboratorio = "Lab. A-204" };

            Assert.Equal(Mensajes.CategoriaNoValida, (await GuardarAsync(datos)).Mensaje);
        }

        [Fact]
        [Trait("HU", "ADM")]
        [Trait("CA", "ADM-CA3")]
        public async Task GuardarAsync_ConImagenValida_GuardaLaUrl()
        {
            await GuardarAsync(Datos(), ImagenJpeg());

            Assert.Equal(UrlNueva, (await UnicoEquipoAsync()).FotoUrl);
            await _imagenes.Received(1).GuardarAsync(Arg.Any<Stream>(), ".jpg", Arg.Any<CancellationToken>());
        }

        [Fact]
        [Trait("HU", "ADM")]
        [Trait("CA", "ADM-CA3")]
        public async Task GuardarAsync_ServicioDeImagenesNoDisponible_NoCreaElEquipo()
        {
            _imagenes.GuardarAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((string?)null);

            var resultado = await GuardarAsync(Datos(), ImagenJpeg());

            Assert.Equal(Mensajes.ImagenNoSubida, resultado.Mensaje);
            Assert.Equal(Mensajes.ServicioImagenesNoDisponible, resultado.Aviso);
            await using var contexto = BaseDatos.CrearContexto();
            Assert.Equal(0, await contexto.Equipos.CountAsync());
        }

        [Fact]
        [Trait("HU", "ADM")]
        [Trait("CA", "ADM-CA3")]
        public async Task GuardarAsync_ContenidoQueNoEsImagen_SeRechazaSinGuardar()
        {
            var resultado = await GuardarAsync(Datos(), Imagen(1, 2, 3, 4));

            Assert.Equal(Mensajes.ImagenNoSubida, resultado.Mensaje);
            await _imagenes.DidNotReceiveWithAnyArgs().GuardarAsync(default!, default!, default);
            await using var contexto = BaseDatos.CrearContexto();
            Assert.Equal(0, await contexto.Equipos.CountAsync());
        }

        [Fact]
        [Trait("HU", "ADM")]
        [Trait("CA", "ADM-CA3")]
        public async Task GuardarAsync_ImagenDemasiadoGrande_SeRechaza()
        {
            var imagen = new ImagenSubida(new MemoryStream(new byte[] { 0xFF, 0xD8, 0xFF }), ValidacionImagen.TamanoMaximoBytes + 1);

            Assert.Equal(Mensajes.ImagenNoSubida, (await GuardarAsync(Datos(), imagen)).Mensaje);
        }

        [Fact]
        [Trait("HU", "ADM")]
        [Trait("CA", "ADM-CA3")]
        public async Task GuardarAsync_SinImagenNueva_ConservaLaFoto()
        {
            await GuardarAsync(Datos(), ImagenJpeg());
            var equipo = await UnicoEquipoAsync();

            await GuardarAsync(Datos(equipo.IdEquipo));

            Assert.Equal(UrlNueva, (await LeerEquipoAsync(equipo.IdEquipo)).FotoUrl);
            await _imagenes.DidNotReceiveWithAnyArgs().EliminarAsync(default!, default);
        }

        [Fact]
        [Trait("HU", "ADM")]
        [Trait("CA", "ADM-CA3")]
        public async Task GuardarAsync_FotoReemplazada_EliminaLaAnterior()
        {
            await GuardarAsync(Datos(), ImagenJpeg());
            var equipo = await UnicoEquipoAsync();

            await GuardarAsync(Datos(equipo.IdEquipo), Imagen(0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A));

            await _imagenes.Received(1).EliminarAsync(UrlNueva, Arg.Any<CancellationToken>());
        }

        [Fact]
        [Trait("HU", "ADM")]
        [Trait("CA", "ADM-CA3")]
        public async Task GuardarAsync_ReemplazaFotoDeDemostracion_NoLaEliminaDeCloudinary()
        {
            var fotoCompartida = FotosDemostracion.Para("LT-0231")!;
            var equipo = await CrearEquipoAsync("LT-1");
            await using (var contexto = BaseDatos.CrearContexto())
            {
                await contexto.Equipos.Where(e => e.IdEquipo == equipo.IdEquipo)
                    .ExecuteUpdateAsync(s => s.SetProperty(e => e.FotoUrl, fotoCompartida));
            }

            await GuardarAsync(Datos(equipo.IdEquipo), ImagenJpeg());

            Assert.Equal(UrlNueva, (await LeerEquipoAsync(equipo.IdEquipo)).FotoUrl);
            await _imagenes.DidNotReceiveWithAnyArgs().EliminarAsync(default!, default);
        }

        [Fact]
        [Trait("HU", "ADM")]
        [Trait("CA", "ADM-CA4")]
        public async Task DarDeBajaAsync_Disponible_LoOcultaDelCatalogo()
        {
            var equipo = await CrearEquipoAsync("LT-1");

            var resultado = await DarDeBajaAsync(equipo.IdEquipo);

            await using var contexto = BaseDatos.CrearContexto();
            var catalogo = await new CatalogoService(contexto, Reloj).ListarAsync(null, 1);
            Assert.True(resultado.Exitoso);
            Assert.True((await LeerEquipoAsync(equipo.IdEquipo)).DeBaja);
            Assert.Empty(catalogo.Equipos);
            Assert.Equal(0, catalogo.TotalGeneral);
        }

        [Fact]
        [Trait("HU", "ADM")]
        [Trait("CA", "ADM-CA4")]
        public async Task DarDeBajaAsync_Prestado_SeBloqueaConLaHoraDeFin()
        {
            var estudiante = await CrearUsuarioAsync("E1");
            var equipo = await CrearEquipoAsync("OS-0112");
            await CrearReservaAsync(estudiante.IdUsuario, equipo.IdEquipo, Ahora.AddHours(-1), Ahora.AddHours(2), EstadoReserva.Aprobada);

            var resultado = await DarDeBajaAsync(equipo.IdEquipo);

            Assert.Equal(Mensajes.NoSePuedeDarDeBaja, resultado.Mensaje);
            Assert.Equal("OS-0112 está prestado hasta las 12:00. Registra su devolución primero.", resultado.Aviso);
            Assert.False((await LeerEquipoAsync(equipo.IdEquipo)).DeBaja);
        }

        [Fact]
        [Trait("HU", "ADM")]
        [Trait("CA", "ADM-CA4")]
        public async Task DarDeBajaAsync_YaDeBaja_DevuelveEstadoInvalido()
        {
            var equipo = await CrearEquipoAsync("LT-1");
            await DarDeBajaAsync(equipo.IdEquipo);

            Assert.Equal(TipoResultado.EstadoInvalido, (await DarDeBajaAsync(equipo.IdEquipo)).Tipo);
        }

        [Fact]
        [Trait("HU", "ADM")]
        [Trait("CA", "ADM-CA4")]
        public async Task DarDeBajaAsync_Inexistente_DevuelveNoEncontrado()
        {
            Assert.Equal(TipoResultado.NoEncontrado, (await DarDeBajaAsync(999_999)).Tipo);
        }

        [Fact]
        [Trait("HU", "ADM")]
        [Trait("CA", "ADM-CA4")]
        public async Task DarDeBajaAsync_ConHistorial_ConservaLasReservas()
        {
            var estudiante = await CrearUsuarioAsync("E1");
            var equipo = await CrearEquipoAsync("LT-1");
            await CrearReservaAsync(estudiante.IdUsuario, equipo.IdEquipo, Ahora.AddDays(-2), Ahora.AddDays(-2).AddHours(1), EstadoReserva.Devuelta);

            await DarDeBajaAsync(equipo.IdEquipo);

            Assert.Equal(1, await ContarReservasAsync());
        }

        [Fact]
        [Trait("HU", "ADM")]
        [Trait("CA", "ADM-CA4")]
        public async Task ReactivarAsync_DeBaja_VuelveAlCatalogo()
        {
            var equipo = await CrearEquipoAsync("LT-1");
            await DarDeBajaAsync(equipo.IdEquipo);

            var resultado = await ReactivarAsync(equipo.IdEquipo);

            Assert.True(resultado.Exitoso);
            Assert.Equal(EstadoEquipo.Disponible, await EstadoDelEquipoAsync(equipo.IdEquipo));
        }

        [Fact]
        [Trait("HU", "ADM")]
        [Trait("CA", "ADM-CA4")]
        public async Task ReactivarAsync_NoEstaDeBaja_DevuelveEstadoInvalido()
        {
            var equipo = await CrearEquipoAsync("LT-1");

            Assert.Equal(Mensajes.EquipoNoEstaDeBaja, (await ReactivarAsync(equipo.IdEquipo)).Mensaje);
        }

        [Fact]
        [Trait("HU", "ADM")]
        [Trait("CA", "ADM-CA4")]
        public async Task CrearReserva_EquipoDeBaja_DevuelveNoEncontrado()
        {
            var estudiante = await CrearUsuarioAsync("E1");
            var equipo = await CrearEquipoAsync("LT-1");
            await DarDeBajaAsync(equipo.IdEquipo);

            await using var contexto = BaseDatos.CrearContexto();
            var resultado = await new ReservaService(contexto, Reloj)
                .CrearAsync(estudiante.IdUsuario, equipo.IdEquipo, Manana, new TimeOnly(10, 0), new TimeOnly(12, 0));

            Assert.Equal(TipoResultado.NoEncontrado, resultado.Tipo);
        }

        [Fact]
        [Trait("HU", "ADM")]
        [Trait("CA", "ADM-CA4")]
        public async Task Aprobar_EquipoDeBaja_SeRechazaSinCambios()
        {
            var estudiante = await CrearUsuarioAsync("E1");
            var equipo = await CrearEquipoAsync("LT-1");
            var reserva = await CrearReservaAsync(estudiante.IdUsuario, equipo.IdEquipo, HoraManana(14), HoraManana(16));
            await DarDeBajaAsync(equipo.IdEquipo);

            await using var contexto = BaseDatos.CrearContexto();
            var resultado = await new ReservaService(contexto, Reloj).AprobarAsync(reserva.IdReserva);

            Assert.Equal(Mensajes.EquipoNoDisponible, resultado.Mensaje);
            Assert.Equal(EstadoReserva.Pendiente, (await LeerReservaAsync(reserva.IdReserva)).Estado);
        }
    }
}
