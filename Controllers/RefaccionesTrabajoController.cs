using CarSlineAPI.Data;
using CarSlineAPI.Models.DTOs;
using CarSlineAPI.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarSlineAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RefaccionesTrabajoController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<RefaccionesTrabajoController> _logger;

        public RefaccionesTrabajoController(ApplicationDbContext db, ILogger<RefaccionesTrabajoController> logger)
        {
            _db = db;
            _logger = logger;
        }

        [HttpPost("agregar")]
        [ProducesResponseType(typeof(AgregarRefaccionesResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> AgregarRefacciones([FromBody] AgregarRefaccionesTrabajoRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new AgregarRefaccionesResponse
                {
                    Success = false,
                    Message = "Datos inválidos"
                });
            }

            try
            {
                // Verificar que el trabajo existe
                var trabajo = await _db.TrabajosPorOrden
                    .Include(t => t.OrdenGeneral)
                    .FirstOrDefaultAsync(t => t.Id == request.TrabajoId && t.Activo);

                if (trabajo == null)
                {
                    return NotFound(new AgregarRefaccionesResponse
                    {
                        Success = false,
                        Message = "Trabajo no encontrado"
                    });
                }

                // Validar que el trabajo no esté completado o cancelado
                if (trabajo.EstadoTrabajo == 6)
                {
                    return BadRequest(new AgregarRefaccionesResponse
                    {
                        Success = false,
                        Message = "No se pueden agregar refacciones a un trabajo cancelado"
                    });
                }

                var refaccionesAgregadas = new List<RefaccionTrabajoDto>();
                decimal totalRefacciones = 0;

                // Procesar cada refacción
                foreach (var refaccionDto in request.Refacciones)
                {
                    var total = refaccionDto.Cantidad * refaccionDto.PrecioUnitario;
                    totalRefacciones += total;

                    var refaccionTrabajo = new Refacciontrabajo
                    {
                        TrabajoId = request.TrabajoId,
                        OrdenGeneralId = trabajo.OrdenGeneralId,
                        Refaccion = refaccionDto.Refaccion,
                        Cantidad = refaccionDto.Cantidad,
                        PrecioUnitario = refaccionDto.PrecioUnitario
                    };

                    _db.Set<Refacciontrabajo>().Add(refaccionTrabajo);

                    refaccionesAgregadas.Add(new RefaccionTrabajoDto
                    {
                        Id = 0, // Se asignará después del SaveChanges
                        TrabajoId = refaccionTrabajo.TrabajoId,
                        OrdenGeneralId = refaccionTrabajo.OrdenGeneralId,
                        Refaccion = refaccionTrabajo.Refaccion,
                        Cantidad = refaccionTrabajo.Cantidad,
                        PrecioUnitario = refaccionTrabajo.PrecioUnitario,
                    });
                }


                await _db.SaveChangesAsync();


                await _db.Entry(trabajo).ReloadAsync();

                // Actualizar los IDs después de guardar
                var refaccionesGuardadas = await _db.Set<Refacciontrabajo>()
                    .Where(r => r.TrabajoId == request.TrabajoId)
                    .OrderByDescending(r => r.Id)
                    .Take(request.Refacciones.Count)
                    .ToListAsync();

                for (int i = 0; i < refaccionesAgregadas.Count && i < refaccionesGuardadas.Count; i++)
                {
                    refaccionesAgregadas[i].Id = refaccionesGuardadas[i].Id;
                }

                _logger.LogInformation(
                    $"Se agregaron {refaccionesAgregadas.Count} refacciones al trabajo {request.TrabajoId}. " +
                    $"Total calculado por trigger: ${trabajo.RefaccionesTotal:F2}");

                return Ok(new AgregarRefaccionesResponse
                {
                    Success = true,
                    Message = $"Se agregaron {refaccionesAgregadas.Count} refacción(es) exitosamente",
                    RefaccionesAgregadas = refaccionesAgregadas,
                    TotalRefacciones = trabajo.RefaccionesTotal, // ✅ Usar el valor actualizado por el trigger
                    CantidadRefacciones = refaccionesAgregadas.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al agregar refacciones al trabajo {request.TrabajoId}");
                return StatusCode(500, new AgregarRefaccionesResponse
                {
                    Success = false,
                    Message = "Error al agregar refacciones"
                });
            }
        }

        /// <summary>
        /// Obtener todas las refacciones de un trabajo
        /// GET api/RefaccionesTrabajo/trabajo/{trabajoId}
        /// </summary>
        [HttpGet("trabajo/{trabajoId}")]
        [ProducesResponseType(typeof(ObtenerRefaccionesTrabajoResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ObtenerRefaccionesPorTrabajo(int trabajoId)
        {
            try
            {
                var trabajo = await _db.TrabajosPorOrden
                    .Include(t => t.OrdenGeneral)
                    .FirstOrDefaultAsync(t => t.Id == trabajoId);

                if (trabajo == null)
                {
                    return NotFound(new ObtenerRefaccionesTrabajoResponse
                    {
                        Success = false,
                        Message = "Trabajo no encontrado"
                    });
                }

                var refacciones = await _db.Set<Refacciontrabajo>()
                    .Where(r => r.TrabajoId == trabajoId)
                    .Select(r => new RefaccionTrabajoDto
                    {
                        Id = r.Id,
                        TrabajoId = r.TrabajoId,
                        OrdenGeneralId = r.OrdenGeneralId,
                        Refaccion = r.Refaccion,
                        Cantidad = r.Cantidad,
                        PrecioUnitario = r.PrecioUnitario,
                        Total = r.Cantidad * r.PrecioUnitario
                    })
                    .ToListAsync();

                var total = refacciones.Sum(r => r.Total);

                return Ok(new ObtenerRefaccionesTrabajoResponse
                {
                    Success = true,
                    Message = refacciones.Any()
                        ? $"Se encontraron {refacciones.Count} refacción(es)"
                        : "No hay refacciones registradas",
                    TrabajoId = trabajoId,
                    NumeroOrden = trabajo.OrdenGeneral?.NumeroOrden ?? "",
                    Refacciones = refacciones,
                    TotalRefacciones = total
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al obtener refacciones del trabajo {trabajoId}");
                return StatusCode(500, new ObtenerRefaccionesTrabajoResponse
                {
                    Success = false,
                    Message = "Error al obtener refacciones"
                });
            }
        }

        /// <summary>
        /// Eliminar una refacción específica de un trabajo
        /// DELETE api/RefaccionesTrabajo/{refaccionId}
        /// </summary>
        [HttpDelete("{refaccionId}")]
        [ProducesResponseType(typeof(AgregarRefaccionesResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> EliminarRefaccion(int refaccionId)
        {
            try
            {
                var refaccion = await _db.Set<Refacciontrabajo>()
                    .Include(r => r.TrabajoPorOrden)
                    .FirstOrDefaultAsync(r => r.Id == refaccionId);

                if (refaccion == null)
                {
                    return NotFound(new AgregarRefaccionesResponse
                    {
                        Success = false,
                        Message = "Refacción no encontrada"
                    });
                }

                // Verificar que el trabajo no esté completado
                if (refaccion.TrabajoPorOrden?.EstadoTrabajo == 4)
                {
                    return BadRequest(new AgregarRefaccionesResponse
                    {
                        Success = false,
                        Message = "No se pueden eliminar refacciones de un trabajo completado"
                    });
                }

                var trabajoId = refaccion.TrabajoId;
                var ordenId = refaccion.OrdenGeneralId;

                _db.Set<Refacciontrabajo>().Remove(refaccion);
                await _db.SaveChangesAsync();

                _logger.LogInformation($"Refacción {refaccionId} eliminada del trabajo {trabajoId}. Los totales se actualizaron automáticamente vía trigger.");

                return Ok(new AgregarRefaccionesResponse
                {
                    Success = true,
                    Message = "Refacción eliminada exitosamente"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al eliminar refacción {refaccionId}");
                return StatusCode(500, new AgregarRefaccionesResponse
                {
                    Success = false,
                    Message = "Error al eliminar refacción"
                });
            }
        }

        /// <summary>
        /// Obtener todas las refacciones de una orden
        /// GET api/RefaccionesTrabajo/orden/{ordenId}
        /// </summary>
        [HttpGet("orden/{ordenId}")]
        [ProducesResponseType(typeof(ObtenerRefaccionesTrabajoResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> ObtenerRefaccionesPorOrden(int ordenId)
        {
            try
            {
                var orden = await _db.OrdenesGenerales.FindAsync(ordenId);

                if (orden == null)
                {
                    return NotFound(new ObtenerRefaccionesTrabajoResponse
                    {
                        Success = false,
                        Message = "Orden no encontrada"
                    });
                }

                var refacciones = await _db.Set<Refacciontrabajo>()
                    .Where(r => r.OrdenGeneralId == ordenId)
                    .Select(r => new RefaccionTrabajoDto
                    {
                        Id = r.Id,
                        TrabajoId = r.TrabajoId,
                        OrdenGeneralId = r.OrdenGeneralId,
                        Refaccion = r.Refaccion,
                        Cantidad = r.Cantidad,
                        PrecioUnitario = r.PrecioUnitario,
                        Total = r.Cantidad * r.PrecioUnitario
                    })
                    .ToListAsync();

                var total = refacciones.Sum(r => r.Total);

                return Ok(new ObtenerRefaccionesTrabajoResponse
                {
                    Success = true,
                    Message = refacciones.Any()
                        ? $"Se encontraron {refacciones.Count} refacción(es)"
                        : "No hay refacciones registradas",
                    TrabajoId = 0,
                    NumeroOrden = orden.NumeroOrden,
                    Refacciones = refacciones,
                    TotalRefacciones = total
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al obtener refacciones de la orden {ordenId}");
                return StatusCode(500, new ObtenerRefaccionesTrabajoResponse
                {
                    Success = false,
                    Message = "Error al obtener refacciones"
                });
            }
        }
    }
}