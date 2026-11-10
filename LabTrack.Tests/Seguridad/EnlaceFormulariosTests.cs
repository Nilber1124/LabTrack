using LabTrack.ViewModels;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Reflection;

namespace LabTrack.Tests.Seguridad
{
    // Evita el under-posting: lo que identifica un registro debe llegar en el formulario,
    // y lo que solo se muestra no se lee desde él.
    public class EnlaceFormulariosTests
    {
        private static PropertyInfo Propiedad<T>(string nombre) => typeof(T).GetProperty(nombre)!;

        [Theory]
        [Trait("HU", "ADM")]
        [Trait("CA", "ADM-USU")]
        [InlineData(typeof(FormularioEquipoViewModel), nameof(FormularioEquipoViewModel.IdEquipo))]
        [InlineData(typeof(FormularioUsuarioViewModel), nameof(FormularioUsuarioViewModel.IdUsuario))]
        public void Formulario_Identificador_EsObligatorioAlEnlazar(Type tipo, string propiedad)
        {
            Assert.True(tipo.GetProperty(propiedad)!.IsDefined(typeof(BindRequiredAttribute)));
        }

        [Fact]
        [Trait("HU", "HU-04")]
        [Trait("CA", "CA-13")]
        public void RechazarSolicitud_DatosDeLaSolicitud_NoSeEnlazanDesdeElFormulario()
        {
            var solicitud = Propiedad<RechazarSolicitudViewModel>(nameof(RechazarSolicitudViewModel.Solicitud));

            Assert.True(solicitud.IsDefined(typeof(BindNeverAttribute)));
        }
    }
}
