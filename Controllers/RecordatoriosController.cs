using CarSlineAPI.Data;
using CarSlineAPI.Models.DTOs;
using CarSlineAPI.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarSlineAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RecordatoriosController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<RecordatoriosController> _logger;

        public RecordatoriosController(ApplicationDbContext db, ILogger<RecordatoriosController> logger)
        {
            _db = db;
            _logger = logger;
        }


        /// <summary>
        /// Obtener recordatorios por tipo (1 = Primero, 2 = Segundo, 3 = Tercero)
        /// GET api/Recordatorios/{tipoRecordatorio}
        /// </summary>
        [HttpGet("{tipoRecordatorio}")]
        [ProducesResponseType(typeof(ObtenerRecordatoriosResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ObtenerRecordatorios(int tipoRecordatorio)
        {
            if (tipoRecordatorio < 1 || tipoRecordatorio > 3)
            {
                return BadRequest(new ObtenerRecordatoriosResponse
                {
                    Success = false,
                    Message = "Tipo de recordatorio inválido. Debe ser 1, 2 o 3."
                });
            }

            try
            {
                var fechaHoy = DateTime.Today.AddMonths(3).AddDays(26);
                List<RecordatorioServicioSimpleDto> recordatorios;

                switch (tipoRecordatorio)
                {
                    case 1: // PRIMER RECORDATORIO
                        var primeros = await _db.ProximosServicios
                            .Where(ps => ps.Activo
                                      && !ps.PrimerRecordatorio
                                      && ps.FechaPrimerRecordatorio <= fechaHoy)
                            .Select(ps => new RecordatorioServicioSimpleDto
                            {
                                Id = ps.Id,
                                ClienteNombre = ps.Cliente.NombreCompleto,
                                ProximoServicioNombre = ps.TipoProximoServicio,
                                FechaProximoServicio = ps.FechaProximoServicio
                            })
                            .OrderBy(ps => ps.FechaProximoServicio)
                            .ToListAsync();

                        recordatorios = primeros;
                        break;

                    case 2: // SEGUNDO RECORDATORIO
                        var segundos = await _db.ProximosServicios
                            .Where(ps => ps.Activo
                                      && ps.PrimerRecordatorio
                                      && !ps.SegundoRecordatorio
                                      && ps.FechaSegundoRecordatorio <= fechaHoy)
                            .Select(ps => new RecordatorioServicioSimpleDto
                            {
                                Id = ps.Id,
                                ClienteNombre = ps.Cliente.NombreCompleto,
                                ProximoServicioNombre = ps.TipoProximoServicio,
                                FechaProximoServicio = ps.FechaProximoServicio
                            })
                            .OrderBy(ps => ps.FechaProximoServicio)
                            .ToListAsync();

                        recordatorios = segundos;
                        break;

                    case 3: // TERCER RECORDATORIO
                        var terceros = await _db.ProximosServicios
                            .Include(ps => ps.Cliente)
                            .Include(ps => ps.Vehiculo)
                            .Where(ps => ps.Activo
                                      && ps.PrimerRecordatorio
                                      && ps.SegundoRecordatorio
                                      && !ps.TercerRecordatorio
                                      && ps.FechaTercerRecordatorio <= fechaHoy)
                            .Select(ps => new RecordatorioServicioSimpleDto
                            {
                                Id = ps.Id,
                                ClienteNombre = ps.Cliente.NombreCompleto,
                                ProximoServicioNombre = ps.TipoProximoServicio,
                                FechaProximoServicio = ps.FechaProximoServicio
                            })
                            .OrderBy(ps => ps.FechaProximoServicio)
                            .ToListAsync();

                        recordatorios = terceros;
                        break;

                    default:
                        recordatorios = new List<RecordatorioServicioSimpleDto>();
                        break;
                }

                var nombreRecordatorio = tipoRecordatorio switch
                {
                    1 => "Primer Recordatorio",
                    2 => "Segundo Recordatorio",
                    3 => "Tercer Recordatorio",
                    _ => "Recordatorio"
                };

                _logger.LogInformation(
                    $"✅ Recordatorios tipo {tipoRecordatorio} obtenidos: {recordatorios.Count} registros");

                return Ok(new ObtenerRecordatoriosResponse
                {
                    Success = true,
                    Message = recordatorios.Any()
                        ? $"Se encontraron {recordatorios.Count} recordatorio(s)"
                        : "No hay recordatorios pendientes",
                    NombreRecordatorio = nombreRecordatorio,
                    Recordatorios = recordatorios,
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al obtener recordatorios tipo {tipoRecordatorio}");
                return StatusCode(500, new ObtenerRecordatoriosResponse
                {
                    Success = false,
                    Message = "Error al obtener recordatorios"
                });
            }
        }

        /// <summary>
        /// Obtener detalle completo de un recordatorio específico
        /// GET api/Recordatorios/detalle/{id}
        /// </summary>
        [HttpGet("detalle/{id}")]
        [ProducesResponseType(typeof(ObtenerRecordatorioResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ObtenerDetalleRecordatorio(int id)
        {
            try
            {
                _logger.LogInformation($"📥 Obteniendo detalle del recordatorio ID: {id}");

                var proximoServicio = await _db.ProximosServicios
                    .Include(ps => ps.Cliente)
                    .Include(ps => ps.Vehiculo)
                    .Where(ps => ps.Id == id && ps.Activo)
                    .FirstOrDefaultAsync();

                if (proximoServicio == null)
                {
                    _logger.LogWarning($"⚠️ Recordatorio ID {id} no encontrado o inactivo");
                    return NotFound(new ObtenerRecordatorioResponse
                    {
                        Success = false,
                        Message = "Recordatorio no encontrado o inactivo",
                        Recordatorios = new List<RecordatorioServicioDto>()
                    });
                }

                // Mapear usando el método auxiliar existente
                var recordatorioDetalle = MapearRecordatorio(proximoServicio);

                _logger.LogInformation($"✅ Detalle del recordatorio ID {id} obtenido exitosamente");

                return Ok(new ObtenerRecordatorioResponse
                {
                    Success = true,
                    Message = "Detalle del recordatorio obtenido exitosamente",
                    Recordatorios = new List<RecordatorioServicioDto> { recordatorioDetalle }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error al obtener detalle del recordatorio ID {id}");
                return StatusCode(500, new ObtenerRecordatorioResponse
                {
                    Success = false,
                    Message = $"Error al obtener el detalle del recordatorio: {ex.Message}",
                    Recordatorios = new List<RecordatorioServicioDto>()
                });
            }
        }

        /// <summary>
        /// Marcar recordatorio como enviado
        /// PUT api/Recordatorios/marcar-enviado
        /// </summary>
        [HttpPut("marcar-enviado")]
        [ProducesResponseType(typeof(RecordatorioResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> MarcarRecordatorioEnviado([FromBody] MarcarRecordatorioRequest request)
        {
            if (request.TipoRecordatorio < 1 || request.TipoRecordatorio > 3)
            {
                return BadRequest(new RecordatorioResponse
                {
                    Success = false,
                    Message = "Tipo de recordatorio inválido"
                });
            }

            try
            {
                var proximoServicio = await _db.ProximosServicios
                    .FirstOrDefaultAsync(ps => ps.Id == request.ProximoServicioId && ps.Activo);

                if (proximoServicio == null)
                {
                    return NotFound(new RecordatorioResponse
                    {
                        Success = false,
                        Message = "Registro de próximo servicio no encontrado"
                    });
                }

                // Actualizar el bit correspondiente
                switch (request.TipoRecordatorio)
                {
                    case 1:
                        proximoServicio.PrimerRecordatorio = true;
                        break;
                    case 2:
                        if (!proximoServicio.PrimerRecordatorio)
                        {
                            return BadRequest(new RecordatorioResponse
                            {
                                Success = false,
                                Message = "Debe enviar el primer recordatorio antes del segundo"
                            });
                        }
                        proximoServicio.SegundoRecordatorio = true;
                        break;
                    case 3:
                        if (!proximoServicio.PrimerRecordatorio || !proximoServicio.SegundoRecordatorio)
                        {
                            return BadRequest(new RecordatorioResponse
                            {
                                Success = false,
                                Message = "Debe enviar los recordatorios previos antes del tercero"
                            });
                        }
                        proximoServicio.TercerRecordatorio = true;
                        break;
                }

                proximoServicio.FechaModificacion = DateTime.Now;
                await _db.SaveChangesAsync();

                _logger.LogInformation(
                    $"✅ Recordatorio tipo {request.TipoRecordatorio} marcado como enviado para ProximoServicio ID {request.ProximoServicioId}");

                return Ok(new RecordatorioResponse
                {
                    Success = true,
                    Message = "Recordatorio marcado como enviado"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al marcar recordatorio {request.ProximoServicioId}");
                return StatusCode(500, new RecordatorioResponse
                {
                    Success = false,
                    Message = "Error al marcar recordatorio"
                });
            }
        }

        /// <summary>
        /// Obtener resumen de todos los recordatorios pendientes
        /// GET api/Recordatorios/resumen
        /// </summary>
        [HttpGet("resumen")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        public async Task<IActionResult> ObtenerResumenRecordatorios()
        {
            try
            {
                var fechaHoy = DateTime.Today.AddMonths(3);

                var primerRecordatorio = await _db.ProximosServicios
                    .CountAsync(ps => ps.Activo && !ps.PrimerRecordatorio
                                   && ps.FechaPrimerRecordatorio <= fechaHoy);

                var segundoRecordatorio = await _db.ProximosServicios
                    .CountAsync(ps => ps.Activo && ps.PrimerRecordatorio && !ps.SegundoRecordatorio
                                   && ps.FechaSegundoRecordatorio <= fechaHoy);

                var tercerRecordatorio = await _db.ProximosServicios
                    .CountAsync(ps => ps.Activo && ps.PrimerRecordatorio && ps.SegundoRecordatorio
                                   && !ps.TercerRecordatorio
                                   && ps.FechaTercerRecordatorio <= fechaHoy);

                return Ok(new
                {
                    Success = true,
                    FechaConsulta = fechaHoy,
                    PrimerRecordatorio = primerRecordatorio,
                    SegundoRecordatorio = segundoRecordatorio,
                    TercerRecordatorio = tercerRecordatorio,
                    TotalPendientes = primerRecordatorio + segundoRecordatorio + tercerRecordatorio
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener resumen de recordatorios");
                return StatusCode(500, new { Success = false, Message = "Error al obtener resumen" });
            }
        }

        // ============================================
        // MÉTODOS PRIVADOS AUXILIARES
        // ============================================

        private static RecordatorioServicioDto MapearRecordatorio(
            CarSlineAPI.Models.Entities.ProximoServicio ps)
        {
            return new RecordatorioServicioDto
            {
                Id = ps.Id,
                Clienteid = ps.ClienteId,
                Vehiculoid= ps.VehiculoId,
                ClienteNombre = ps.Cliente?.NombreCompleto ?? "",
                Telefono = ps.Cliente?.TelefonoMovil ?? "",
                TelefonoCasa = ps.Cliente?.TelefonoCasa ?? "",
                Correo = ps.Cliente?.CorreoElectronico ?? "Sin Correo Registrado",
                InfoVehiculo = ps.Vehiculo != null
                    ? $"{ps.Vehiculo.Marca} {ps.Vehiculo.Modelo} {ps.Vehiculo.Anio} {ps.Vehiculo.Version}"
                    : "",
                VIN = ps.Vehiculo?.VIN ?? "",
                Placas = ps.Vehiculo?.Placas ?? "Sin Placas",

                UltimoServicioRealizado = ps.UltimoServicioRealizado,
                FechaUltimoServicio = ps.FechaUltimoServicio,
                UltimoKilometraje = ps.UltimoKilometraje,
                TipoProximoServicio = ps.TipoProximoServicio,
                FechaProximoServicio = ps.FechaProximoServicio,
                KilometrajeProximoServicio = ps.KilometrajeProximoServicio,
            };
        }
    }
}