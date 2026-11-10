using LabTrack.Controllers;
using LabTrack.Data;
using LabTrack.Models;
using LabTrack.Services;
using LabTrack.Services.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

ArchivoEnv.Cargar();

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllersWithViews(opciones =>
    {
        opciones.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
        opciones.Filters.Add(new ResponseCacheAttribute { NoStore = true, Location = ResponseCacheLocation.None });
    })
    .AddSessionStateTempDataProvider();

var cadenaConexion = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Falta ConnectionStrings:DefaultConnection en la configuración.");

builder.Services.AddDbContext<AppDbContext>(opciones => opciones.UseSqlServer(cadenaConexion));

builder.Services.Configure<SeguridadOpciones>(builder.Configuration.GetSection(SeguridadOpciones.Seccion));
builder.Services.AddSingleton<TimeProvider>(new ZonaLimaTimeProvider());
builder.Services.AddScoped<IPasswordHasher<Usuario>, PasswordHasher<Usuario>>();
builder.Services.AddScoped<IAutenticacionService, AutenticacionService>();
builder.Services.AddScoped<ICatalogoService, CatalogoService>();
builder.Services.AddScoped<IReservaService, ReservaService>();
builder.Services.AddScoped<IDevolucionService, DevolucionService>();
builder.Services.AddScoped<IEquipoService, EquipoService>();
builder.Services.AddScoped<IUsuarioService, UsuarioService>();
builder.Services.Configure<CloudinaryOpciones>(builder.Configuration.GetSection(CloudinaryOpciones.Seccion));
builder.Services.AddSingleton<IAlmacenImagenes, AlmacenImagenesCloudinary>();

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(opciones =>
{
    opciones.Cookie.Name = ".LabTrack.Sesion";
    opciones.Cookie.HttpOnly = true;
    opciones.Cookie.IsEssential = true;
    opciones.IdleTimeout = TimeSpan.FromMinutes(30);
});

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(opciones =>
    {
        opciones.Cookie.Name = ".LabTrack.Auth";
        opciones.Cookie.HttpOnly = true;
        opciones.LoginPath = "/Login";
        opciones.AccessDeniedPath = "/Login/Denegado";
        opciones.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        opciones.SlidingExpiration = true;

        // Una cuenta dada de baja pierde el acceso de inmediato, aunque tenga la sesión abierta.
        opciones.Events.OnValidatePrincipal = async contexto =>
        {
            var idUsuario = contexto.Principal?.ObtenerIdUsuario();
            var autenticacion = contexto.HttpContext.RequestServices.GetRequiredService<IAutenticacionService>();

            if (idUsuario is null || !await autenticacion.EstaActivoAsync(idUsuario.Value, contexto.HttpContext.RequestAborted))
            {
                contexto.RejectPrincipal();
                await contexto.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            }
        };
    });

// Todo lo que no declare lo contrario exige un usuario autenticado.
builder.Services.AddAuthorization(opciones =>
{
    opciones.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
});

var app = builder.Build();

using (var alcance = app.Services.CreateScope())
{
    var contexto = alcance.ServiceProvider.GetRequiredService<AppDbContext>();
    await contexto.Database.MigrateAsync();

    if (app.Environment.IsDevelopment())
    {
        await DbSeeder.SembrarAsync(
            contexto,
            app.Configuration,
            alcance.ServiceProvider.GetRequiredService<IPasswordHasher<Usuario>>(),
            alcance.ServiceProvider.GetRequiredService<TimeProvider>(),
            app.Logger);
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    // Ante un error se redirige con GET a la página de error: así no hace falta desactivar la
    // validación antiforgery para reejecutar una petición POST que falló.
    app.UseExceptionHandler(manejo => manejo.Run(contexto =>
    {
        contexto.Response.Redirect($"/Login/Error?id={Uri.EscapeDataString(contexto.TraceIdentifier)}");
        return Task.CompletedTask;
    }));
}

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();
app.UseSession();

// Los recursos estáticos son públicos: la política global no debe redirigirlos al login.
app.MapStaticAssets().AllowAnonymous();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Login}/{action=Index}/{id?}")
    .WithStaticAssets();

await app.RunAsync();
