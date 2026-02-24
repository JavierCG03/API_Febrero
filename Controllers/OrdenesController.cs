// ============================================
// Controllers/OrdenesController.cs - ACTUALIZADO
// ============================================
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
    public class OrdenesController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<OrdenesController> _logger;

        public OrdenesController(ApplicationDbContext db, ILogger<OrdenesController> logger)
        {
            _db = db;
            _logger = logger;
        }

        [HttpPost("crear-con-trabajos")]
        public async Task<IActionResult> CrearOrdenConTrabajos(
            [FromBody] CrearOrdenConTrabajosRequest request,
            [FromHeader(Name = "X-User-Id")] int asesorId)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { Success = false, Message = "Datos inválidos" });

            var strategy = _db.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _db.Database.BeginTransactionAsync();

                try
                {
                    // 1. Generar número de orden
                    var prefijo = request.TipoOrdenId switch
                    {
                        1 => "SRV",
                        2 => "DIA",
                        3 => "REP",
                        4 => "GAR",
                        5 => "RTO",
                        _ => "ORD"
                    };

                    var maxNumero = await _db.OrdenesGenerales
                        .Where(o => o.NumeroOrden.StartsWith(prefijo + "-"))
                        .Select(o => o.NumeroOrden)
                        .ToListAsync();

                    int siguiente = 1;
                    if (maxNumero.Any())
                    {
                        var maxInt = maxNumero
                            .Select(s =>
                            {
                                var parts = s.Split('-', 2);
                                if (parts.Length < 2) return 0;
                                return int.TryParse(parts[1], out var n) ? n : 0;
                            })
                            .DefaultIfEmpty(0)
                            .Max();
                        siguiente = maxInt + 1;
                    }

                    var numeroOrden = $"{prefijo}-{siguiente:D6}";

                    // 2. Crear orden general
                    var ordenGeneral = new OrdenGeneral
                    {
                        NumeroOrden = numeroOrden,
                        TipoOrdenId = request.TipoOrdenId,
                        ClienteId = request.ClienteId,
                        VehiculoId = request.VehiculoId,
                        TipoServicioId = request.TipoServicioId,
                        AsesorId = asesorId,
                        KilometrajeActual = request.KilometrajeActual,
                        EstadoOrdenId = 1, // Pendiente
                        FechaHoraPromesaEntrega = request.FechaHoraPromesaEntrega,
                        ObservacionesAsesor = request.ObservacionesAsesor,
                        CostoTotal = 0, // Se calculará después
                        FechaCreacion = DateTime.Now,
                        Activo = true,
                        TotalTrabajos = request.Trabajos.Count,
                        TrabajosCompletados = 0,
                        ProgresoGeneral = 0
                    };

                    _db.OrdenesGenerales.Add(ordenGeneral);
                    await _db.SaveChangesAsync();

                    // 3. Crear trabajos asociados
                    foreach (var t in request.Trabajos)
                    {
                        var trabajo = new TrabajoPorOrden
                        {
                            OrdenGeneralId = ordenGeneral.Id,
                            Trabajo = t.Trabajo,
                            IndicacionesTrabajo = string.IsNullOrWhiteSpace(t.Indicaciones) ? null : t.Indicaciones,
                            EstadoTrabajo = 1,
                            Activo = true,
                            FechaCreacion = DateTime.Now
                        };

                        _db.TrabajosPorOrden.Add(trabajo);
                    }

                    await _db.SaveChangesAsync();
                    await transaction.CommitAsync();

                    _logger.LogInformation($"Orden {numeroOrden} creada con {request.Trabajos.Count} trabajos");

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

        /// <summary>
        /// Obtener órdenes por tipo (para asesor)
        /// GET api/Ordenes/asesor/{tipoOrdenId}
        /// </summary>
        [HttpGet("asesor/{tipoOrdenId}")]
        public async Task<IActionResult> ObtenerOrdenesPorTipo(
        int tipoOrdenId,
        [FromHeader(Name = "X-User-Id")] int asesorId)
        {
            try
            {
                var ordenes = await _db.OrdenesGenerales
                    .Include(o => o.Cliente)
                    .Include(o => o.Vehiculo)
                    .Include(o => o.Trabajos.Where(t => t.Activo))
                        .ThenInclude(t => t.TecnicoAsignado)
                    .Where(o => o.TipoOrdenId == tipoOrdenId
                             //&& o.AsesorId == asesorId
                             && o.Activo
                             && new[] { 1, 2, 3 }.Contains(o.EstadoOrdenId))
                    .OrderBy(o => o.FechaHoraPromesaEntrega)
                    .Select(o => new OrdenConTrabajosDto
                    {
                        Id = o.Id,
                        NumeroOrden = o.NumeroOrden,
                        TipoOrdenId = o.TipoOrdenId,
                        ClienteNombre = o.Cliente.NombreCompleto,
                        ClienteTelefono = o.Cliente.TelefonoMovil,
                        TipoServicio=o.TipoServicio.NombreServicio,
                        VehiculoCompleto = $"{o.Vehiculo.Marca} {o.Vehiculo.Modelo} {o.Vehiculo.Color} / {o.Vehiculo.Anio}",
                        VIN = o.Vehiculo.VIN,
                        Placas = o.Vehiculo.Placas ?? "",
                        FechaHoraPromesaEntrega = o.FechaHoraPromesaEntrega,
                        HoraFin = o.FechaFinalizacion,
                        EstadoOrdenId = o.EstadoOrdenId,
                        TotalTrabajos = o.TotalTrabajos,
                        TrabajosCompletados = o.TrabajosCompletados,
                        ProgresoGeneral = o.ProgresoGeneral,
                        CostoTotal = o.CostoTotal,
                        TieneEvidencia = o.TieneEvidencia,
                        Trabajos = o.Trabajos
                            .Where(t => t.Activo)
                            .Select(t => new TrabajoDto
                            {
                                Id = t.Id,
                                Trabajo = t.Trabajo,
                                TecnicoNombre = t.TecnicoAsignado != null ? t.TecnicoAsignado.NombreCompleto : null,
                                EstadoTrabajo = t.EstadoTrabajo,
                                FechaHoraInicio = t.FechaHoraInicio,
                                FechaHoraTermino = t.FechaHoraTermino
                            }).ToList()
                    })
                    .ToListAsync();
                return Ok(ordenes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener órdenes");
                return StatusCode(500, new { Message = "Error al obtener órdenes" });
            }
        }

        [HttpGet("Jefe-Taller/{tipoOrdenId}")]// Para obtener todas las ordenes generales 
        public async Task<IActionResult> ObtenerOrdenesPorTipo_Jefe(int tipoOrdenId)
        {
            try
            {
                var ordenes = await _db.OrdenesGenerales
                    .Include(o => o.Cliente)
                    .Include(o => o.Vehiculo)
                    .Include(o => o.Trabajos.Where(t => t.Activo))
                        .ThenInclude(t => t.TecnicoAsignado)
                    .Where(o => o.TipoOrdenId == tipoOrdenId
                             && o.Activo
                             && new[] { 1, 2, 3 }.Contains(o.EstadoOrdenId))
                    .OrderBy(o => o.FechaHoraPromesaEntrega)
                    .Select(o => new OrdenConTrabajosDto
                    {
                        Id = o.Id,
                        NumeroOrden = o.NumeroOrden,
                        TipoOrdenId = o.TipoOrdenId,
                        ClienteNombre = o.Cliente.NombreCompleto,
                        ClienteTelefono = o.Cliente.TelefonoMovil,
                        TipoServicio = o.TipoServicio.NombreServicio,
                        VehiculoCompleto = $"{o.Vehiculo.Marca} {o.Vehiculo.Modelo} {o.Vehiculo.Color} / {o.Vehiculo.Anio}",
                        VIN = o.Vehiculo.VIN,
                        Placas = o.Vehiculo.Placas ?? "",
                        FechaHoraPromesaEntrega = o.FechaHoraPromesaEntrega,
                        EstadoOrdenId = o.EstadoOrdenId,
                        TotalTrabajos = o.TotalTrabajos,
                        TrabajosCompletados = o.TrabajosCompletados,
                        ProgresoGeneral = o.ProgresoGeneral,
                        CostoTotal = o.CostoTotal,
                        Trabajos = o.Trabajos
                            .Where(t => t.Activo)
                            .Select(t => new TrabajoDto
                            {
                                Id = t.Id,
                                Trabajo = t.Trabajo,
                                TecnicoNombre = t.TecnicoAsignado != null ? t.TecnicoAsignado.NombreCompleto : null,
                                EstadoTrabajo = t.EstadoTrabajo,
                                FechaHoraInicio = t.FechaHoraInicio,
                                FechaHoraTermino = t.FechaHoraTermino
                            }).ToList()
                    })
                    .ToListAsync();

                return Ok(ordenes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener órdenes");
                return StatusCode(500, new { Message = "Error al obtener órdenes" });
            }
        }
        /// <summary>
        /// Obtener trabajos activos con información básica (para Jefe de Taller)
        /// GET api/Ordenes/Trabajos?fecha=2024-12-30
        /// Estados: 2=Asignado, 3=En Proceso, 4=Completado (solo del día especificado), 5=Pausado
        /// </summary>
        [HttpGet("Trabajos")]
        [ProducesResponseType(typeof(List<TrabajoSimpleDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ObtenerTrabajosTecnicos([FromQuery] DateTime? fecha = null)
        {
            try
            {
                // ✅ Si no se proporciona fecha, usar hoy
                var fechaFiltro = (fecha ?? DateTime.Today).Date;
                var fechaSiguiente = fechaFiltro.AddDays(1);

                var trabajos = await _db.TrabajosPorOrden
                    .Include(t => t.OrdenGeneral)
                    .Include(t => t.TecnicoAsignado)
                    .Include(t => t.EstadoTrabajoNavegacion)
                    .Where(t => t.Activo && (
                        new[] { 2, 3, 5 }.Contains(t.EstadoTrabajo) ||
                        (t.EstadoTrabajo == 4 &&
                         t.FechaHoraTermino.HasValue &&
                         t.FechaHoraTermino.Value >= fechaFiltro &&
                         t.FechaHoraTermino.Value < fechaSiguiente)
                    ))
                    .OrderBy(t => t.EstadoTrabajo)
                    .ThenBy(t => t.OrdenGeneral.FechaHoraPromesaEntrega)
                    .Select(t => new TrabajoSimpleDto
                    {
                        Trabajo = t.Trabajo,
                        FechaHoraPromesaEntrega = t.OrdenGeneral.FechaHoraPromesaEntrega,
                        TecnicoNombre = t.TecnicoAsignado != null ? t.TecnicoAsignado.NombreCompleto : "Sin asignar",
                        EstadoTrabajoNombre = t.EstadoTrabajoNavegacion != null ? t.EstadoTrabajoNavegacion.NombreEstado : "Desconocido",
                        FechaHoraAsignacionTecnico = t.FechaHoraAsignacionTecnico
                    })
                    .ToListAsync();

                _logger.LogInformation(
                    $"✅ Trabajos obtenidos para {fechaFiltro:yyyy-MM-dd}: {trabajos.Count} " +
                    $"(Completados: {trabajos.Count(t => t.EstadoTrabajoNombre == "Completado")})");

                return Ok(trabajos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener trabajos técnicos");
                return StatusCode(500, new { Message = "Error al obtener trabajos" });
            }
        }

        /// <summary>
        /// Obtener orden detallada con todos sus trabajos
        /// GET api/Ordenes/detalle/{ordenId}
        /// </summary>
        [HttpGet("detalle/{ordenId}")]
        public async Task<IActionResult> ObtenerDetalleOrden(int ordenId)
        {
            try
            {
                var orden = await _db.OrdenesGenerales
                    .Include(o => o.Cliente)
                    .Include(o => o.Vehiculo)
                    .Include(o =>o.TipoOrden)
                    .Include(o => o.TipoServicio)
                    .Include(o => o.Asesor)
                    .Include(o => o.EstadoOrden)
                    .Include(o => o.Trabajos.Where(t => t.Activo))
                        .ThenInclude(t => t.TecnicoAsignado)
                    .Include(o => o.Trabajos)
                        .ThenInclude(t => t.EstadoTrabajoNavegacion)
                    .Where(o => o.Id == ordenId && o.Activo)
                    .Select(o => new OrdenConTrabajosDto
                    {
                        Id = o.Id,
                        NumeroOrden = o.NumeroOrden,
                        TipoOrdenId = o.TipoOrdenId,
                        TipoOrden= o.TipoOrden.NombreTipo,
                        TipoServicio= o.TipoServicio.NombreServicio,
                        ClienteId= o.ClienteId,
                        ClienteNombre = o.Cliente.NombreCompleto,
                        ClienteTelefono = o.Cliente.TelefonoMovil,
                        VehiculoId = o.VehiculoId,
                        VehiculoCompleto = $"{o.Vehiculo.Marca} {o.Vehiculo.Modelo} {o.Vehiculo.Color} / {o.Vehiculo.Anio}",
                        VIN = o.Vehiculo.VIN,
                        Placas = o.Vehiculo.Placas ?? "",
                        AsesorNombre = o.Asesor.NombreCompleto,
                        KilometrajeActual = o.KilometrajeActual,
                        FechaCreacion = o.FechaCreacion,
                        FechaHoraPromesaEntrega = o.FechaHoraPromesaEntrega,
                        EstadoOrdenId = o.EstadoOrdenId,
                        EstadoOrden = o.EstadoOrden.NombreEstado,
                        CostoTotal = o.CostoTotal,
                        TotalTrabajos = o.TotalTrabajos,
                        TrabajosCompletados = o.TrabajosCompletados,
                        ProgresoGeneral = o.ProgresoGeneral,
                        ObservacionesAsesor = o.ObservacionesAsesor,
                        Trabajos = o.Trabajos
                            .Where(t => t.Activo)
                            .Select(t => new TrabajoDto
                            {
                                Id = t.Id,
                                Trabajo = t.Trabajo,
                                TecnicoAsignadoId = t.TecnicoAsignadoId,
                                TecnicoNombre = t.TecnicoAsignado != null ? t.TecnicoAsignado.NombreCompleto : null,
                                FechaHoraAsignacionTecnico = t.FechaHoraAsignacionTecnico,
                                FechaHoraInicio = t.FechaHoraInicio,
                                FechaHoraTermino = t.FechaHoraTermino,
                                IndicacionesTrabajo = t.IndicacionesTrabajo,
                                ComentariosTecnico = t.ComentariosTecnico,
                                ComentariosJefeTaller = t.ComentariosJefeTaller,
                                EstadoTrabajo = t.EstadoTrabajo,
                                EstadoTrabajoNombre = t.EstadoTrabajoNavegacion != null ? t.EstadoTrabajoNavegacion.NombreEstado : null,
                                ColorEstado = t.EstadoTrabajoNavegacion != null ? t.EstadoTrabajoNavegacion.Color : null
                            }).ToList()
                    })
                    .FirstOrDefaultAsync();

                if (orden == null)
                    return NotFound(new { Message = "Orden no encontrada" });

                return Ok(orden);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al obtener detalle de orden {ordenId}");
                return StatusCode(500, new { Message = "Error al obtener detalle de orden" });
            }
        }

        /// <summary>
        /// Cancelar orden
        /// PUT api/Ordenes/cancelar/{ordenId}
        /// </summary>
        [HttpPut("cancelar/{ordenId}")]
        public async Task<IActionResult> CancelarOrden(int ordenId)
        {
            try
            {
                var orden = await _db.OrdenesGenerales.FindAsync(ordenId);
                if (orden == null)
                    return NotFound(new { Success = false, Message = "Orden no encontrada" });

                orden.EstadoOrdenId = 5; // Cancelada
                orden.Activo = false;

                // Cancelar todos los trabajos pendientes
                var trabajos = await _db.TrabajosPorOrden
                    .Where(t => t.OrdenGeneralId == ordenId && t.Activo && (t.EstadoTrabajo == 1 || t.EstadoTrabajo == 2))
                    .ToListAsync();

                foreach (var trabajo in trabajos)
                {
                    trabajo.EstadoTrabajo = 6; // Cancelado
                }

                await _db.SaveChangesAsync();

                _logger.LogInformation($"Orden {ordenId} cancelada");

                return Ok(new { Success = true, Message = "Orden cancelada" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al cancelar orden {ordenId}");
                return StatusCode(500, new { Success = false, Message = "Error al cancelar orden" });
            }
        }

        /// <summary>
        /// Entregar orden (solo si todos los trabajos están completados)
        /// PUT api/Ordenes/entregar/{ordenId}
        /// </summary>
        
        [HttpPut("entregar/{ordenId}")]
        public async Task<IActionResult> EntregarOrden(int ordenId)
        {
            try
            {
                var orden = await _db.OrdenesGenerales
                    .Include(o => o.Trabajos)
                    .FirstOrDefaultAsync(o => o.Id == ordenId);

                if (orden == null)
                    return NotFound(new { Success = false, Message = "Orden no encontrada" });

                // Verificar que todos los trabajos estén completados
                var trabajosPendientes = orden.Trabajos.Count(t => t.Activo && t.EstadoTrabajo != 4);
                if (trabajosPendientes > 0)
                    return BadRequest(new
                    {
                        Success = false,
                        Message = $"No se puede entregar. Hay {trabajosPendientes} trabajo(s) sin completar"
                    });

                if (orden.TipoOrdenId != 4 && orden.CostoTotal==0)
                {
                    return BadRequest(new
                    {
                        Success = false,
                        Message = $"No se puede entregar. Falta definir los costos de Refacciones y Mano de Obra"
                    });


                }

                orden.EstadoOrdenId = 4; // Entregada
                orden.FechaEntrega = DateTime.Now;

                await _db.SaveChangesAsync();

                await CrearOActualizarProximoServicio(orden);

                _logger.LogInformation($"Orden {ordenId} entregada");

                return Ok(new { Success = true, Message = "Orden entregada" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al entregar orden {ordenId}");
                return StatusCode(500, new { Success = false, Message = "Error al entregar orden" });
            }
        }

        /// <summary>
        /// Crear o actualizar el próximo servicio cuando se entrega una orden de servicio
        /// </summary>
        private async Task CrearOActualizarProximoServicio(OrdenGeneral orden)
        {
            try
            {
                if (orden.TipoOrdenId != 1)
                {
                    _logger.LogInformation($"Orden {orden.Id} no es de servicio. No se crea próximo servicio.");
                    return;
                }


                // Validar que tenga TipoServicioId
                if (!orden.TipoServicioId.HasValue)
                {
                    _logger.LogWarning($"Orden {orden.Id} no tiene TipoServicioId definido");
                    return;
                }

                // Determinar el próximo servicio basado en el actual
                string proximoServicio = orden.TipoServicioId.Value switch
                {
                    1 => "Segundo Servicio",
                    2 => "Tercer Servicio",
                    3 => "Servicio Externo",
                    _ => "Servicio Externo"
                };

                // Obtener el tipo de servicio
                var tipoServicio = await _db.TiposServicio
                    .FirstOrDefaultAsync(ts => ts.Id == orden.TipoServicioId.Value);

                if (tipoServicio == null)
                {
                    _logger.LogWarning($"No se encontró el tipo de servicio {orden.TipoServicioId.Value}");
                    return;
                }

                // Buscar si ya existe un registro para este vehículo
                var proximoServicioExistente = await _db.ProximosServicios
                    .FirstOrDefaultAsync(ps => ps.VehiculoId == orden.VehiculoId);

                if (proximoServicioExistente != null)
                {
                    // ACTUALIZAR registro existente
                    proximoServicioExistente.ClienteId = orden.ClienteId;
                    proximoServicioExistente.UltimoServicioRealizado = tipoServicio.NombreServicio;
                    proximoServicioExistente.TipoProximoServicio = proximoServicio;
                    proximoServicioExistente.UltimoKilometraje = orden.KilometrajeActual;
                    proximoServicioExistente.FechaUltimoServicio = orden.FechaEntrega ?? DateTime.Today;
                    proximoServicioExistente.PrimerRecordatorio = false;
                    proximoServicioExistente.SegundoRecordatorio = false;
                    proximoServicioExistente.TercerRecordatorio = false;
                    proximoServicioExistente.Activo = true;
                    proximoServicioExistente.FechaModificacion = DateTime.Now;

                    _db.ProximosServicios.Update(proximoServicioExistente);

                    _logger.LogInformation($"✅ Próximo servicio ACTUALIZADO - Vehículo: {orden.VehiculoId}, Próximo: {proximoServicio}");
                }
                else
                {
                    // CREAR nuevo registro
                    var nuevoProximoServicio = new ProximoServicio
                    {
                        ClienteId = orden.ClienteId,
                        VehiculoId = orden.VehiculoId,
                        TipoProximoServicio = proximoServicio,
                        UltimoServicioRealizado = tipoServicio.NombreServicio,
                        UltimoKilometraje = orden.KilometrajeActual,
                        FechaUltimoServicio = orden.FechaEntrega ?? DateTime.Today,
                        PrimerRecordatorio = false,
                        SegundoRecordatorio = false,
                        TercerRecordatorio = false,
                        Activo = true,
                        FechaModificacion = DateTime.Now
                    };

                    await _db.ProximosServicios.AddAsync(nuevoProximoServicio);

                    _logger.LogInformation($"✅ Próximo servicio CREADO - Vehículo: {orden.VehiculoId}, Próximo: {proximoServicio}");
                }

                await _db.SaveChangesAsync();

                _logger.LogInformation($"💾 Próximo servicio guardado exitosamente para orden {orden.Id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error al crear/actualizar próximo servicio para orden {orden.Id}");
                // No lanzar excepción para no afectar la entrega
            }
        }
    }  
}