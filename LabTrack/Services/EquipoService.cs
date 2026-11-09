using LabTrack.Data;
using LabTrack.Models;
using LabTrack.Reglas;
using LabTrack.Services.Interfaces;
using LabTrack.ViewModels;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace LabTrack.Services
{
    public sealed class EquipoService : IEquipoService
    {
        public const int TamanoPagina = 20;
        private const int ErrorClaveDuplicada = 2601;
        private const int ErrorRestriccionUnica = 2627;

        private readonly AppDbContext _context;
        private readonly TimeProvider _tiempo;
        private readonly IAlmacenImagenes _imagenes;

        public EquipoService(AppDbContext context, TimeProvider tiempo, IAlmacenImagenes imagenes)
        {
            _context = context;
            _tiempo = tiempo;
            _imagenes = imagenes;
        }

        public async Task<GestionEquiposViewModel> ListarAsync(
            string? busqueda, string? categoria, int pagina, CancellationToken cancellationToken = default)
        {
            busqueda = string.IsNullOrWhiteSpace(busqueda) ? null : busqueda.Trim();
            categoria = string.IsNullOrWhiteSpace(categoria) ? null : categoria.Trim();
            var enCurso = EstadoPrestamo.EnCurso(_tiempo.GetLocalNow().DateTime);

            var equipos = _context.Equipos.AsNoTracking();
            if (categoria is not null)
            {
                equipos = equipos.Where(e => e.Categoria.Nombre == categoria);
            }

            if (busqueda is not null)
            {
                equipos = equipos.Where(e => e.Nombre.Contains(busqueda) || e.CodigoInventario.Contains(busqueda));
            }

            var consulta = equipos.Select(e => new
            {
                e.IdEquipo,
                e.Nombre,
                e.CodigoInventario,
                e.IdCategoria,
                Categoria = e.Categoria.Nombre,
                e.Laboratorio,
                e.FotoUrl,
                e.DeBaja,
                PrestadoHasta = e.Reservas.AsQueryable().Where(enCurso).Max(r => (DateTime?)r.Fin)
            });

            var total = await consulta.CountAsync(cancellationToken);
            var deBaja = await consulta.CountAsync(e => e.DeBaja, cancellationToken);
            var disponibles = await consulta.CountAsync(e => !e.DeBaja && e.PrestadoHasta == null, cancellationToken);
            pagina = PaginacionViewModel.Normalizar(pagina, total, TamanoPagina);

            var filas = await consulta
                .OrderBy(e => e.Nombre)
                .ThenBy(e => e.IdEquipo)
                .Skip((pagina - 1) * TamanoPagina)
                .Take(TamanoPagina)
                .ToListAsync(cancellationToken);

            var categorias = await ListarCategoriasAsync(cancellationToken);

            return new GestionEquiposViewModel
            {
                Equipos = filas.Select(e => new EquipoGestionViewModel
                {
                    IdEquipo = e.IdEquipo,
                    Nombre = e.Nombre,
                    CodigoInventario = e.CodigoInventario,
                    IdCategoria = e.IdCategoria,
                    Categoria = e.Categoria,
                    Laboratorio = e.Laboratorio,
                    FotoUrl = e.FotoUrl,
                    DeBaja = e.DeBaja,
                    Prestado = !e.DeBaja && e.PrestadoHasta is not null
                }).ToList(),
                Categorias = categorias,
                CategoriaSeleccionada = categoria,
                Busqueda = busqueda,
                TotalDisponibles = disponibles,
                TotalDeBaja = deBaja,
                Formulario = new FormularioEquipoViewModel { Categorias = categorias },
                Paginacion = new PaginacionViewModel
                {
                    Pagina = pagina,
                    Total = total,
                    TamanoPagina = TamanoPagina,
                    Etiqueta = "equipos",
                    EtiquetaSingular = "equipo",
                    Parametros = new() { ["busqueda"] = busqueda, ["categoria"] = categoria }
                }
            };
        }

        public async Task<FormularioEquipoViewModel> ObtenerFormularioNuevoAsync(CancellationToken cancellationToken = default)
            => new() { Categorias = await ListarCategoriasAsync(cancellationToken) };

        public async Task<FormularioEquipoViewModel?> ObtenerFormularioEdicionAsync(int idEquipo, CancellationToken cancellationToken = default)
        {
            var formulario = await _context.Equipos.AsNoTracking()
                .Where(e => e.IdEquipo == idEquipo)
                .Select(e => new FormularioEquipoViewModel
                {
                    IdEquipo = e.IdEquipo,
                    Nombre = e.Nombre,
                    CodigoInventario = e.CodigoInventario,
                    IdCategoria = e.IdCategoria,
                    Laboratorio = e.Laboratorio,
                    FotoActual = e.FotoUrl
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (formulario is not null)
            {
                formulario.Categorias = await ListarCategoriasAsync(cancellationToken);
            }

            return formulario;
        }

        public async Task<ResultadoOperacion> GuardarAsync(
            DatosEquipo datos, ImagenSubida? imagen, CancellationToken cancellationToken = default)
        {
            var codigo = datos.CodigoInventario.Trim();
            var nombre = datos.Nombre.Trim();

            if (!Laboratorios.Opciones.Contains(datos.Laboratorio))
            {
                return ResultadoOperacion.Fallo(TipoResultado.Validacion, Mensajes.LaboratorioNoValido);
            }

            if (!await _context.Categorias.AnyAsync(c => c.IdCategoria == datos.IdCategoria, cancellationToken))
            {
                return ResultadoOperacion.Fallo(TipoResultado.Validacion, Mensajes.CategoriaNoValida);
            }

            var titular = await _context.Equipos.AsNoTracking()
                .Where(e => e.CodigoInventario == codigo && e.IdEquipo != datos.IdEquipo)
                .Select(e => e.Nombre)
                .FirstOrDefaultAsync(cancellationToken);
            if (titular is not null)
            {
                return CodigoRepetido(codigo, titular);
            }

            var equipo = datos.IdEquipo == 0
                ? new Equipo()
                : await _context.Equipos.FirstOrDefaultAsync(e => e.IdEquipo == datos.IdEquipo, cancellationToken);
            if (equipo is null)
            {
                return ResultadoOperacion.Fallo(TipoResultado.NoEncontrado, Mensajes.EquipoNoEncontrado);
            }

            var extension = imagen is null ? null : await DetectarExtensionValidaAsync(imagen, cancellationToken);
            if (imagen is not null && extension is null)
            {
                return ResultadoOperacion.Fallo(TipoResultado.Validacion, Mensajes.ImagenNoSubida, Mensajes.ImagenNoValida);
            }

            var fotoAnterior = equipo.FotoUrl;
            var fotoNueva = imagen is null ? null : await _imagenes.GuardarAsync(imagen.Contenido, extension!, cancellationToken);
            if (imagen is not null && fotoNueva is null)
            {
                return ResultadoOperacion.Fallo(TipoResultado.Validacion, Mensajes.ImagenNoSubida, Mensajes.ServicioImagenesNoDisponible);
            }

            equipo.Nombre = nombre;
            equipo.CodigoInventario = codigo;
            equipo.IdCategoria = datos.IdCategoria;
            equipo.Laboratorio = datos.Laboratorio;
            equipo.FotoUrl = fotoNueva ?? fotoAnterior;

            if (datos.IdEquipo == 0)
            {
                _context.Equipos.Add(equipo);
            }

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                await DescartarAsync(fotoNueva, cancellationToken);
                return ResultadoOperacion.Fallo(TipoResultado.Conflicto, Mensajes.ConflictoConcurrenciaEquipo);
            }
            catch (DbUpdateException ex) when (EsViolacionDeUnicidad(ex))
            {
                await DescartarAsync(fotoNueva, cancellationToken);
                return ResultadoOperacion.Fallo(TipoResultado.Conflicto, Mensajes.CodigoRepetido);
            }

            if (fotoNueva is not null)
            {
                await DescartarAsync(fotoAnterior, cancellationToken);
            }

            return ResultadoOperacion.Ok($"Equipo guardado. {equipo.Nombre} ya está en el catálogo.");
        }

        public async Task<ResultadoOperacion> DarDeBajaAsync(int idEquipo, CancellationToken cancellationToken = default)
        {
            await using var transaccion = await _context.Database.BeginTransactionAsync(cancellationToken);

            // El mismo bloqueo que usa la aprobación: nadie presta el equipo mientras se da de baja.
            var existe = await _context.Equipos
                .FromSqlInterpolated($"SELECT * FROM [Equipos] WITH (UPDLOCK, HOLDLOCK) WHERE [IdEquipo] = {idEquipo}")
                .AsNoTracking()
                .AnyAsync(cancellationToken);
            if (!existe)
            {
                return ResultadoOperacion.Fallo(TipoResultado.NoEncontrado, Mensajes.EquipoNoEncontrado);
            }

            var equipo = await _context.Equipos.SingleAsync(e => e.IdEquipo == idEquipo, cancellationToken);
            if (equipo.DeBaja)
            {
                return ResultadoOperacion.Fallo(TipoResultado.EstadoInvalido, Mensajes.EquipoYaDeBaja);
            }

            var prestadoHasta = await _context.Reservas.AsNoTracking()
                .Where(r => r.IdEquipo == idEquipo)
                .Where(EstadoPrestamo.EnCurso(_tiempo.GetLocalNow().DateTime))
                .MaxAsync(r => (DateTime?)r.Fin, cancellationToken);
            if (prestadoHasta is not null)
            {
                return ResultadoOperacion.Fallo(
                    TipoResultado.Conflicto,
                    Mensajes.NoSePuedeDarDeBaja,
                    $"{equipo.CodigoInventario} está prestado hasta las {Formato.Hora(prestadoHasta.Value)}. Registra su devolución primero.");
            }

            equipo.DeBaja = true;
            await _context.SaveChangesAsync(cancellationToken);
            await transaccion.CommitAsync(cancellationToken);

            return ResultadoOperacion.Ok($"{equipo.Nombre} se dio de baja y ya no aparece en el catálogo.");
        }

        public async Task<ResultadoOperacion> ReactivarAsync(int idEquipo, CancellationToken cancellationToken = default)
        {
            var equipo = await _context.Equipos.FirstOrDefaultAsync(e => e.IdEquipo == idEquipo, cancellationToken);
            if (equipo is null)
            {
                return ResultadoOperacion.Fallo(TipoResultado.NoEncontrado, Mensajes.EquipoNoEncontrado);
            }

            if (!equipo.DeBaja)
            {
                return ResultadoOperacion.Fallo(TipoResultado.EstadoInvalido, Mensajes.EquipoNoEstaDeBaja);
            }

            equipo.DeBaja = false;
            await _context.SaveChangesAsync(cancellationToken);

            return ResultadoOperacion.Ok($"{equipo.Nombre} se reactivó y ya está en el catálogo.");
        }

        private Task<List<CategoriaFiltroViewModel>> ListarCategoriasAsync(CancellationToken cancellationToken)
            => _context.Categorias.AsNoTracking()
                .OrderBy(c => c.IdCategoria)
                .Select(c => new CategoriaFiltroViewModel { IdCategoria = c.IdCategoria, Nombre = c.Nombre, Total = c.Equipos.Count })
                .ToListAsync(cancellationToken);

        private static async Task<string?> DetectarExtensionValidaAsync(ImagenSubida imagen, CancellationToken cancellationToken)
        {
            if (!ValidacionImagen.TamanoValido(imagen.Bytes))
            {
                return null;
            }

            var cabecera = new byte[ValidacionImagen.BytesDeCabecera];
            var leidos = await imagen.Contenido.ReadAsync(cabecera, cancellationToken);
            imagen.Contenido.Position = 0;

            return ValidacionImagen.DetectarExtension(cabecera.AsSpan(0, leidos));
        }

        private async Task DescartarAsync(string? url, CancellationToken cancellationToken)
        {
            if (url is not null)
            {
                await _imagenes.EliminarAsync(url, cancellationToken);
            }
        }

        private static ResultadoOperacion CodigoRepetido(string codigo, string titular)
            => ResultadoOperacion.Fallo(TipoResultado.Conflicto, Mensajes.CodigoRepetido, $"{codigo} pertenece a {titular}.");

        private static bool EsViolacionDeUnicidad(DbUpdateException ex)
            => ex.InnerException is SqlException { Number: ErrorClaveDuplicada or ErrorRestriccionUnica };
    }
}
