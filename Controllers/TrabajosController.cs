// ============================================
// Controllers/TrabajosController.cs
// ============================================
using CarSlineAPI.Data;
using CarSlineAPI.Models.DTOs;
using CarSlineAPI.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarSlineAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TrabajosController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<TrabajosController> _logger;

        public TrabajosController(ApplicationDbContext db, ILogger<TrabajosController> logger)
        {
            _db = db;
            _logger = logger;
        }

        /// <summary>
        /// Obtener todos los trabajos de una orden
        /// GET api/Trabajos/orden/{ordenId}
        /// </summary>
        [HttpGet("orden/{ordenId}")]
        [ProducesResponseType(typeof(List<TrabajoDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ObtenerTrabajosPorOrden(int ordenId)
        {
            try
            {
                var trabajos = await _db.Set<TrabajoPorOrden>()
                    .Include(t => t.TecnicoAsignado)
                    .Include(t => t.EstadoTrabajoNavegacion)
                    .Where(t => t.OrdenGeneralId == ordenId && t.Activo)
                    .OrderBy(t => t.FechaCreacion)
                    .Select(t => new TrabajoDto
                    {
                        Id = t.Id,
                        OrdenGeneralId = t.OrdenGeneralId,
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
                        ColorEstado = t.EstadoTrabajoNavegacion != null ? t.EstadoTrabajoNavegacion.Color : null,
                        FechaCreacion = t.FechaCreacion
                    })
                    .ToListAsync();

                return Ok(trabajos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al obtener trabajos de orden {ordenId}");
                return StatusCode(500, new { Message = "Error al obtener trabajos" });
            }
        }

        /// <summary>
        /// Obtener orden completa con todos sus trabajos
        /// GET api/Trabajos/orden-completa/{ordenId}
        /// </summary>
        [HttpGet("orden-completa/{ordenId}")]
        [ProducesResponseType(typeof(OrdenConTrabajosDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> ObtenerOrdenCompleta(int ordenId)
        {
            try
            {
                var orden = await _db.OrdenesGenerales
                    .Include(o => o.Cliente)
                    .Include(o => o.Vehiculo)
                    .Include(o => o.Asesor)
                    .Include(o => o.Trabajos)
                        .ThenInclude(t => t.TecnicoAsignado)
                    .Include(o => o.Trabajos)
                        .ThenInclude(t => t.EstadoTrabajoNavegacion)
                    .Where(o => o.Id == ordenId && o.Activo)
                    .Select(o => new OrdenConTrabajosDto
                    {
                        Id = o.Id,
                        NumeroOrden = o.NumeroOrden,
                        TipoOrdenId = o.TipoOrdenId,
                        ClienteNombre = o.Cliente.NombreCompleto,
                        ClienteTelefono = o.Cliente.TelefonoMovil,
                        VehiculoCompleto = $"{o.Vehiculo.Marca} {o.Vehiculo.Modelo} {o.Vehiculo.Color} / {o.Vehiculo.Anio}",
                        VIN = o.Vehiculo.VIN,
                        Placas = o.Vehiculo.Placas ?? "",
                        AsesorNombre = o.Asesor.NombreCompleto,
                        KilometrajeActual = o.KilometrajeActual,
                        FechaCreacion = o.FechaCreacion,
                        FechaHoraPromesaEntrega = o.FechaHoraPromesaEntrega,
                        EstadoOrdenId = o.EstadoOrdenId,
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
                                OrdenGeneralId = t.OrdenGeneralId,
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
                                ColorEstado = t.EstadoTrabajoNavegacion != null ? t.EstadoTrabajoNavegacion.Color : null,
                                FechaCreacion = t.FechaCreacion
                            }).ToList()
                    })
                    .FirstOrDefaultAsync();

                if (orden == null)
                    return NotFound(new { Message = "Orden no encontrada" });

                return Ok(orden);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al obtener orden completa {ordenId}");
                return StatusCode(500, new { Message = "Error al obtener orden" });
            }
        }

        /// <summary>
        /// Agregar trabajo a una orden existente
        /// POST api/Trabajos/agregar
        /// </summary>
        [HttpPost("agregar/{ordenId}")]
        [ProducesResponseType(typeof(TrabajoResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> AgregarTrabajo(int ordenId, [FromBody] CrearTrabajoRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(new TrabajoResponse
                {
                    Success = false,
                    Message = "Datos inválidos"
                });

            try
            {
                var orden = await _db.OrdenesGenerales.FindAsync(ordenId);
                if (orden == null || !orden.Activo)
                    return NotFound(new TrabajoResponse
                    {
                        Success = false,
                        Message = "Orden no encontrada"
                    });

                var trabajo = new TrabajoPorOrden
                {
                    OrdenGeneralId = ordenId,
                    Trabajo = request.Trabajo,
                    TecnicoAsignadoId = request.TecnicoAsignadoId,
                    FechaHoraAsignacionTecnico = request.TecnicoAsignadoId.HasValue ? DateTime.Now : null,
                    EstadoTrabajo = 1, // Pendiente
                    Activo = true,
                    FechaCreacion = DateTime.Now
                };

                _db.Set<TrabajoPorOrden>().Add(trabajo);
                await _db.SaveChangesAsync();

                // Recargar con relaciones
                await _db.Entry(trabajo)
                    .Reference(t => t.TecnicoAsignado)
                    .LoadAsync();
                await _db.Entry(trabajo)
                    .Reference(t => t.EstadoTrabajoNavegacion)
                    .LoadAsync();

                _logger.LogInformation($"Trabajo agregado a orden {ordenId}");

                return Ok(new TrabajoResponse
                {
                    Success = true,
                    Message = "Trabajo agregado exitosamente",
                    Trabajo = new TrabajoDto
                    {
                        Id = trabajo.Id,
                        OrdenGeneralId = trabajo.OrdenGeneralId,
                        Trabajo = trabajo.Trabajo,
                        TecnicoAsignadoId = trabajo.TecnicoAsignadoId,
                        TecnicoNombre = trabajo.TecnicoAsignado?.NombreCompleto,
                        EstadoTrabajo = trabajo.EstadoTrabajo,
                        EstadoTrabajoNombre = trabajo.EstadoTrabajoNavegacion?.NombreEstado,
                        ColorEstado = trabajo.EstadoTrabajoNavegacion?.Color,
                        FechaCreacion = trabajo.FechaCreacion
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al agregar trabajo");
                return StatusCode(500, new TrabajoResponse
                {
                    Success = false,
                    Message = "Error al agregar trabajo"
                });
            }
        }

        /// <summary>
        /// Asignar técnico a un trabajo
        /// PUT api/Trabajos/asignar-tecnico
        /// </summary>
        // En tu TrabajosController.cs

        [HttpPut("{trabajoId}/asignar-tecnico/{tecnicoId}")]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> AsignarTecnico(
            int trabajoId,
            int tecnicoId,
            [FromHeader(Name = "X-User-Id")] int jefeId)
        {
            try
            {
                // Validar que el usuario que hace la petición es Jefe de Taller
                var jefe = await _db.Usuarios.FindAsync(jefeId);
                if (jefe == null || !jefe.Activo || jefe.RolId != 3) // RolId 3 = Jefe de Taller
                {
                    return Unauthorized(new AuthResponse
                    {
                        Success = false,
                        Message = "No tienes permisos para asignar técnicos"
                    });
                }

                // Buscar el trabajo
                var trabajo = await _db.Set<TrabajoPorOrden>()
                    .Include(t => t.OrdenGeneral)
                    .FirstOrDefaultAsync(t => t.Id == trabajoId);

                if (trabajo == null || !trabajo.Activo)
                {
                    return NotFound(new AuthResponse
                    {
                        Success = false,
                        Message = "Trabajo no encontrado"
                    });
                }

                // Validar que el trabajo esté en estado Pendiente (1)
                if (trabajo.EstadoTrabajo != 1)
                {
                    return BadRequest(new AuthResponse
                    {
                        Success = false,
                        Message = "Solo se pueden asignar trabajos en estado pendiente"
                    });
                }

                // Validar que el trabajo no tenga técnico asignado
                if (trabajo.TecnicoAsignadoId.HasValue)
                {
                    return BadRequest(new AuthResponse
                    {
                        Success = false,
                        Message = "Este trabajo ya tiene un técnico asignado. Use reasignar si desea cambiarlo."
                    });
                }

                // Buscar y validar el técnico
                var tecnico = await _db.Usuarios.FindAsync(tecnicoId);
                if (tecnico == null || !tecnico.Activo || tecnico.RolId != 5)
                {
                    return BadRequest(new AuthResponse
                    {
                        Success = false,
                        Message = "Técnico no válido o no encontrado"
                    });
                }

                // Asignar técnico
                trabajo.TecnicoAsignadoId = tecnicoId;
                trabajo.FechaHoraAsignacionTecnico = DateTime.Now;
                trabajo.EstadoTrabajo = 2; // Asignado

                await _db.SaveChangesAsync();

                _logger.LogInformation(
                    $"Jefe {jefeId} asignó técnico {tecnicoId} ({tecnico.NombreCompleto}) al trabajo {trabajoId} de la orden {trabajo.OrdenGeneralId}");

                return Ok(new AuthResponse
                {
                    Success = true,
                    Message = $"Técnico {tecnico.NombreCompleto} asignado exitosamente"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al asignar técnico {tecnicoId} al trabajo {trabajoId}");
                return StatusCode(500, new AuthResponse
                {
                    Success = false,
                    Message = "Error interno al asignar técnico"
                });
            }
        }

        [HttpPut("{trabajoId}/reasignar-tecnico/{nuevoTecnicoId}")]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> ReasignarTecnico(
            int trabajoId,
            int nuevoTecnicoId,
            [FromHeader(Name = "X-User-Id")] int jefeId)
        {
            try
            {
                // Validar que el usuario que hace la petición es Jefe de Taller
                var jefe = await _db.Usuarios.FindAsync(jefeId);
                if (jefe == null || !jefe.Activo || jefe.RolId != 3)
                {
                    return Unauthorized(new AuthResponse
                    {
                        Success = false,
                        Message = "No tienes permisos para reasignar técnicos"
                    });
                }

                // Buscar el trabajo
                var trabajo = await _db.Set<TrabajoPorOrden>()
                    .Include(t => t.OrdenGeneral)
                    .Include(t => t.TecnicoAsignado)
                    .FirstOrDefaultAsync(t => t.Id == trabajoId);

                if (trabajo == null || !trabajo.Activo)
                {
                    return NotFound(new AuthResponse
                    {
                        Success = false,
                        Message = "Trabajo no encontrado"
                    });
                }

                // Validar que el trabajo tenga técnico asignado
                if (!trabajo.TecnicoAsignadoId.HasValue)
                {
                    return BadRequest(new AuthResponse
                    {
                        Success = false,
                        Message = "Este trabajo no tiene técnico asignado. Use asignar en su lugar."
                    });
                }

                // Validar que el trabajo NO esté en proceso (3), completado (4), pausado (5) o cancelado (6)
                if (trabajo.EstadoTrabajo == 3 || trabajo.EstadoTrabajo == 4 || trabajo.EstadoTrabajo == 6)
                {
                    return BadRequest(new AuthResponse
                    {
                        Success = false,
                        Message = "No se puede reasignar un trabajo que ya está en proceso, completado, pausado o cancelado"
                    });
                }

                // Validar que el nuevo técnico sea diferente al actual
                if (trabajo.TecnicoAsignadoId == nuevoTecnicoId)
                {
                    return BadRequest(new AuthResponse
                    {
                        Success = false,
                        Message = "El técnico seleccionado ya está asignado a este trabajo"
                    });
                }

                // Buscar y validar el nuevo técnico
                var nuevoTecnico = await _db.Usuarios.FindAsync(nuevoTecnicoId);
                if (nuevoTecnico == null || !nuevoTecnico.Activo || nuevoTecnico.RolId != 5)
                {
                    return BadRequest(new AuthResponse
                    {
                        Success = false,
                        Message = "Técnico no válido o no encontrado"
                    });
                }

                // Guardar el técnico anterior para el log
                string tecnicoAnterior = trabajo.TecnicoAsignado?.NombreCompleto ?? "Desconocido";

                // Reasignar técnico
                trabajo.TecnicoAsignadoId = nuevoTecnicoId;
                trabajo.FechaHoraAsignacionTecnico = DateTime.Now;
                trabajo.EstadoTrabajo = 2; // Asignado

                // Limpiar fechas de inicio si existían (porque cambia de técnico)
                trabajo.FechaHoraInicio = null;

                await _db.SaveChangesAsync();

                _logger.LogInformation(
                    $"Jefe {jefeId} reasignó trabajo {trabajoId} de {tecnicoAnterior} a {nuevoTecnico.NombreCompleto}");

                return Ok(new AuthResponse
                {
                    Success = true,
                    Message = $"Trabajo reasignado a {nuevoTecnico.NombreCompleto}"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al reasignar técnico en trabajo {trabajoId}");
                return StatusCode(500, new AuthResponse
                {
                    Success = false,
                    Message = "Error interno al reasignar técnico"
                });
            }
        }
        /// <summary>
        /// Iniciar trabajo (técnico comienza a trabajar)
        /// PUT api/Trabajos/iniciar/{trabajoId}
        /// </summary>
        [HttpPut("iniciar/{trabajoId}")]
        [ProducesResponseType(typeof(TrabajoResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> IniciarTrabajo(
            int trabajoId,
            [FromHeader(Name = "X-User-Id")] int tecnicoId)
        {

            try
            {
                var trabajo = await _db.Set<TrabajoPorOrden>().FindAsync(trabajoId);


                if (trabajo == null || !trabajo.Activo)
                    return NotFound(new TrabajoResponse
                    {
                        Success = false,
                        Message = "Trabajo no encontrado"
                    });

                if (trabajo.TecnicoAsignadoId != tecnicoId)
                    return Unauthorized(new TrabajoResponse
                    {
                        Success = false,
                        Message = "No estás asignado a este trabajo"
                    });

                if (trabajo.EstadoTrabajo > 2)
                    return BadRequest(new TrabajoResponse
                    {
                        Success = false,
                        Message = "El trabajo ya fue iniciado"
                    });

                trabajo.EstadoTrabajo = 3; // En Proceso
                trabajo.FechaHoraInicio = DateTime.Now;

                await _db.SaveChangesAsync();

                _logger.LogInformation($"Trabajo {trabajoId} iniciado por técnico {tecnicoId}");

                return Ok(new TrabajoResponse
                {
                    Success = true,
                    Message = "Trabajo iniciado exitosamente"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al iniciar trabajo");
                return StatusCode(500, new TrabajoResponse
                {
                    Success = false,
                    Message = "Error al iniciar trabajo"
                });
            }
        }


        [HttpPut("restablecer-pendiente/{trabajoId}")]
        [ProducesResponseType(typeof(TrabajoResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> RestablecerTrabajo(
            int trabajoId,
            [FromHeader(Name = "X-User-Id")] int tecnicoId)
        {
            try
            {
                var trabajo = await _db.Set<TrabajoPorOrden>().FindAsync(trabajoId);


                if (trabajo == null || !trabajo.Activo)
                    return NotFound(new TrabajoResponse
                    {
                        Success = false,
                        Message = "Trabajo no encontrado"
                    });

                if (trabajo.TecnicoAsignadoId != tecnicoId)
                    return Unauthorized(new TrabajoResponse
                    {
                        Success = false,
                        Message = "No estás asignado a este trabajo"
                    });

                if (trabajo.EstadoTrabajo != 3)
                    return BadRequest(new TrabajoResponse
                    {
                        Success = false,
                        Message = "El trabajo no se puede restablecer"
                    });

                trabajo.EstadoTrabajo = 2; // Asignado

                await _db.SaveChangesAsync();

                _logger.LogInformation($"Trabajo {trabajoId} restablecido por técnico {tecnicoId}");

                return Ok(new TrabajoResponse
                {
                    Success = true,
                    Message = "Trabajo restablecido exitosamente"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al restablecer trabajo");
                return StatusCode(500, new TrabajoResponse
                {
                    Success = false,
                    Message = "Error al restablecer trabajo"
                });
            }
        }

        [HttpPut("Pausar/{trabajoId}")]
        [ProducesResponseType(typeof(TrabajoResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> PausarTrabajo(
            int trabajoId,
            [FromHeader(Name = "X-User-Id")] int tecnicoId,
            [FromBody] string motivo)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(motivo))
                {
                    return BadRequest(new TrabajoResponse
                    {
                        Success = false,
                        Message = "El motivo de la pausa es obligatorio"
                    });
                }

                var trabajo = await _db.Set<TrabajoPorOrden>()
                    .FirstOrDefaultAsync(t => t.Id == trabajoId && t.Activo);

                if (trabajo == null)
                    return NotFound(new TrabajoResponse
                    {
                        Success = false,
                        Message = "Trabajo no encontrado"
                    });

                if (trabajo.TecnicoAsignadoId != tecnicoId)
                    return Unauthorized(new TrabajoResponse
                    {
                        Success = false,
                        Message = "No estás asignado a este trabajo"
                    });

                if (trabajo.EstadoTrabajo != 3)
                    return BadRequest(new TrabajoResponse
                    {
                        Success = false,
                        Message = "El trabajo no se puede pausar porque no está en proceso"
                    });

                trabajo.EstadoTrabajo = 5; // Pausado

                var pausa = new pausatrabajo
                {
                    TrabajoId = trabajo.Id,
                    OrdenGeneralId = trabajo.OrdenGeneralId,
                    Motivo = motivo,
                    FechaHoraPausa = DateTime.Now
                };

                _db.Set<pausatrabajo>().Add(pausa);

                await _db.SaveChangesAsync();

                _logger.LogInformation($"Trabajo {trabajoId} pausado por técnico {tecnicoId}");

                return Ok(new TrabajoResponse
                {
                    Success = true,
                    Message = "Trabajo pausado exitosamente"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al pausar trabajo");
                return StatusCode(500, new TrabajoResponse
                {
                    Success = false,
                    Message = "Error al pausar trabajo"
                });
            }
        }


        [HttpPut("Reanudar/{trabajoId}")]
        [ProducesResponseType(typeof(TrabajoResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> ReanudarTrabajo(
            int trabajoId,
            [FromHeader(Name = "X-User-Id")] int tecnicoId)
        {
            try
            {
                var trabajo = await _db.Set<TrabajoPorOrden>()
                    .FirstOrDefaultAsync(t => t.Id == trabajoId && t.Activo);

                if (trabajo == null)
                    return NotFound(new TrabajoResponse
                    {
                        Success = false,
                        Message = "Trabajo no encontrado"
                    });

                if (trabajo.TecnicoAsignadoId != tecnicoId)
                    return Unauthorized(new TrabajoResponse
                    {
                        Success = false,
                        Message = "No estás asignado a este trabajo"
                    });

                if (trabajo.EstadoTrabajo != 5) // Pausado
                    return BadRequest(new TrabajoResponse
                    {
                        Success = false,
                        Message = "El trabajo no se puede reanudar porque no está pausado"
                    });

                // 🔎 Buscar la última pausa activa (sin reanudación)
                var pausaActiva = await _db.Set<pausatrabajo>()
                    .Where(p => p.TrabajoId == trabajoId && p.FechaHoraReanudacion == null)
                    .OrderByDescending(p => p.FechaHoraPausa)
                    .FirstOrDefaultAsync();

                if (pausaActiva == null)
                    return BadRequest(new TrabajoResponse
                    {
                        Success = false,
                        Message = "No existe una pausa activa para este trabajo"
                    });

                pausaActiva.FechaHoraReanudacion = DateTime.Now;

                trabajo.EstadoTrabajo = 3; 

                await _db.SaveChangesAsync();

                _logger.LogInformation(
                    $"Trabajo {trabajoId} reanudado por técnico {tecnicoId}. Pausa cerrada: {pausaActiva.Id}");

                return Ok(new TrabajoResponse
                {
                    Success = true,
                    Message = "Trabajo reanudado exitosamente"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al reanudar trabajo");
                return StatusCode(500, new TrabajoResponse
                {
                    Success = false,
                    Message = "Error al reanudar el trabajo"
                });
            }
        }

        [HttpPut("FijarCostoManoObra/{trabajoId}")]
        [ProducesResponseType(typeof(ManoObraResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> FijarManoObra(
           int trabajoId,
           [FromBody] FijarManoObraRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ManoObraResponse
                {
                    Success = false,
                    Message = "Datos inválidos"
                });
            }

            try
            {
                var trabajo = await _db.TrabajosPorOrden
                    .Include(t => t.OrdenGeneral)
                    .FirstOrDefaultAsync(t => t.Id == trabajoId && t.Activo);

                if (trabajo == null)
                {
                    return NotFound(new ManoObraResponse
                    {
                        Success = false,
                        Message = "Trabajo no encontrado"
                    });
                }


                if (trabajo.EstadoTrabajo == 6)
                {
                    return BadRequest(new ManoObraResponse
                    {
                        Success = false,
                        Message = "No se puede fijar mano de obra a un trabajo cancelado",
                        
                    });
                }


                if (request.CostoManoObra < 0)
                {
                    return BadRequest(new ManoObraResponse
                    {
                        Success = false,
                        Message = "El costo de mano de obra debe ser mayor o igual a 0"
                    });
                }

                // Actualizar el costo de mano de obra
                trabajo.CostoManoObra = request.CostoManoObra;

                await _db.SaveChangesAsync();

                return Ok(new ManoObraResponse
                {
                    Success = true,
                    Message = "Costo de mano de obra actualizado exitosamente",
                    CostoManoObra = trabajo.CostoManoObra

                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al fijar mano de obra del trabajo {trabajoId}");
                return StatusCode(500, new ManoObraResponse
                {
                    Success = false,
                    Message = "Error al fijar el costo de mano de obra"
                });
            }
        }

        [HttpGet("CostoManoObra/{trabajoId}")]
        [ProducesResponseType(typeof(ManoObraResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ObtenerManoObraPorTrabajo(int trabajoId)
        {
            try
            {
                var trabajo = await _db.TrabajosPorOrden
                    .Where(t => t.Id == trabajoId && t.Activo)
                    .Select(t => new
                    { 
                        t.CostoManoObra,
                    })
                    .FirstOrDefaultAsync();

            if (trabajo == null)
            {
                return NotFound(new ManoObraResponse
                {
                    Success = false,
                    Message = "Trabajo no encontrado"
                });
            }

            return Ok(new ManoObraResponse
            {
                Success = true,
                Message = "Información de mano de obra obtenida exitosamente",
                CostoManoObra = trabajo.CostoManoObra,

            });
        }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al obtener mano de obra del trabajo {trabajoId}");
                return StatusCode(500, new ManoObraResponse
                {
                    Success = false,
                    Message = "Error al obtener información de mano de obra"
                });
            }
        }

        [HttpGet("Trabajo/{trabajoId}")]
        [ProducesResponseType(typeof(TrabajoSimpleResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> InfoTrabajoSimple(int trabajoId)
        {
            try
            {
                var trabajo = await _db.TrabajosPorOrden
                    .Where(t => t.Id == trabajoId && t.Activo)
                    .Select(t => new
                    {
                        t.Trabajo,
                        Vehiculo = t.OrdenGeneral.Vehiculo
                    })
                    .FirstOrDefaultAsync();

                if (trabajo == null)
                {
                    return NotFound(new TrabajoSimpleResponse
                    {
                        Success = false,
                        Message = "Trabajo no encontrado"
                    });
                }

                return Ok(new TrabajoSimpleResponse
                {
                    Success = true,
                    Message = "Información de trabajo obtenida",
                    Trabajo = trabajo.Trabajo,
                    VIN = trabajo.Vehiculo.VIN,
                    VehiculoCompleto = $"{trabajo.Vehiculo.Marca} {trabajo.Vehiculo.Modelo} {trabajo.Vehiculo.Version} {trabajo.Vehiculo.Anio}"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al Obtener el Trabajo {trabajoId}");
                return StatusCode(500, new TrabajoSimpleResponse
                {
                    Success = false,
                    Message = "Error al obtener información del Trabajo"
                });
            }
        }
        /// <summary>
        /// Completar trabajo
        /// PUT api/Trabajos/completar/{trabajoId}
        /// </summary>
        [HttpPut("completar/{trabajoId}")]
        [ProducesResponseType(typeof(TrabajoResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> CompletarTrabajo(
            int trabajoId,
            [FromHeader(Name = "X-User-Id")] int tecnicoId,
            [FromBody] string? comentarios = null)
        {
            try
            {
                var trabajo = await _db.Set<TrabajoPorOrden>().FindAsync(trabajoId);

                if (trabajo == null || !trabajo.Activo)
                    return NotFound(new TrabajoResponse
                    {
                        Success = false,
                        Message = "Trabajo no encontrado"
                    });

                if (trabajo.TecnicoAsignadoId != tecnicoId)
                    return Unauthorized(new TrabajoResponse
                    {
                        Success = false,
                        Message = "No estás asignado a este trabajo"
                    });

                if (trabajo.EstadoTrabajo != 3)
                    return BadRequest(new TrabajoResponse
                    {
                        Success = false,
                        Message = "El trabajo no está en proceso"
                    });

                trabajo.EstadoTrabajo = 4; // Completado
                trabajo.FechaHoraTermino = DateTime.Now;
                trabajo.ComentariosTecnico = comentarios;

                await _db.SaveChangesAsync();

                _logger.LogInformation($"Trabajo {trabajoId} completado por técnico {tecnicoId}");

                return Ok(new TrabajoResponse
                {
                    Success = true,
                    Message = "Trabajo completado exitosamente"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al completar trabajo");
                return StatusCode(500, new TrabajoResponse
                {
                    Success = false,
                    Message = "Error al completar trabajo"
                });
            }
        }

        /// <summary>
        /// Obtener trabajos asignados a un técnico
        /// GET api/Trabajos/mis-trabajos/{tecnicoId}?estadoFiltro=2
        /// </summary>
        [HttpGet("mis-trabajos/{tecnicoId}")]
        [ProducesResponseType(typeof(List<MiTrabajoDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ObtenerMisTrabajos(
            int tecnicoId,
            [FromQuery] int? estadoFiltro = null)
        {
            try
            {
                // Verificar que el técnico existe y está activo
                var tecnico = await _db.Usuarios.FindAsync(tecnicoId);
                if (tecnico == null || !tecnico.Activo || tecnico.RolId != 5) // RolId 5 = Técnico
                {
                    return NotFound(new
                    {
                        Success = false,
                        Message = "Técnico no encontrado o no activo"
                    });
                }

                var query = _db.Set<TrabajoPorOrden>()
                    .Include(t => t.OrdenGeneral)
                        .ThenInclude(o => o.Cliente)
                    .Include(t => t.OrdenGeneral)
                        .ThenInclude(o => o.Vehiculo)
                    .Include(t => t.EstadoTrabajoNavegacion)
                    .Where(t => t.TecnicoAsignadoId == tecnicoId && t.Activo);

                // Aplicar filtro de estado si se proporciona
                if (estadoFiltro.HasValue)
                {
                    query = query.Where(t => t.EstadoTrabajo == estadoFiltro.Value);

                    if (estadoFiltro == 4)
                    {
                        var hoy = DateTime.Today;
                        query = query.Where(t => t.FechaHoraTermino.HasValue &&
                                                 t.FechaHoraTermino.Value.Date == hoy);
                    }
                }

                var trabajos = await query
                    .OrderBy(t => t.EstadoTrabajo)
                    .ThenBy(t => t.FechaCreacion)
                    .Select(t => new MiTrabajoDto
                    {
                        Id = t.Id,
                        OrdenGeneralId = t.OrdenGeneralId,
                        TipoOrden = t.OrdenGeneral.TipoOrdenId,
                        NumeroOrden = t.OrdenGeneral.NumeroOrden,
                        Trabajo = t.Trabajo,

                        // Información del Vehículo
                        VehiculoCompleto = $"{t.OrdenGeneral.Vehiculo.Marca} {t.OrdenGeneral.Vehiculo.Modelo} {t.OrdenGeneral.Vehiculo.Color} / {t.OrdenGeneral.Vehiculo.Anio}",
                        VIN = t.OrdenGeneral.Vehiculo.VIN,
                        Placas = t.OrdenGeneral.Vehiculo.Placas ?? "",

                        // Información del Trabajo
                        FechaHoraAsignacionTecnico = t.FechaHoraAsignacionTecnico,
                        FechaHoraInicio = t.FechaHoraInicio,
                        FechaHoraTermino = t.FechaHoraTermino,
                        IndicacionesTrabajo = t.IndicacionesTrabajo,
                        ComentariosTecnico = t.ComentariosTecnico,

                        // Estado
                        EstadoTrabajo = t.EstadoTrabajo,
                        EstadoTrabajoNombre = t.EstadoTrabajoNavegacion != null ? t.EstadoTrabajoNavegacion.NombreEstado : null,

                        // Fechas de la Orden
                        FechaCreacion = t.FechaCreacion,
                        FechaPromesaEntrega = t.OrdenGeneral.FechaHoraPromesaEntrega
                    })
                    .ToListAsync();

                // Respuesta con información adicional
                var response = new
                {

                    Trabajos = trabajos
                };

                _logger.LogInformation($"Consulta exitosa: Técnico {tecnicoId} - {trabajos.Count} trabajos");

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al obtener trabajos del técnico {tecnicoId}");
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "Error al obtener trabajos"
                });
            }
        }

    }
}