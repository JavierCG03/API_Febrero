// Controllers/CitasController.cs
using CarSlineAPI.Data;
using CarSlineAPI.Models.DTOs;
using CarSlineAPI.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarSlineAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CitasController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<CitasController> _logger;

        public CitasController(ApplicationDbContext db, ILogger<CitasController> logger)
        {
            _db = db;
            _logger = logger;
        }

        [HttpPost("crear-con-trabajos")]
        public async Task<IActionResult> CrearCitaConTrabajos(
            [FromBody] CrearCitaConTrabajosRequest request,
            [FromHeader(Name = "X-User-Id")] int encargadoCitasId)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { Success = false, Message = "Datos inválidos" });

            var strategy = _db.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _db.Database.BeginTransactionAsync();

                try
                {
                    // 1. Crear Cita
                    var cita = new Cita
                    {
                        TipoOrdenId = request.TipoOrdenId,
                        ClienteId = request.ClienteId,
                        VehiculoId = request.VehiculoId,
                        TipoServicioId = request.TipoServicioId,
                        EncargadoCitasId = encargadoCitasId,
                        FechaCita = request.FechaCita,
                        FechaCreacion = DateTime.Now,
                        Activo = true
                    };

                    _db.citas.Add(cita);
                    await _db.SaveChangesAsync();

                    // 2. Crear trabajos asociados a la cita
                    foreach (var t in request.Trabajos)
                    {
                        var trabajo = new TrabajoPorCita
                        {
                            CitaId = cita.Id,
                            Trabajo = t.Trabajo,
                            IndicacionesTrabajo = string.IsNullOrWhiteSpace(t.Indicaciones) ? null : t.Indicaciones,
                            Activo = true
                        };

                        _db.trabajosporcitas.Add(trabajo);
                    }

                    await _db.SaveChangesAsync();
                    await transaction.CommitAsync();

                    _logger.LogInformation($"Cita ID {cita.Id} creada con {request.Trabajos.Count} trabajos para fecha {cita.FechaCita:dd/MM/yyyy}");

                    return Ok(new
                    {
                        Success = true,
                        CitaId = cita.Id,
                        FechaCita = cita.FechaCita,
                        TotalTrabajos = request.Trabajos.Count,
                        Message = "Cita creada exitosamente"
                    });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Error al crear cita con trabajos");
                    return StatusCode(500, new { Success = false, Message = "Error al crear cita" });
                }
            });
        }

        /// <summary>
        /// Obtener citas por fecha
        /// GET api/Citas/por-fecha?fecha=2024-12-30
        /// </summary>
        [HttpGet("por-fecha")]
        public async Task<IActionResult> ObtenerCitasPorFecha([FromQuery] DateTime? fecha = null)
        {
            try
            {
                var fechaConsulta = (fecha ?? DateTime.Today).Date;
                var fechaSiguiente = fechaConsulta.AddDays(1);

                var citas = await _db.citas
                    .Include(c => c.Cliente)
                    .Include(c => c.Vehiculo)
                    .Include(c => c.TipoOrden)
                    .Where(c => c.Activo
                             && c.FechaCita >= fechaConsulta
                             && c.FechaCita < fechaSiguiente)
                    .OrderBy(c => c.FechaCita)
                    .Select(c => new
                    {
                        c.Id,
                        c.FechaCita,
                        ClienteNombre = c.Cliente.NombreCompleto,
                        ClienteTelefono = c.Cliente.TelefonoMovil,
                        VehiculoInfo = $"{c.Vehiculo.Marca} {c.Vehiculo.Modelo} {c.Vehiculo.Anio}",
                        TipoOrden = c.TipoOrden.NombreTipo,
                        TipoServicio = c.TipoServicio != null ? c.TipoServicio.NombreServicio : "Sin especificar",
                        c.FechaCreacion
                    })
                    .ToListAsync();

                return Ok(new
                {
                    Success = true,
                    Fecha = fechaConsulta,
                    TotalCitas = citas.Count,
                    Citas = citas
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener citas");
                return StatusCode(500, new { Success = false, Message = "Error al obtener citas" });
            }
        }

        /// <summary>
        /// Obtener detalle de una cita con sus trabajos
        /// GET api/Citas/{citaId}
        /// </summary>
        [HttpGet("{citaId}")]
        public async Task<IActionResult> ObtenerDetalleCita(int citaId)
        {
            try
            {
                var cita = await _db.citas
                    .Include(c => c.Cliente)
                    .Include(c => c.Vehiculo)
                    .Include(c => c.TipoServicio)
                    .Include(c => c.EncargadoCitas)
                    .Where(c => c.Id == citaId && c.Activo)
                    .FirstOrDefaultAsync();

                if (cita == null)
                    return NotFound(new { Success = false, Message = "Cita no encontrada" });

                var trabajos = await _db.trabajosporcitas
                    .Where(t => t.CitaId == citaId && t.Activo)
                    .Select(t => new
                    {
                        t.Id,
                        t.Trabajo,
                        t.IndicacionesTrabajo
                    })
                    .ToListAsync();

                return Ok(new
                {
                    Success = true,
                    Cita = new
                    {
                        cita.Id,
                        cita.FechaCita,
                        ClienteNombre = cita.Cliente.NombreCompleto,
                        ClienteTelefono = cita.Cliente.TelefonoMovil,
                        VehiculoInfo = $"{cita.Vehiculo.Marca} {cita.Vehiculo.Modelo} {cita.Vehiculo.Anio}",
                        VIN = cita.Vehiculo.VIN,
                        TipoServicio = cita.TipoServicio?.NombreServicio ?? "Sin especificar",
                        EncargadoNombre = cita.EncargadoCitas.NombreCompleto,
                        cita.FechaCreacion,
                        Trabajos = trabajos
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al obtener detalle de cita {citaId}");
                return StatusCode(500, new { Success = false, Message = "Error al obtener detalle de cita" });
            }
        }

        /// <summary>
        /// Cancelar una cita
        /// PUT api/Citas/cancelar/{citaId}
        /// </summary>
        [HttpPut("cancelar/{citaId}")]
        public async Task<IActionResult> CancelarCita(int citaId)
        {
            try
            {
                var cita = await _db.citas.FindAsync(citaId);

                if (cita == null || !cita.Activo)
                    return NotFound(new { Success = false, Message = "Cita no encontrada" });

                cita.Activo = false;
                await _db.SaveChangesAsync();

                _logger.LogInformation($"Cita {citaId} cancelada");

                return Ok(new { Success = true, Message = "Cita cancelada exitosamente" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al cancelar cita {citaId}");
                return StatusCode(500, new { Success = false, Message = "Error al cancelar cita" });
            }
        }
    }
}