using LabTrack.Data;
using LabTrack.Reglas;
using LabTrack.Services.Interfaces;
using LabTrack.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace LabTrack.Services
{
    public sealed class CatalogoService : ICatalogoService
    {
        public const int TamanoPagina = 16;

        private readonly AppDbContext _context;
        private readonly TimeProvider _tiempo;

        public CatalogoService(AppDbContext context, TimeProvider tiempo)
        {
            _context = context;
            _tiempo = tiempo;
        }

        public async Task<CatalogoViewModel> ListarAsync(
            string? categoria, int pagina, string? busqueda = null, CancellationToken cancellationToken = default)
        {
            categoria = string.IsNullOrWhiteSpace(categoria) ? null : categoria.Trim();
            busqueda = string.IsNullOrWhiteSpace(busqueda) ? null : busqueda.Trim();
            var enCurso = EstadoPrestamo.EnCurso(_tiempo.GetLocalNow().DateTime);

            var equipos = _context.Equipos.AsNoTracking().Where(e => !e.DeBaja);
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
                Categoria = e.Categoria.Nombre,
                e.FotoUrl,
                PrestadoHasta = e.Reservas.AsQueryable().Where(enCurso).Max(r => (DateTime?)r.Fin)
            });

            var total = await consulta.CountAsync(cancellationToken);
            var disponibles = await consulta.CountAsync(e => e.PrestadoHasta == null, cancellationToken);
            pagina = PaginacionViewModel.Normalizar(pagina, total, TamanoPagina);

            var filas = await consulta
                .OrderBy(e => e.PrestadoHasta != null)
                .ThenBy(e => e.Nombre)
                .ThenBy(e => e.IdEquipo)
                .Skip((pagina - 1) * TamanoPagina)
                .Take(TamanoPagina)
                .ToListAsync(cancellationToken);

            var categorias = await _context.Categorias.AsNoTracking()
                .OrderBy(c => c.IdCategoria)
                .Select(c => new CategoriaFiltroViewModel { Nombre = c.Nombre, Total = c.Equipos.Count(e => !e.DeBaja) })
                .ToListAsync(cancellationToken);

            return new CatalogoViewModel
            {
                Equipos = filas.Select(e => new EquipoCatalogoViewModel
                {
                    IdEquipo = e.IdEquipo,
                    Nombre = e.Nombre,
                    CodigoInventario = e.CodigoInventario,
                    Categoria = e.Categoria,
                    FotoUrl = e.FotoUrl,
                    Estado = EstadoPrestamo.Calcular(e.PrestadoHasta is not null),
                    PrestadoHasta = e.PrestadoHasta
                }).ToList(),
                Categorias = categorias,
                CategoriaSeleccionada = categoria,
                Busqueda = busqueda,
                TotalDisponibles = disponibles,
                Paginacion = new PaginacionViewModel
                {
                    Pagina = pagina,
                    Total = total,
                    TamanoPagina = TamanoPagina,
                    Etiqueta = "equipos",
                    EtiquetaSingular = "equipo",
                    Parametros = new() { ["categoria"] = categoria, ["busqueda"] = busqueda }
                }
            };
        }
    }
}
