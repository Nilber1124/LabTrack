using LabTrack.Controllers;
using LabTrack.Reglas;
using LabTrack.Services.Interfaces;
using LabTrack.Tests.Soporte;
using LabTrack.ViewModels;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace LabTrack.Tests.Controllers
{
    public class CatalogoControllerTests
    {
        private readonly ICatalogoService _servicio = Substitute.For<ICatalogoService>();
        private readonly CatalogoController _controlador;

        public CatalogoControllerTests()
        {
            _controlador = new CatalogoController(_servicio);
            ContextoControlador.Configurar(_controlador, 1, NombresRol.Estudiante);
        }

        [Fact]
        [Trait("HU", "HU-02")]
        [Trait("CA", "CA-05")]
        public async Task Index_ConFiltro_ConsultaLaCategoriaYPagina()
        {
            _servicio.ListarAsync("Redes", 2, Arg.Any<CancellationToken>()).Returns(new CatalogoViewModel());

            await _controlador.Index("Redes", 2);

            await _servicio.Received(1).ListarAsync("Redes", 2, Arg.Any<CancellationToken>());
        }

        [Fact]
        [Trait("HU", "HU-02")]
        [Trait("CA", "CA-06")]
        public async Task Index_SinEquipos_EntregaElMensajeExacto()
        {
            _servicio.ListarAsync("Redes", 1, Arg.Any<CancellationToken>()).Returns(new CatalogoViewModel());

            var vista = Assert.IsType<ViewResult>(await _controlador.Index("Redes"));

            var modelo = Assert.IsType<CatalogoViewModel>(vista.Model);
            Assert.Equal("No hay equipos disponibles en esta categoría", modelo.MensajeVacio);
        }

        [Fact]
        [Trait("HU", "HU-02")]
        [Trait("CA", "CA-04")]
        public async Task Index_ConEquipos_NoEntregaMensajeVacio()
        {
            var catalogo = new CatalogoViewModel { Equipos = [new EquipoCatalogoViewModel { Nombre = "Laptop" }] };
            _servicio.ListarAsync(null, 1, Arg.Any<CancellationToken>()).Returns(catalogo);

            var vista = Assert.IsType<ViewResult>(await _controlador.Index(null));

            Assert.Null(Assert.IsType<CatalogoViewModel>(vista.Model).MensajeVacio);
        }
    }
}
