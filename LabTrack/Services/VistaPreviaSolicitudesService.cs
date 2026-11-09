using LabTrack.Models;
using LabTrack.Services.Interfaces;
using LabTrack.ViewModels;

namespace LabTrack.Services
{
    // Simulador independiente: no utiliza ApplicationDbContext ni guarda datos reales.
    public class VistaPreviaSolicitudesService
    {
        public EscenarioVistaPreviaViewModel CrearEscenario()
        {
            var inicio = new DateTimeOffset(2026, 11, 10, 9, 0, 0, TimeSpan.FromHours(-5));
            var nombres = new[] { "Diego Huamán Torres", "Mateo Rivas Castro", "Sofía Gutiérrez Vega", "Joaquín Ramos Peña" };
            var equipos = new[] { "Osciloscopio digital", "Raspberry Pi · Kit", "MacBook Air M2", "Osciloscopio digital" };
            return new EscenarioVistaPreviaViewModel
            {
                Solicitudes = Enumerable.Range(0, nombres.Length).Select(i => new SolicitudSimuladaViewModel
                {
                    Solicitud = new SolicitudPendienteViewModel
                    {
                        Id = i + 1, Estudiante = nombres[i], CodigoEstudiante = $"DEMO-{i + 1:000}",
                        Equipo = equipos[i], CodigoInventario = $"LAB-{(i == 3 ? 1 : i + 1):000}",
                        Inicio = inicio.AddHours(i == 3 ? 1 : i), Fin = inicio.AddHours(i == 3 ? 3 : i + 2)
                    }
                }).ToList()
            };
        }

        public SolicitudesListadoViewModel Listar(EscenarioVistaPreviaViewModel escenario, string? busqueda)
        {
            busqueda = busqueda?.Trim() ?? string.Empty;
            var pendientes = escenario.Solicitudes.Where(x => x.Estado == EstadoReserva.Pendiente).Select(x => x.Solicitud);
            if (busqueda.Length > 0)
                pendientes = pendientes.Where(x =>
                    x.Estudiante.Contains(busqueda, StringComparison.OrdinalIgnoreCase)
                    || x.CodigoEstudiante.Contains(busqueda, StringComparison.OrdinalIgnoreCase)
                    || x.Equipo.Contains(busqueda, StringComparison.OrdinalIgnoreCase)
                    || x.CodigoInventario.Contains(busqueda, StringComparison.OrdinalIgnoreCase));
            var lista = pendientes.ToList();
            foreach (var solicitud in lista)
                solicitud.EquipoPrestado = escenario.Solicitudes.Any(x =>
                    x.Estado == EstadoReserva.Aprobada && SeSuperponen(x.Solicitud, solicitud));

            return new SolicitudesListadoViewModel
            {
                Solicitudes = lista, Pagina = 1, TamanoPagina = 20, Total = lista.Count,
                Busqueda = busqueda, EsVistaPrevia = true,
                ResolucionesSimuladas = escenario.Solicitudes.Where(x => x.Estado != EstadoReserva.Pendiente).ToList()
            };
        }

        public EstadoResolucion Aprobar(EscenarioVistaPreviaViewModel escenario, long id)
        {
            var solicitud = escenario.Solicitudes.SingleOrDefault(x => x.Solicitud.Id == id);
            if (solicitud is null) return EstadoResolucion.NoEncontrada;
            if (solicitud.Estado != EstadoReserva.Pendiente) return EstadoResolucion.YaResuelta;
            if (escenario.Solicitudes.Any(x => x.Estado == EstadoReserva.Aprobada
                && SeSuperponen(x.Solicitud, solicitud.Solicitud)))
                return EstadoResolucion.ConflictoHorario;
            solicitud.Estado = EstadoReserva.Aprobada;
            return EstadoResolucion.Completada;
        }

        public EstadoResolucion Rechazar(EscenarioVistaPreviaViewModel escenario, long id, string? motivo)
        {
            motivo = motivo?.Trim();
            if (string.IsNullOrWhiteSpace(motivo) || motivo.Length > 255) return EstadoResolucion.MotivoInvalido;
            var solicitud = escenario.Solicitudes.SingleOrDefault(x => x.Solicitud.Id == id);
            if (solicitud is null) return EstadoResolucion.NoEncontrada;
            if (solicitud.Estado != EstadoReserva.Pendiente) return EstadoResolucion.YaResuelta;
            solicitud.Estado = EstadoReserva.Rechazada;
            solicitud.Motivo = motivo;
            return EstadoResolucion.Completada;
        }

        private static bool SeSuperponen(SolicitudPendienteViewModel a, SolicitudPendienteViewModel b)
            => a.CodigoInventario == b.CodigoInventario && a.Inicio < b.Fin && b.Inicio < a.Fin;
    }
}
