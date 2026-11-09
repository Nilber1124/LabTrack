using System.Text.RegularExpressions;
using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace LabTrack.Tests
{
    public class VistaPreviaTests
    {
        private static WebApplicationFactory<Program> CrearAplicacion(string environment)
            => new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment(environment);
                builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:DefaultConnection"] = "Server=servidor-inexistente;Database=NoUsar;Integrated Security=True;"
                    }));
            });

        [Fact]
        public async Task Vista_previa_funciona_sin_login_ni_base_y_usa_rutas_de_demo()
        {
            await using var app = CrearAplicacion("Development");
            using var client = app.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
            var response = await client.GetAsync("/VistaPrevia/Solicitudes");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var html = await response.Content.ReadAsStringAsync();
            Assert.Contains("Datos ficticios", html);
            Assert.Contains("Osciloscopio digital", html);
            Assert.Contains("/VistaPrevia/Aprobar", html);
            Assert.Contains("confirmar-aprobacion", html);
            Assert.Contains("confirmar-rechazo", html);
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/Solicitudes")).StatusCode);
            var resultado = await client.GetStringAsync("/VistaPrevia/Solicitudes?busqueda=Raspberry");
            Assert.Contains("Raspberry", resultado);
            Assert.DoesNotContain("Osciloscopio digital", resultado);
        }

        [Theory]
        [InlineData("Production")]
        [InlineData("Staging")]
        [InlineData("Testing")]
        public async Task Vista_previa_no_se_publica_fuera_de_desarrollo(string environment)
        {
            await using var app = CrearAplicacion(environment);
            using var client = app.CreateClient();
            Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/VistaPrevia/Solicitudes")).StatusCode);
        }

        private static string Token(string html)
        {
            var match = Regex.Match(html, """name="__RequestVerificationToken"[^>]*value="([^"]+)" """.TrimEnd());
            Assert.True(match.Success);
            return WebUtility.HtmlDecode(match.Groups[1].Value);
        }

        private static async Task<HttpResponseMessage> Post(HttpClient client, string accion, string token, long id = 1, string motivo = "")
            => await client.PostAsync("/VistaPrevia/" + accion, new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Id"] = id.ToString(), ["Motivo"] = motivo, ["__RequestVerificationToken"] = token
            }));

        [Fact]
        public async Task Demo_aprueba_detecta_conflictos_y_mantiene_sesiones_aisladas()
        {
            await using var app = CrearAplicacion("Development");
            using var client = app.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
            var html = await client.GetStringAsync("/VistaPrevia/Solicitudes");
            var token = Token(html);
            Assert.Equal(HttpStatusCode.Redirect, (await Post(client, "Aprobar", token)).StatusCode);
            var aprobado = await client.GetStringAsync("/VistaPrevia/Solicitudes");
            Assert.Contains("Aprobada", aprobado);
            Assert.Contains("resultado-solicitud", aprobado);
            Assert.Contains("data-auto-open", aprobado);
            Assert.Contains("Equipo prestado para ese bloque.", aprobado);
            Assert.Equal(HttpStatusCode.Redirect, (await Post(client, "Aprobar", token, 4)).StatusCode);
            var conflicto = await client.GetStringAsync("/VistaPrevia/Solicitudes");
            Assert.Contains("El equipo ya", conflicto);

            using var otraSesion = app.CreateClient();
            Assert.DoesNotContain("Equipo prestado para ese bloque.", await otraSesion.GetStringAsync("/VistaPrevia/Solicitudes"));
            Assert.Equal(HttpStatusCode.Redirect, (await Post(client, "Reiniciar", token)).StatusCode);
            Assert.DoesNotContain("Equipo prestado para ese bloque.", await client.GetStringAsync("/VistaPrevia/Solicitudes"));
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task Demo_exige_motivo_y_conserva_el_rechazo(string invalido)
        {
            await using var app = CrearAplicacion("Development");
            using var client = app.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
            var html = await client.GetStringAsync("/VistaPrevia/Rechazar/1");
            Assert.Equal(HttpStatusCode.OK, (await Post(client, "Rechazar", Token(html), motivo: invalido)).StatusCode);
            var response = await Post(client, "Rechazar", Token(html), motivo: "Equipo en mantenimiento");
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            var rechazo = await client.GetStringAsync("/VistaPrevia/Solicitudes");
            Assert.Contains("Rechazada", rechazo);
            Assert.Contains("Equipo en mantenimiento", rechazo);
        }

        [Fact]
        public async Task Demo_requiere_antifalsificacion_para_modificar_la_sesion()
        {
            await using var app = CrearAplicacion("Development");
            using var client = app.CreateClient();
            var response = await client.PostAsync("/VistaPrevia/Aprobar", new FormUrlEncodedContent(new Dictionary<string, string> { ["id"] = "1" }));
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }
}
