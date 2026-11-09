using LabTrack.Models;
using LabTrack.Tests.Soporte;

namespace LabTrack.Tests.Models
{
    public class ModelosValidacionTests
    {
        private static Usuario UsuarioValido() => new() { Codigo = "20231458", Nombre = "Valeria Quispe", PasswordHash = "hash", IdRol = 1 };
        private static Equipo EquipoValido() => new() { Nombre = "Laptop Dell", CodigoInventario = "LT-0231", IdCategoria = 1, Laboratorio = "Lab. A-204" };
        private static Reserva ReservaValida() => new() { IdUsuario = 1, IdEquipo = 1 };

        [Fact]
        [Trait("HU", "HU-01")]
        [Trait("CA", "CA-01")]
        public void Usuario_DatosCompletos_NoTieneErrores()
        {
            Assert.Empty(Validacion.Errores(UsuarioValido()));
        }

        [Theory]
        [Trait("HU", "HU-01")]
        [Trait("CA", "CA-01")]
        [InlineData("")]
        [InlineData("codigo con espacios")]
        [InlineData("123456789012345678901")]
        public void Usuario_CodigoInvalido_TieneError(string codigo)
        {
            var usuario = UsuarioValido();
            usuario.Codigo = codigo;

            Assert.Contains(Validacion.Errores(usuario), e => e.MemberNames.Contains(nameof(Usuario.Codigo)));
        }

        [Fact]
        [Trait("HU", "HU-01")]
        [Trait("CA", "CA-01")]
        public void Usuario_SinHash_TieneError()
        {
            var usuario = UsuarioValido();
            usuario.PasswordHash = string.Empty;

            Assert.Contains(Validacion.Errores(usuario), e => e.MemberNames.Contains(nameof(Usuario.PasswordHash)));
        }

        [Fact]
        [Trait("HU", "HU-01")]
        [Trait("CA", "CA-03")]
        public void Usuario_IntentosNegativos_TieneError()
        {
            var usuario = UsuarioValido();
            usuario.IntentosFallidos = -1;

            Assert.Contains(Validacion.Errores(usuario), e => e.MemberNames.Contains(nameof(Usuario.IntentosFallidos)));
        }

        [Fact]
        [Trait("HU", "HU-02")]
        [Trait("CA", "CA-04")]
        public void Equipo_DatosCompletos_NoTieneErrores()
        {
            Assert.Empty(Validacion.Errores(EquipoValido()));
        }

        [Fact]
        [Trait("HU", "HU-02")]
        [Trait("CA", "CA-04")]
        public void Equipo_SinNombre_TieneError()
        {
            var equipo = EquipoValido();
            equipo.Nombre = string.Empty;

            Assert.Contains(Validacion.Errores(equipo), e => e.MemberNames.Contains(nameof(Equipo.Nombre)));
        }

        [Fact]
        [Trait("HU", "HU-02")]
        [Trait("CA", "CA-04")]
        public void Equipo_CodigoInventarioExcedido_TieneError()
        {
            var equipo = EquipoValido();
            equipo.CodigoInventario = new string('X', 31);

            Assert.Contains(Validacion.Errores(equipo), e => e.MemberNames.Contains(nameof(Equipo.CodigoInventario)));
        }

        [Fact]
        [Trait("HU", "HU-02")]
        [Trait("CA", "CA-05")]
        public void Equipo_SinCategoria_TieneError()
        {
            var equipo = EquipoValido();
            equipo.IdCategoria = 0;

            Assert.Contains(Validacion.Errores(equipo), e => e.MemberNames.Contains(nameof(Equipo.IdCategoria)));
        }

        [Fact]
        [Trait("HU", "HU-02")]
        [Trait("CA", "CA-05")]
        public void Categoria_SinNombre_TieneError()
        {
            Assert.NotEmpty(Validacion.Errores(new Categoria()));
        }

        [Fact]
        [Trait("HU", "HU-01")]
        [Trait("CA", "CA-01")]
        public void Rol_SinNombre_TieneError()
        {
            Assert.NotEmpty(Validacion.Errores(new Rol()));
        }

        [Fact]
        [Trait("HU", "HU-03")]
        [Trait("CA", "CA-08")]
        public void Reserva_DatosCompletos_NoTieneErrores()
        {
            Assert.Empty(Validacion.Errores(ReservaValida()));
        }

        [Fact]
        [Trait("HU", "HU-03")]
        [Trait("CA", "CA-08")]
        public void Reserva_SinEquipo_TieneError()
        {
            var reserva = ReservaValida();
            reserva.IdEquipo = 0;

            Assert.Contains(Validacion.Errores(reserva), e => e.MemberNames.Contains(nameof(Reserva.IdEquipo)));
        }

        [Fact]
        [Trait("HU", "HU-04")]
        [Trait("CA", "CA-13")]
        public void Reserva_MotivoExcedido_TieneError()
        {
            var reserva = ReservaValida();
            reserva.MotivoRechazo = new string('x', 201);

            Assert.Contains(Validacion.Errores(reserva), e => e.MemberNames.Contains(nameof(Reserva.MotivoRechazo)));
        }
    }
}
