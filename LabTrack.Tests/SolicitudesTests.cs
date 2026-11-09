using System.Net;
using System.Text.RegularExpressions;
using LabTrack.Models;
using LabTrack.Services;
using LabTrack.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LabTrack.Tests;

public sealed class SolicitudesTests(SolicitudesFactory factory) : IClassFixture<SolicitudesFactory>, IAsyncLifetime
{
    private long _encargadoId;
    private long _estudianteId;
    private long _reservaId;
    private long _otraReservaId;

    public async Task InitializeAsync()
    {
        await using var db = factory.CrearContexto();
        await db.Penalizaciones.ExecuteDeleteAsync();
        await db.Reservas.ExecuteDeleteAsync();
        await db.Equipos.ExecuteDeleteAsync();
        await db.Categorias.ExecuteDeleteAsync();
        await db.Usuarios.ExecuteDeleteAsync();
        var encargado = new Usuario { Codigo = "ENC", Nombre = "Encargado de prueba", PasswordHash = "solo-pruebas", Rol = RolUsuario.Encargado };
        var estudiante = new Usuario { Codigo = "EST", Nombre = "Estudiante de prueba", PasswordHash = "solo-pruebas" };
        var equipo = new Equipo
        {
            CodigoInventario = "LAB-01", Nombre = "Microscopio",
            Categoria = new Categoria { Nombre = "Laboratorio" }
        };
        var inicio = new DateTimeOffset(2030, 1, 1, 10, 0, 0, TimeSpan.Zero);
        var primera = new Reserva { Estudiante = estudiante, Equipo = equipo, Inicio = inicio, Fin = inicio.AddHours(2) };
        var segunda = new Reserva { Estudiante = estudiante, Equipo = equipo, Inicio = inicio.AddHours(1), Fin = inicio.AddHours(3) };
        db.Add(encargado);
        db.AddRange(primera, segunda);
        await db.SaveChangesAsync();
        _encargadoId = encargado.Id;
        _estudianteId = estudiante.Id;
        _reservaId = primera.Id;
        _otraReservaId = segunda.Id;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private static string Token(string html)
    {
        var match = Regex.Match(html, """name="__RequestVerificationToken"[^>]*value="([^"]+)" """.TrimEnd());
        Assert.True(match.Success, "La vista debe generar un token antifalsificaciÃƒÆ’Ã‚Â³n.");
        return WebUtility.HtmlDecode(match.Groups[1].Value);
    }

    [Fact]
    public async Task Solo_encargados_autenticados_pueden_entrar()
    {
        using var anonimo = factory.CrearCliente();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonimo.GetAsync("/Solicitudes")).StatusCode);
        using var estudiante = factory.CrearCliente(_estudianteId, "Estudiante");
        Assert.Equal(HttpStatusCode.Forbidden, (await estudiante.GetAsync("/Solicitudes")).StatusCode);
        using var rolFalso = factory.CrearCliente(_estudianteId);
        Assert.Equal(HttpStatusCode.Forbidden, (await rolFalso.GetAsync("/Solicitudes")).StatusCode);
    }

    [Fact]
    public async Task Listado_muestra_solo_pendientes_y_datos_del_estudiante_y_equipo()
    {
        await using var db = factory.CrearContexto();
        var service = new SolicitudesService(db, TimeProvider.System);
        await service.RechazarAsync(_encargadoId, _otraReservaId, "Equipo en mantenimiento");
        var listado = await service.ListarPendientesAsync(_encargadoId, 1);
        var solicitud = Assert.Single(listado.Solicitudes);
        Assert.Equal(_reservaId, solicitud.Id);
        Assert.Equal("Estudiante de prueba", solicitud.Estudiante);
        Assert.Equal("Microscopio", solicitud.Equipo);
        using var client = factory.CrearCliente(_encargadoId);
        var html = await client.GetStringAsync("/Solicitudes");
        Assert.Contains("Estudiante de prueba", html);
        Assert.Contains("Aprobar", html);
        Assert.Contains("Rechazar", html);
    }

    [Fact]
    public async Task Aprobar_guarda_auditoria_y_el_equipo_figura_prestado()
    {
        using var client = factory.CrearCliente(_encargadoId);
        var html = await client.GetStringAsync("/Solicitudes");
        var result = await client.PostAsync("/Solicitudes/Aprobar", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["id"] = _reservaId.ToString(), ["__RequestVerificationToken"] = Token(html)
        }));
        Assert.Equal(HttpStatusCode.Redirect, result.StatusCode);
        await using var db = factory.CrearContexto();
        var reserva = await db.Reservas.SingleAsync(x => x.Id == _reservaId);
        Assert.Equal(EstadoReserva.Aprobada, reserva.Estado);
        Assert.Equal(_encargadoId, reserva.ResueltaPor);
        Assert.NotNull(reserva.ResueltaEn);
        var pendientes = await new SolicitudesService(db, TimeProvider.System).ListarPendientesAsync(_encargadoId, 1);
        Assert.True(Assert.Single(pendientes.Solicitudes).EquipoPrestado);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task Rechazo_no_admite_motivo_vacio(string? motivo)
    {
        using var client = factory.CrearCliente(_encargadoId);
        var html = await client.GetStringAsync($"/Solicitudes/Rechazar/{_reservaId}");
        var result = await client.PostAsync("/Solicitudes/Rechazar", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Id"] = _reservaId.ToString(), ["Motivo"] = motivo ?? "", ["__RequestVerificationToken"] = Token(html)
        }));
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        await using var db = factory.CrearContexto();
        Assert.Equal(EstadoReserva.Pendiente, (await db.Reservas.FindAsync(_reservaId))!.Estado);
        Assert.Contains("Escribe el motivo del rechazo.", await result.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Rechazo_valido_guarda_motivo_y_no_presta_equipo()
    {
        using var client = factory.CrearCliente(_encargadoId);
        var html = await client.GetStringAsync($"/Solicitudes/Rechazar/{_reservaId}");
        var result = await client.PostAsync("/Solicitudes/Rechazar", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Id"] = _reservaId.ToString(), ["Motivo"] = " Equipo en mantenimiento ",
            ["__RequestVerificationToken"] = Token(html)
        }));
        Assert.Equal(HttpStatusCode.Redirect, result.StatusCode);
        await using var db = factory.CrearContexto();
        var reserva = await db.Reservas.FindAsync(_reservaId);
        Assert.Equal(EstadoReserva.Rechazada, reserva!.Estado);
        Assert.Equal("Equipo en mantenimiento", reserva.MotivoRechazo);
        Assert.Equal(_encargadoId, reserva.ResueltaPor);
        var listado = await new SolicitudesService(db, TimeProvider.System).ListarPendientesAsync(_encargadoId, 1);
        Assert.False(Assert.Single(listado.Solicitudes).EquipoPrestado);
    }

    [Fact]
    public async Task Post_sin_token_es_rechazado()
    {
        using var client = factory.CrearCliente(_encargadoId);
        var result = await client.PostAsync("/Solicitudes/Aprobar", new FormUrlEncodedContent(new Dictionary<string, string> { ["id"] = _reservaId.ToString() }));
        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
    }

    [Fact]
    public async Task Motivo_excesivo_y_doble_resolucion_no_cambian_datos()
    {
        await using var db = factory.CrearContexto();
        var service = new SolicitudesService(db, TimeProvider.System);
        Assert.Equal(EstadoResolucion.MotivoInvalido, await service.RechazarAsync(_encargadoId, _reservaId, new string('a', 256)));
        Assert.Equal(EstadoResolucion.Completada, await service.RechazarAsync(_encargadoId, _reservaId, "Mantenimiento"));
        Assert.Equal(EstadoResolucion.YaResuelta, await service.AprobarAsync(_encargadoId, _reservaId));
        Assert.Equal(EstadoReserva.Rechazada, (await db.Reservas.FindAsync(_reservaId))!.Estado);
    }

    [Fact]
    public async Task Aprobaciones_concurrentes_superpuestas_solo_permiten_una()
    {
        await using var db1 = factory.CrearContexto();
        await using var db2 = factory.CrearContexto();
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        async Task<EstadoResolucion> Aprobar(SolicitudesService service, long id)
        {
            await gate.Task;
            return await service.AprobarAsync(_encargadoId, id);
        }
        var t1 = Aprobar(new SolicitudesService(db1, TimeProvider.System), _reservaId);
        var t2 = Aprobar(new SolicitudesService(db2, TimeProvider.System), _otraReservaId);
        gate.SetResult();
        var results = await Task.WhenAll(t1, t2);
        Assert.Single(results, x => x == EstadoResolucion.Completada);
        Assert.Single(results, x => x == EstadoResolucion.ConflictoHorario);
        await using var verification = factory.CrearContexto();
        Assert.Equal(1, await verification.Reservas.CountAsync(x => x.Estado == EstadoReserva.Aprobada));
    }

    [Fact]
    public async Task Reservas_contiguas_no_se_consideran_superpuestas()
    {
        await using var db = factory.CrearContexto();
        var primera = await db.Reservas.FindAsync(_reservaId);
        var segunda = await db.Reservas.FindAsync(_otraReservaId);
        segunda!.Inicio = primera!.Fin;
        segunda.Fin = segunda.Inicio.AddHours(1);
        await db.SaveChangesAsync();
        var service = new SolicitudesService(db, TimeProvider.System);
        Assert.Equal(EstadoResolucion.Completada, await service.AprobarAsync(_encargadoId, _reservaId));
        Assert.Equal(EstadoResolucion.Completada, await service.AprobarAsync(_encargadoId, _otraReservaId));
    }

    [Fact]
    public async Task Busqueda_filtra_por_codigo_y_pagina_fuera_de_rango_se_normaliza()
    {
        await using var db = factory.CrearContexto();
        var service = new SolicitudesService(db, TimeProvider.System);
        var encontrado = await service.ListarPendientesAsync(_encargadoId, int.MaxValue, "LAB-01");
        Assert.Equal(2, encontrado.Total);
        Assert.Equal(1, encontrado.Pagina);
        Assert.Empty((await service.ListarPendientesAsync(_encargadoId, -1, "NO-EXISTE")).Solicitudes);
        using var client = factory.CrearCliente(_encargadoId);
        var html = await client.GetStringAsync("/Solicitudes?busqueda=NO-EXISTE");
        Assert.Contains("No encontramos solicitudes", html);
    }

    [Fact]
    public async Task Aprobar_y_rechazar_la_misma_reserva_concurrentemente_conserva_una_decision()
    {
        await using var db1 = factory.CrearContexto();
        await using var db2 = factory.CrearContexto();
        var aprobar = new SolicitudesService(db1, TimeProvider.System).AprobarAsync(_encargadoId, _reservaId);
        var rechazar = new SolicitudesService(db2, TimeProvider.System).RechazarAsync(_encargadoId, _reservaId, "Mantenimiento");
        var results = await Task.WhenAll(aprobar, rechazar);
        Assert.Single(results, x => x == EstadoResolucion.Completada);
        Assert.Single(results, x => x == EstadoResolucion.YaResuelta);
        await using var db = factory.CrearContexto();
        var reserva = await db.Reservas.FindAsync(_reservaId);
        Assert.NotNull(reserva!.ResueltaEn);
        Assert.Equal(_encargadoId, reserva.ResueltaPor);
    }

    [Fact]
    public async Task Fecha_invalida_y_penalizacion_duplicada_son_rechazadas_por_SQL_Server()
    {
        await using (var db = factory.CrearContexto())
        {
            var reserva = await db.Reservas.FindAsync(_reservaId);
            reserva!.Fin = reserva.Inicio;
            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        }
        await using (var db = factory.CrearContexto())
        {
            db.Penalizaciones.Add(new Penalizacion { ReservaId = _reservaId });
            await db.SaveChangesAsync();
        }
        await using (var db = factory.CrearContexto())
        {
            db.Penalizaciones.Add(new Penalizacion { ReservaId = _reservaId });
            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        }
    }

    [Fact]
    public async Task No_aprueba_si_la_reserva_no_corresponde_a_un_estudiante()
    {
        await using var db = factory.CrearContexto();
        var reserva = await db.Reservas.FindAsync(_reservaId);
        reserva!.EstudianteId = _encargadoId;
        await db.SaveChangesAsync();
        var result = await new SolicitudesService(db, TimeProvider.System).AprobarAsync(_encargadoId, _reservaId);
        Assert.Equal(EstadoResolucion.EstudianteInvalido, result);
        await db.Entry(reserva).ReloadAsync();
        Assert.Equal(EstadoReserva.Pendiente, reserva.Estado);
    }
}
