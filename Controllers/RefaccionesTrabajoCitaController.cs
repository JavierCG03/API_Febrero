using CarSlineAPI.Data;
using CarSlineAPI.Models.DTOs;
using CarSlineAPI.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarSlineAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RefaccionesCitaController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<RefaccionesCitaController> _logger;

        public RefaccionesCitaController(ApplicationDbContext db, ILogger<RefaccionesCitaController> logger)
        {
            _db = db;
            _logger = logger;
        }

        // ============================================
        // AGREGAR REFACCIONES
        // ============================================

        /// <summary>
        /// Agregar refacciones a un trabajo de cita
        /// POST api/RefaccionesCita/agregar
        /// </summary>
        [HttpPost("agregar")]
        [ProducesResponseType(typeof(AgregarRefaccionesCitaResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> AgregarRefacciones([FromBody] AgregarRefaccionesCitaRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new AgregarRefaccionesCitaResponse
                {
                    Success = false,
                    Message = "Datos inválidos"
                });
            }

            try
            {
                // Verificar que el trabajo de cita existe y está activo
                var trabajoCita = await _db.TrabajosPorCitas
                    .Include(t => t.Cita)
                    .FirstOrDefaultAsync(t => t.Id == request.TrabajoCitaId && t.Activo);

                if (trabajoCita == null)
                {
                    return NotFound(new AgregarRefaccionesCitaResponse
                    {
                        Success = false,
                        Message = "Trabajo de cita no encontrado"
                    });
                }

                // Verificar que la cita siga activa
                if (trabajoCita.Cita == null || !trabajoCita.Cita.Activo)
                {
                    return BadRequest(new AgregarRefaccionesCitaResponse
                    {
                        Success = false,
                        Message = "No se pueden agregar refacciones a una cita cancelada"
                    });
                }

                var refaccionesAgregadas = new List<RefaccionPorCitaDto>();

                foreach (var dto in request.Refacciones)
                {
                    var refaccion = new RefaccionPorCita
                    {
                        TrabajoCitaId = request.TrabajoCitaId,
                        Refaccion = dto.Refaccion,
                        Cantidad = dto.Cantidad,
                        Precio = dto.Precio,
                        PrecioVenta = dto.PrecioVenta,
                        FechaCompra = DateTime.Now,
                        TrabajoOrdenId = null,
                        Activo = false  // false = pendiente de transferir a orden
                    };

                    _db.Refaccionesporcita.Add(refaccion);

                    refaccionesAgregadas.Add(new RefaccionPorCitaDto
                    {
                        TrabajoCitaId = refaccion.TrabajoCitaId,
                        Refaccion = refaccion.Refaccion,
                        Cantidad = refaccion.Cantidad,
                        Precio = refaccion.Precio,
                        PrecioVenta = refaccion.PrecioVenta,
                        FechaCompra = refaccion.FechaCompra,
                        Transferida = false
                    });
                }

                // Marcar el trabajo de cita con refacciones listas
                //trabajoCita.RefaccionesListas = true;

                await _db.SaveChangesAsync();

                // Recuperar IDs generados
                var guardadas = await _db.Refaccionesporcita
                    .Where(r => r.TrabajoCitaId == request.TrabajoCitaId)
                    .OrderByDescending(r => r.Id)
                    .Take(request.Refacciones.Count)
                    .ToListAsync();

                for (int i = 0; i < refaccionesAgregadas.Count && i < guardadas.Count; i++)
                    refaccionesAgregadas[i].Id = guardadas[i].Id;

                var totalCosto = refaccionesAgregadas.Sum(r => r.TotalCosto);

                _logger.LogInformation(
                    $"Se agregaron {refaccionesAgregadas.Count} refacciones al trabajo de cita {request.TrabajoCitaId}");

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
                _logger.LogError(ex, $"Error al agregar refacciones al trabajo de cita {request.TrabajoCitaId}");
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

        /// <summary>
        /// Marcar un trabajo de cita como con refacciones listas
        /// PUT api/RefaccionesCita/{trabajoCitaId}/marcar-listas
        /// </summary>
        [HttpPut("{trabajoCitaId}/marcar-listas")]
        [ProducesResponseType(typeof(EliminarRefaccionCitaResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> MarcarRefaccionesListas(int trabajoCitaId)
        {
            try
            {
                var trabajoCita = await _db.TrabajosPorCitas
                    .Include(t => t.Cita)
                    .FirstOrDefaultAsync(t => t.Id == trabajoCitaId && t.Activo);

                if (trabajoCita == null)
                {
                    return NotFound(new EliminarRefaccionCitaResponse
                    {
                        Success = false,
                        Message = "Trabajo de cita no encontrado"
                    });
                }

                if (trabajoCita.Cita == null || !trabajoCita.Cita.Activo)
                {
                    return BadRequest(new EliminarRefaccionCitaResponse
                    {
                        Success = false,
                        Message = "No se puede modificar una cita cancelada"
                    });
                }

                trabajoCita.RefaccionesListas = true;

                await _db.SaveChangesAsync();

                _logger.LogInformation(
                    $"Trabajo de cita {trabajoCitaId} marcado como refacciones listas");

                return Ok(new EliminarRefaccionCitaResponse
                {
                    Success = true,
                    Message = "Refacciones marcadas como listas exitosamente"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    $"Error al marcar refacciones listas del trabajo {trabajoCitaId}");

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

        /// <summary>
        /// Obtener todas las refacciones de un trabajo de cita
        /// GET api/RefaccionesCita/trabajo/{trabajoCitaId}
        /// </summary>
        [HttpGet("trabajo/{trabajoCitaId}")]
        [ProducesResponseType(typeof(ObtenerRefaccionesCitaResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ObtenerRefaccionesPorTrabajoCita(int trabajoCitaId)
        {
            try
            {
                var trabajoCita = await _db.TrabajosPorCitas
                    .FirstOrDefaultAsync(t => t.Id == trabajoCitaId);

                if (trabajoCita == null)
                {
                    return NotFound(new ObtenerRefaccionesCitaResponse
                    {
                        Success = false,
                        Message = "Trabajo de cita no encontrado"
                    });
                }

                var refacciones = await _db.Refaccionesporcita
                    .Where(r => r.TrabajoCitaId == trabajoCitaId)
                    .OrderBy(r => r.Id)
                    .Select(r => new RefaccionPorCitaDto
                    {
                        Id = r.Id,
                        TrabajoCitaId = r.TrabajoCitaId,
                        Refaccion = r.Refaccion,
                        Cantidad = r.Cantidad,
                        Precio = r.Precio,
                        PrecioVenta = r.PrecioVenta,
                        FechaCompra = r.FechaCompra,
                        Transferida = r.Activo,        // Activo=true significa ya transferida a orden
                        TrabajoOrdenId = r.TrabajoOrdenId
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
                    TrabajoCitaId = trabajoCitaId,
                    TrabajoCitaNombre = trabajoCita.Trabajo,
                    Refacciones = refacciones,
                    TotalCosto = totalCosto,
                    TotalVenta = totalVenta,
                    RefaccionesListas = trabajoCita.RefaccionesListas
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al obtener refacciones del trabajo de cita {trabajoCitaId}");
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
                        Refacciones = _db.Refaccionesporcita
                            .Where(r => r.TrabajoCitaId == t.Id)
                            .OrderBy(r => r.Id)
                            .Select(r => new RefaccionPorCitaDto
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

        // ============================================
        // ELIMINAR REFACCIONES
        // ============================================

        /// <summary>
        /// Eliminar una refacción de un trabajo de cita
        /// DELETE api/RefaccionesCita/{refaccionId}
        ///
        /// IMPORTANTE: Solo se pueden eliminar refacciones que AÚN NO han sido
        /// transferidas a una orden (Activo = false).
        /// </summary>
        [HttpDelete("{refaccionId}")]
        [ProducesResponseType(typeof(EliminarRefaccionCitaResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> EliminarRefaccion(int refaccionId)
        {
            try
            {
                // Traer la refacción junto con el estado de la cita
                var data = await _db.Refaccionesporcita
                    .Where(r => r.Id == refaccionId)
                    .Select(r => new
                    {
                        Refaccion = r,
                        CitaActiva = r.TrabajoPorCita != null && r.TrabajoPorCita.Cita != null
                            ? r.TrabajoPorCita.Cita.Activo
                            : false,
                        TrabajoCitaId = r.TrabajoCitaId
                    })
                    .FirstOrDefaultAsync();

                if (data == null)
                {
                    return NotFound(new EliminarRefaccionCitaResponse
                    {
                        Success = false,
                        Message = "Refacción no encontrada"
                    });
                }

                // No se puede eliminar si ya fue transferida a una orden
                if (data.Refaccion.Activo)
                {
                    return BadRequest(new EliminarRefaccionCitaResponse
                    {
                        Success = false,
                        Message = "No se puede eliminar una refacción que ya fue transferida a una orden de trabajo"
                    });
                }

                _db.Refaccionesporcita.Remove(data.Refaccion);

                // Verificar si el trabajo de cita todavía tiene más refacciones
                // Si se eliminan todas, desmarcar RefaccionesListas
                var refaccionesRestantes = await _db.Refaccionesporcita
                    .CountAsync(r => r.TrabajoCitaId == data.TrabajoCitaId && r.Id != refaccionId);

                if (refaccionesRestantes == 0)
                {
                    var trabajoCita = await _db.TrabajosPorCitas.FindAsync(data.TrabajoCitaId);
                    if (trabajoCita != null)
                        trabajoCita.RefaccionesListas = false;
                }

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
                var refaccion = await _db.Refaccionesporcita.FindAsync(refaccionId);

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