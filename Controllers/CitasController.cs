
using CarSlineAPI.Data;
using CarSlineAPI.Models.DTOs;
using CarSlineAPI.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;

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
        [FromHeader(Name = "X-User-Id")] int EncargadoCitasId)
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
                    var Cita = new Cita
                    {
                        TipoOrdenId = request.TipoOrdenId,
                        ClienteId = request.ClienteId,
                        VehiculoId = request.VehiculoId,
                        TipoServicioId = request.TipoServicioId,
                        EncargadoCitas= EncargadoCitasId,
                        FechaCita= request.FechaCita,
                        FechaCreacion = DateTime.Now,
                        Activo = true
                    };

                    _db.OrdenesGenerales.Add(Cita);
                    await _db.SaveChangesAsync();

                    // 3. Crear trabajos asociados
                    foreach (var t in request.Trabajos)
                    {
                        var trabajo = new TrabajoPorCita
                        {
                            CitaId = Cita.Id,
                            Trabajo = t.Trabajo,
                            IndicacionesTrabajo = string.IsNullOrWhiteSpace(t.Indicaciones) ? null : t.Indicaciones,
                            Activo = true,
                        };

                        _db.TrabajosPorOrden.Add(trabajo);
                    }

                    await _db.SaveChangesAsync();
                    await transaction.CommitAsync();


                    return Ok(new
                    {
                        Success = true,
                        NumeroOrden = numeroOrden,
                        OrdenId = ordenGeneral.Id,
                        TotalTrabajos = request.Trabajos.Count,
                        Message = "Orden creada exitosamente"
                    });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Error al crear orden con trabajos");
                    return StatusCode(500, new { Success = false, Message = "Error al crear orden" });
                }
            });
        }
    }
}
