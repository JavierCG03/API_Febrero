using CarSlineAPI.Data;
using CarSlineAPI.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarSlineAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HistorialController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<BusquedaController> _logger;

        public HistorialController(ApplicationDbContext db, ILogger<BusquedaController> logger)
        {
            _db = db;
            _logger = logger;
        }

        [HttpGet("Historial-General/{vehiculoId}")]
        [ProducesResponseType(typeof(HistorialGeneralResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> ObtenerHistorial(int vehiculoId)
        {
            try
            {
                var fechaLimite = DateTime.Now.AddMonths(-12);
                var ordenes = await _db.OrdenesGenerales
                    .Include(o => o.TipoOrden)
                    .Include(o => o.EstadoOrden) 
                    .Where(o => o.VehiculoId == vehiculoId
                             && o.Activo
                             && o.FechaCreacion >= fechaLimite)
                    .OrderByDescending(o => o.FechaCreacion)
                    .ToListAsync();

                var historial = ordenes.Select(o => new HistorialOrdenDto
                {
                    OrdenId = o.Id,
                    NumeroOrden = o.NumeroOrden,
                    FechaOrden = o.FechaCreacion,
                    TipoOrden = o.TipoOrden.NombreTipo ?? "",
                    EstadoOrden =o.EstadoOrden.NombreEstado ?? "",
                    KilometrajeRegistrado = o.KilometrajeActual,
                    ObservacionesAsesor = o.ObservacionesAsesor ?? ""
                }).ToList();

                return Ok(new HistorialGeneralResponse
                {
                    Success = true,
                    Message = historial.Any() ? "Historial encontrado" : "Sin Ordenes recientes",
                    Historial = historial,
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al obtener historial del vehículo {vehiculoId}");

                return StatusCode(500, new HistorialGeneralResponse
                {
                    Success = false,
                    Message = "Error al obtener historial de servicios",
                    Historial = new List<HistorialOrdenDto>()
                });
            }
        }

        [HttpGet("Historial-Servicio/{vehiculoId}")]
        [ProducesResponseType(typeof(HistorialVehiculoResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> ObtenerHistorialServicio(int vehiculoId)
        {
            try
            {
                var fechaLimite = DateTime.Now.AddMonths(-12);

                var ordenes = await _db.OrdenesGenerales
                    .Include(o => o.TipoServicio) // Necesario para o.TipoServicio.NombreServicio
                    .Where(o => o.VehiculoId == vehiculoId
                             && o.Activo
                             && o.TipoOrdenId == 1   // Solo órdenes de servicio
                             && o.EstadoOrdenId == 4 // Solo órdenes ENTREGADAS
                             && o.FechaCreacion >= fechaLimite)
                    .OrderByDescending(o => o.FechaCreacion)
                    .ToListAsync();

                var historial = ordenes.Select(o => new HistorialServicioDto
                {
                    NumeroOrden = o.NumeroOrden,
                    FechaServicio = o.FechaCreacion,
                    TipoServicio = o.TipoServicio.NombreServicio ?? "",
                    KilometrajeRegistrado = o.KilometrajeActual,
                    ObservacionesAsesor = o.ObservacionesAsesor ?? ""
                }).ToList();

                var ultimoServicio = historial.FirstOrDefault();

                return Ok(new HistorialVehiculoResponse
                {
                    Success = true,
                    Message = historial.Any() ? "Historial encontrado" : "Sin servicios recientes",
                    Historial = historial,
                    UltimoServicio = ultimoServicio?.TipoServicio ?? "",
                    UltimoKilometraje = ultimoServicio?.KilometrajeRegistrado ?? 0,
                    UltimaFechaServicio = ultimoServicio?.FechaServicio
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al obtener historial del vehículo {vehiculoId}");

                return StatusCode(500, new HistorialVehiculoResponse
                {
                    Success = false,
                    Message = "Error al obtener historial de servicios",
                    Historial = new List<HistorialServicioDto>()
                });
            }
        }
    }
}