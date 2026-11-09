using LabTrack.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;

namespace LabTrack.Tests.Seguridad
{
    public class AutorizacionControladoresTests
    {
        private static IEnumerable<Type> Controladores()
            => typeof(LoginController).Assembly.GetTypes()
                .Where(t => typeof(Controller).IsAssignableFrom(t) && !t.IsAbstract);

        private static bool EsAnonimo(Type controlador)
            => controlador.IsDefined(typeof(AllowAnonymousAttribute))
                || controlador.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                    .Any(m => m.IsDefined(typeof(AllowAnonymousAttribute)));

        private static string? Roles<T>() => typeof(T).GetCustomAttribute<AuthorizeAttribute>()?.Roles;

        [Fact]
        [Trait("HU", "HU-01")]
        [Trait("CA", "CA-01")]
        public void Controladores_SoloLogin_EsAnonimo()
        {
            var anonimos = Controladores().Where(EsAnonimo);

            Assert.Equal(typeof(LoginController), Assert.Single(anonimos));
        }

        [Fact]
        [Trait("HU", "HU-01")]
        [Trait("CA", "CA-01")]
        public void Controladores_ExceptoLogin_TienenAuthorize()
        {
            var sinAutorizacion = Controladores()
                .Where(t => t != typeof(LoginController) && !t.IsDefined(typeof(AuthorizeAttribute)));

            Assert.Empty(sinAutorizacion);
        }

        [Fact]
        [Trait("HU", "HU-02")]
        [Trait("CA", "CA-04")]
        public void Catalogo_RolEsperado_EsEstudiante()
        {
            Assert.Equal("ESTUDIANTE", Roles<CatalogoController>());
        }

        [Fact]
        [Trait("HU", "HU-03")]
        [Trait("CA", "CA-08")]
        public void Reservas_RolEsperado_EsEstudiante()
        {
            Assert.Equal("ESTUDIANTE", Roles<ReservasController>());
        }

        [Fact]
        [Trait("HU", "HU-04")]
        [Trait("CA", "CA-11")]
        public void Solicitudes_RolesEsperados_SonEncargadoYAdministrador()
        {
            Assert.Equal("ENCARGADO,ADMINISTRADOR", Roles<SolicitudesController>());
        }

        [Fact]
        [Trait("HU", "HU-05")]
        [Trait("CA", "CA-14")]
        public void Devoluciones_RolesEsperados_SonEncargadoYAdministrador()
        {
            Assert.Equal("ENCARGADO,ADMINISTRADOR", Roles<DevolucionesController>());
        }

        [Fact]
        [Trait("HU", "HU-05")]
        [Trait("CA", "CA-16")]
        public void Penalizados_RolesEsperados_SonEncargadoYAdministrador()
        {
            Assert.Equal("ENCARGADO,ADMINISTRADOR", Roles<PenalizadosController>());
        }

        [Fact]
        [Trait("HU", "ADM")]
        [Trait("CA", "ADM-USU")]
        public void Usuarios_RolEsperado_EsSoloAdministrador()
        {
            Assert.Equal("ADMINISTRADOR", Roles<UsuariosController>());
        }

        [Fact]
        [Trait("HU", "ADM")]
        [Trait("CA", "ADM-CA2")]
        public void Equipos_RolEsperado_EsSoloAdministrador()
        {
            Assert.Equal("ADMINISTRADOR", Roles<EquiposController>());
        }

        [Fact]
        [Trait("HU", "HU-01")]
        [Trait("CA", "CA-01")]
        public void Controladores_AccionesPost_NoIgnoranAntiforgery()
        {
            var inseguras = Controladores()
                .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                .Where(m => m.IsDefined(typeof(HttpPostAttribute)) && m.IsDefined(typeof(IgnoreAntiforgeryTokenAttribute)));

            Assert.Empty(inseguras);
        }
    }
}
