using LabTrack.Data;
using LabTrack.Models;
using LabTrack.Services;
using LabTrack.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllersWithViews(opciones =>
    {
        opciones.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
        opciones.Filters.Add(new ResponseCacheAttribute { NoStore = true, Location = ResponseCacheLocation.None });
    })
    .AddSessionStateTempDataProvider();

var cadenaConexion = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Falta ConnectionStrings:DefaultConnection (User Secrets o variable de entorno).");

builder.Services.AddDbContext<AppDbContext>(opciones => opciones.UseSqlServer(cadenaConexion));

builder.Services.Configure<SeguridadOpciones>(builder.Configuration.GetSection(SeguridadOpciones.Seccion));
builder.Services.AddSingleton<TimeProvider>(new ZonaLimaTimeProvider());
builder.Services.AddScoped<IPasswordHasher<Usuario>, PasswordHasher<Usuario>>();
builder.Services.AddScoped<IAutenticacionService, AutenticacionService>();
builder.Services.AddScoped<ICatalogoService, CatalogoService>();

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
            app.Logger);
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Login/Error");
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

app.Run();
