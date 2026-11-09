using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using LabTrack.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace LabTrack.Tests;

public sealed class SolicitudesFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    // Solo se crea y elimina esta base temporal, nunca la base LabTrack del usuario.
    private readonly string _database = "LabTrack_HU04_" + Guid.NewGuid().ToString("N");
    private string ConnectionString
    {
        get
        {
            var builder = new SqlConnectionStringBuilder(
                Environment.GetEnvironmentVariable("LABTRACK_TEST_SQLSERVER")
                ?? "Server=localhost;Integrated Security=True;TrustServerCertificate=True;");
            builder.InitialCatalog = _database;
            return builder.ConnectionString;
        }
    }

    public ApplicationDbContext CrearContexto() => new(
        new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(ConnectionString).Options);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<ApplicationDbContext>();
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<ApplicationDbContext>>();
            services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(ConnectionString));
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = "Pruebas";
                options.DefaultChallengeScheme = "Pruebas";
                options.DefaultForbidScheme = "Pruebas";
            }).AddScheme<AuthenticationSchemeOptions, PruebasAuthenticationHandler>("Pruebas", _ => { });
        });
    }

    public async Task InitializeAsync()
    {
        await using var context = CrearContexto();
        await context.Database.MigrateAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await using var context = CrearContexto();
        await context.Database.EnsureDeletedAsync();
        await DisposeAsync();
    }

    public HttpClient CrearCliente(long? usuarioId = null, string rol = "Encargado")
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });
        if (usuarioId.HasValue)
        {
            client.DefaultRequestHeaders.Add("X-Test-User", usuarioId.Value.ToString());
            client.DefaultRequestHeaders.Add("X-Test-Role", rol);
        }
        return client;
    }
}

// Este esquema existe exclusivamente en el proyecto de pruebas.
internal sealed class PruebasAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("X-Test-User", out var id))
            return Task.FromResult(AuthenticateResult.NoResult());
        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, id.ToString()),
            new Claim(ClaimTypes.Role, Request.Headers["X-Test-Role"].ToString())
        ], Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(
            new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)));
    }
}
