using LabTrack.ViewModels;

namespace LabTrack.Tests.ViewModels
{
    public class PaginacionViewModelTests
    {
        [Theory]
        [Trait("HU", "HU-02")]
        [Trait("CA", "CA-04")]
        [InlineData(0, 20, 1)]
        [InlineData(-3, 20, 1)]
        [InlineData(2, 20, 2)]
        [InlineData(99, 20, 3)]
        [InlineData(5, 0, 1)]
        public void Normalizar_PaginaFueraDeRango_QuedaDentroDelRango(int pagina, int total, int esperada)
        {
            Assert.Equal(esperada, PaginacionViewModel.Normalizar(pagina, total, 8));
        }

        [Fact]
        [Trait("HU", "HU-02")]
        [Trait("CA", "CA-04")]
        public void DesdeYHasta_SegundaPagina_DevuelveElIntervalo()
        {
            var paginacion = new PaginacionViewModel { Pagina = 2, Total = 12, TamanoPagina = 8 };

            Assert.Equal((9, 12), (paginacion.Desde, paginacion.Hasta));
        }

        [Fact]
        [Trait("HU", "HU-02")]
        [Trait("CA", "CA-06")]
        public void Desde_SinRegistros_DevuelveCero()
        {
            Assert.Equal(0, new PaginacionViewModel { Pagina = 1, Total = 0, TamanoPagina = 8 }.Desde);
        }
    }
}
