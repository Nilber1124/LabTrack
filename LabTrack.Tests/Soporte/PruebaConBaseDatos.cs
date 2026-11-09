using LabTrack.Data;
using LabTrack.Models;
using LabTrack.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;

namespace LabTrack.Tests.Soporte
{
    public abstract class PruebaConBaseDatos : IAsyncLifetime
    {
        protected const int IdRolEstudiante = 1;
        protected const int IdRolEncargado = 2;
        protected const int IdComputo = 1;
        protected const int IdElectronica = 2;
        protected const int IdRedes = 3;

        protected static readonly DateTime Ahora = new(2026, 11, 9, 10, 0, 0);
        protected static readonly DateOnly Manana = new(2026, 11, 10);

        protected PruebaConBaseDatos(BaseDatosFixture baseDatos)
        {
            BaseDatos = baseDatos;
            Reloj = RelojPrueba.En(Ahora);
        }

        protected BaseDatosFixture BaseDatos { get; }
        protected FakeTimeProvider Reloj { get; }

        public Task InitializeAsync() => BaseDatos.LimpiarAsync();
        public Task DisposeAsync() => Task.CompletedTask;

        protected static DateTime HoraManana(int hora, int minuto = 0)
            => Manana.ToDateTime(new TimeOnly(hora, minuto));

        protected async Task<Usuario> CrearUsuarioAsync(
            string codigo, string contrasena = "clave-prueba", int idRol = IdRolEstudiante, EstadoUsuario estado = EstadoUsuario.Activo)
        {
            await using var contexto = BaseDatos.CrearContexto();
            var usuario = new Usuario { Codigo = codigo, Nombre = $"Nombre {codigo}", IdRol = idRol, Estado = estado };
            usuario.PasswordHash = new PasswordHasher<Usuario>().HashPassword(usuario, contrasena);
            contexto.Usuarios.Add(usuario);
            await contexto.SaveChangesAsync();
            return usuario;
        }

        protected async Task<Equipo> CrearEquipoAsync(string codigo, int idCategoria = IdComputo, string? nombre = null)
        {
            await using var contexto = BaseDatos.CrearContexto();
            var equipo = new Equipo { CodigoInventario = codigo, Nombre = nombre ?? $"Equipo {codigo}", IdCategoria = idCategoria, Laboratorio = "Lab. A-204" };
            contexto.Equipos.Add(equipo);
            await contexto.SaveChangesAsync();
            return equipo;
        }

        protected async Task<Reserva> CrearReservaAsync(
            int idUsuario, int idEquipo, DateTime inicio, DateTime fin, EstadoReserva estado = EstadoReserva.Pendiente)
        {
            await using var contexto = BaseDatos.CrearContexto();
            var reserva = new Reserva { IdUsuario = idUsuario, IdEquipo = idEquipo, Inicio = inicio, Fin = fin, Estado = estado };
            contexto.Reservas.Add(reserva);
            await contexto.SaveChangesAsync();
            return reserva;
        }

        protected async Task<Reserva> LeerReservaAsync(int idReserva)
        {
            await using var contexto = BaseDatos.CrearContexto();
            return await contexto.Reservas.AsNoTracking().SingleAsync(r => r.IdReserva == idReserva);
        }

        protected async Task<Usuario> LeerUsuarioAsync(int idUsuario)
        {
            await using var contexto = BaseDatos.CrearContexto();
            return await contexto.Usuarios.AsNoTracking().SingleAsync(u => u.IdUsuario == idUsuario);
        }

        protected async Task<int> ContarReservasAsync()
        {
            await using var contexto = BaseDatos.CrearContexto();
            return await contexto.Reservas.CountAsync();
        }

        // El estado del equipo se comprueba como lo ve el catálogo.
        protected async Task<EstadoEquipo> EstadoDelEquipoAsync(int idEquipo)
        {
            await using var contexto = BaseDatos.CrearContexto();
            var catalogo = await new CatalogoService(contexto, Reloj).ListarAsync(null, 1);
            return catalogo.Equipos.Single(e => e.IdEquipo == idEquipo).Estado;
        }
    }
}
