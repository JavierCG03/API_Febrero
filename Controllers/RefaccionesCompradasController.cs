using CarSlineAPI.Data;
using CarSlineAPI.Models.DTOs;
using CarSlineAPI.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Asn1.Ocsp;

namespace CarSlineAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RefaccionesCompradasController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<RefaccionesCompradasController> _logger;

        public RefaccionesCompradasController(ApplicationDbContext db, ILogger<RefaccionesCompradasController> logger)
        {
            _db = db;
            _logger = logger;
        }

        // ============================================
        // AGREGAR REFACCIONES
        // ============================================

        [HttpPost("agregar")]
        public async Task<IActionResult> AgregarRefacciones([FromBody] AgregarRefaccionesRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(new AgregarRefaccionesCitaResponse
                {
                    Success = false,
                    Message = "Datos inválidos"
                });
            try
            {
                // Verificar que el trabajo existe según el origen
                if (!request.Orden)
                {
                    var trabajoCita = await _db.TrabajosPorCitas
                        .FirstOrDefaultAsync(t => t.Id == request.TrabajoId && t.Activo);

                    if (trabajoCita == null)
                        return NotFound(new AgregarRefaccionesCitaResponse
                        {
                            Success = false,
                            Message = "Trabajo de cita no encontrado"
                        });
                }
                else
                {
                    var trabajoOrden = await _db.TrabajosPorOrden
                        .FirstOrDefaultAsync(t => t.Id == request.TrabajoId && t.Activo);

                    if (trabajoOrden == null)
                        return NotFound(new AgregarRefaccionesCitaResponse
                        {
                            Success = false,
                            Message = "Trabajo de orden no encontrado"
                        });
                }

                var refaccionesAgregadas = new List<RefaccionCompradaDto>();

                foreach (var dto in request.Refacciones)
                {
                    var refaccion = new RefaccionComprada
                    {
                        // Colocar el Id en la columna correcta según el origen
                        TrabajoCitaId = !request.Orden ? request.TrabajoId : null,
                        TrabajoOrdenId = request.Orden ? request.TrabajoId : null,
                        Refaccion = dto.Refaccion,
                        Cantidad = dto.Cantidad,
                        Precio = dto.Precio,
                        PrecioVenta = dto.PrecioVenta,
                        FechaCompra = DateTime.Now,
                        Activo = false
                    };

                    _db.RefaccionesCompradas.Add(refaccion);

                    refaccionesAgregadas.Add(new RefaccionCompradaDto
                    {
                        TrabajoCitaId = refaccion.TrabajoCitaId,
                        TrabajoOrdenId = refaccion.TrabajoOrdenId,
                        Refaccion = refaccion.Refaccion,
                        Cantidad = refaccion.Cantidad,
                        Precio = refaccion.Precio,
                        PrecioVenta = refaccion.PrecioVenta,
                        FechaCompra = refaccion.FechaCompra,
                        Transferida = false
                    });
                }

                await _db.SaveChangesAsync();

                // Recuperar IDs generados filtrando por la columna correcta
                var guardadas = await _db.RefaccionesCompradas
                    .Where(r => request.Orden
                        ? r.TrabajoOrdenId == request.TrabajoId
                        : r.TrabajoCitaId == request.TrabajoId)
                    .OrderByDescending(r => r.Id)
                    .Take(request.Refacciones.Count)
                    .ToListAsync();

                for (int i = 0; i < refaccionesAgregadas.Count && i < guardadas.Count; i++)
                    refaccionesAgregadas[i].Id = guardadas[i].Id;

                var totalCosto = refaccionesAgregadas.Sum(r => r.TotalCosto);

                _logger.LogInformation(
                    $"Se agregaron {refaccionesAgregadas.Count} refacciones al trabajo {request.TrabajoId} (orden={request.Orden})");

                return Ok(new AgregarRefaccionesCitaResponse
                {
                    Success = true,
                    Message = $"Se agregaron {refaccionesAgregadas.Count} refacción(es) exitosamente",
                    RefaccionesAgregadas = refaccionesAgregadas,
                    CantidadRefacciones = refaccionesAgregadas.Count,
                    TotalCosto = totalCosto
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al agregar refacciones al trabajo {request.TrabajoId}");
                return StatusCode(500, new AgregarRefaccionesCitaResponse
                {
                    Success = false,
                    Message = "Error al agregar refacciones"
                });
            }
        }

        // ============================================
        // MARCAR REFACCIONES COMO LISTAS
        // ============================================

        [HttpPut("{trabajoId}/marcar-listas")]
        [ProducesResponseType(typeof(EliminarRefaccionCitaResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> MarcarRefaccionesListas(
            int trabajoId,
            [FromQuery] bool orden = false)
        {
            try
            {
                if (!orden)
                {
                    var trabajoCita = await _db.TrabajosPorCitas
                        .FirstOrDefaultAsync(t => t.Id == trabajoId && t.Activo);

                    if (trabajoCita == null)
                        return NotFound(new EliminarRefaccionCitaResponse
                        {
                            Success = false,
                            Message = "Trabajo de cita no encontrado o inactivo"
                        });

                    trabajoCita.RefaccionesListas = true;
                }
                else
                {
                    var trabajoOrden = await _db.TrabajosPorOrden
                        .FirstOrDefaultAsync(t => t.Id == trabajoId && t.Activo);

                    if (trabajoOrden == null)
                        return NotFound(new EliminarRefaccionCitaResponse
                        {
                            Success = false,
                            Message = "Trabajo de orden no encontrado o inactivo"
                        });

                    trabajoOrden.RefaccionesListas = true;
                }

                await _db.SaveChangesAsync();

                _logger.LogInformation(
                    $"Trabajo {trabajoId} (orden={orden}) marcado como refacciones listas");

                return Ok(new EliminarRefaccionCitaResponse
                {
                    Success = true,
                    Message = "Refacciones marcadas como listas exitosamente"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al marcar refacciones listas del trabajo {trabajoId}");
                return StatusCode(500, new EliminarRefaccionCitaResponse
                {
                    Success = false,
                    Message = "Error al actualizar estado de refacciones"
                });
            }
        }
        // ============================================
        // OBTENER REFACCIONES
        // ============================================

        /// GET api/RefaccionesCita/trabajo/{trabajoId}?orden=false  → filtra por TrabajoCitaId
        /// GET api/RefaccionesCita/trabajo/{trabajoId}?orden=true   → filtra por TrabajoOrdenId
        [HttpGet("trabajo/{trabajoId}")]
        public async Task<IActionResult> ObtenerRefaccionesPorTrabajo(int trabajoId, [FromQuery] bool orden = false)
        {
            try
            {
                string trabajoNombre;
                bool refaccionesListas;

                if (!orden)
                {
                    var trabajoCita = await _db.TrabajosPorCitas
                        .FirstOrDefaultAsync(t => t.Id == trabajoId && t.Activo);

                    if (trabajoCita == null)
                        return NotFound(new ObtenerRefaccionesCitaResponse
                        {
                            Success = false,
                            Message = "Trabajo de cita no encontrado"
                        });

                    trabajoNombre = trabajoCita.Trabajo;
                    refaccionesListas = trabajoCita.RefaccionesListas;
                }
                else
                {
                    var trabajoOrden = await _db.TrabajosPorOrden
                        .FirstOrDefaultAsync(t => t.Id == trabajoId && t.Activo);

                    if (trabajoOrden == null)
                        return NotFound(new ObtenerRefaccionesCitaResponse
                        {
                            Success = false,
                            Message = "Trabajo de orden no encontrado"
                        });

                    trabajoNombre = trabajoOrden.Trabajo;
                    refaccionesListas = trabajoOrden.RefaccionesListas;
                }

                var refacciones = await _db.RefaccionesCompradas
                    .Where(r => orden
                        ? r.TrabajoOrdenId == trabajoId
                        : r.TrabajoCitaId == trabajoId)
                    .OrderBy(r => r.Id)
                    .Select(r => new RefaccionCompradaDto
                    {
                        Id = r.Id,
                        TrabajoCitaId = r.TrabajoCitaId,
                        TrabajoOrdenId = r.TrabajoOrdenId,
                        Refaccion = r.Refaccion,
                        Cantidad = r.Cantidad,
                        Precio = r.Precio,
                        PrecioVenta = r.PrecioVenta,
                        FechaCompra = r.FechaCompra,
                        Transferida = r.Activo
                    })
                    .ToListAsync();

                var totalCosto = refacciones.Sum(r => r.TotalCosto);
                decimal? totalVenta = refacciones.All(r => r.PrecioVenta.HasValue)
                    ? refacciones.Sum(r => r.TotalVenta ?? 0)
                    : null;

                return Ok(new ObtenerRefaccionesCitaResponse
                {
                    Success = true,
                    Message = refacciones.Any()
                        ? $"Se encontraron {refacciones.Count} refacción(es)"
                        : "No hay refacciones registradas",
                    TrabajoId = trabajoId,
                    TrabajoNombre = trabajoNombre,
                    Refacciones = refacciones,
                    TotalCosto = totalCosto,
                    TotalVenta = totalVenta,
                    RefaccionesListas = refaccionesListas
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    $"Error al obtener refacciones del trabajo {trabajoId} (orden={orden})");

                return StatusCode(500, new ObtenerRefaccionesCitaResponse
                {
                    Success = false,
                    Message = "Error al obtener refacciones"
                });
            }
        }
        /// <summary>
        /// Obtener todas las refacciones de una cita completa (todos sus trabajos)
        /// GET api/RefaccionesCita/cita/{citaId}
        /// </summary>
        [HttpGet("cita/{citaId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ObtenerRefaccionesPorCita(int citaId)
        {
            try
            {
                var citaExiste = await _db.Citas.AnyAsync(c => c.Id == citaId && c.Activo);

                if (!citaExiste)
                {
                    return NotFound(new { Success = false, Message = "Cita no encontrada" });
                }

                // Traer trabajos de la cita con sus refacciones
                var trabajosConRefacciones = await _db.TrabajosPorCitas
                    .Where(t => t.CitaId == citaId && t.Activo)
                    .Select(t => new
                    {
                        t.Id,
                        t.Trabajo,
                        t.RefaccionesListas,
                        Refacciones = _db.RefaccionesCompradas
                            .Where(r => r.TrabajoCitaId == t.Id)
                            .OrderBy(r => r.Id)
                            .Select(r => new RefaccionCompradaDto
                            {
                                Id = r.Id,
                                TrabajoCitaId = r.TrabajoCitaId,
                                Refaccion = r.Refaccion,
                                Cantidad = r.Cantidad,
                                Precio = r.Precio,
                                PrecioVenta = r.PrecioVenta,
                                FechaCompra = r.FechaCompra,
                                Transferida = r.Activo,
                                TrabajoOrdenId = r.TrabajoOrdenId
                            }).ToList()
                    })
                    .ToListAsync();

                return Ok(new
                {
                    Success = true,
                    CitaId = citaId,
                    Trabajos = trabajosConRefacciones
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al obtener refacciones de la cita {citaId}");
                return StatusCode(500, new { Success = false, Message = "Error al obtener refacciones" });
            }
        }


        [HttpDelete("{refaccionId}")]
        [ProducesResponseType(typeof(EliminarRefaccionCitaResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> EliminarRefaccion(int refaccionId)
        {
            try
            {
                var data = await _db.RefaccionesCompradas
                    .Where(r => r.Id == refaccionId)
                    .Select(r => new
                    {
                        Refaccion = r,
                        r.TrabajoCitaId,
                        r.TrabajoOrdenId
                    })
                    .FirstOrDefaultAsync();

                if (data == null)
                    return NotFound(new EliminarRefaccionCitaResponse
                    {
                        Success = false,
                        Message = "Refacción no encontrada"
                    });


                // No se puede eliminar si ya fue transferida a una orden
                if (data.Refaccion.Activo)
                {
                    return BadRequest(new EliminarRefaccionCitaResponse
                    {
                        Success = false,
                        Message = "No se puede eliminar una refacción que ya fue transferida a una orden de trabajo"
                    });
                }

                _db.RefaccionesCompradas.Remove(data.Refaccion); 
                await _db.SaveChangesAsync();

                _logger.LogInformation($"Refacción de cita {refaccionId} eliminada exitosamente");

                return Ok(new EliminarRefaccionCitaResponse
                {
                    Success = true,
                    Message = "Refacción eliminada exitosamente"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al eliminar refacción de cita {refaccionId}");
                return StatusCode(500, new EliminarRefaccionCitaResponse
                {
                    Success = false,
                    Message = "Error al eliminar refacción"
                });
            }
        }

        // ============================================
        // ACTUALIZAR PRECIO DE VENTA
        // ============================================

        /// <summary>
        /// Actualizar el precio de venta de una refacción de cita
        /// PUT api/RefaccionesCita/{refaccionId}/precio-venta
        /// </summary>
        [HttpPut("{refaccionId}/precio-venta")]
        [ProducesResponseType(typeof(EliminarRefaccionCitaResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ActualizarPrecioVenta(
            int refaccionId,
            [FromBody] ActualizarPrecioVentaRefaccionCitaRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new EliminarRefaccionCitaResponse
                {
                    Success = false,
                    Message = "Datos inválidos"
                });
            }

            try
            {
                var refaccion = await _db.RefaccionesCompradas.FindAsync(refaccionId);

                if (refaccion == null)
                {
                    return NotFound(new EliminarRefaccionCitaResponse
                    {
                        Success = false,
                        Message = "Refacción no encontrada"
                    });
                }

                if (refaccion.Activo)
                {
                    return BadRequest(new EliminarRefaccionCitaResponse
                    {
                        Success = false,
                        Message = "No se puede modificar una refacción que ya fue transferida a una orden"
                    });
                }

                refaccion.PrecioVenta = request.PrecioVenta;
                await _db.SaveChangesAsync();

                return Ok(new EliminarRefaccionCitaResponse
                {
                    Success = true,
                    Message = "Precio de venta actualizado exitosamente"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al actualizar precio de venta de refacción {refaccionId}");
                return StatusCode(500, new EliminarRefaccionCitaResponse
                {
                    Success = false,
                    Message = "Error al actualizar precio de venta"
                });
            }
        }
    }
}