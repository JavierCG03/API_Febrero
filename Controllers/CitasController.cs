// Controllers/CitasController.cs
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

                    _db.Citas.Add(cita);
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

                        _db.TrabajosPorCitas.Add(trabajo);
                    }

                    await _db.SaveChangesAsync();
                    await transaction.CommitAsync();

                    _logger.LogInformation($"Cita ID {cita.Id} creada con {request.Trabajos.Count} trabajos para fecha {cita.FechaCita:dd/MM/yyyy}");

                    var recordatorio = await _db.ProximosServicios.SingleOrDefaultAsync(ps => ps.VehiculoId == request.VehiculoId);

                    if (request.TipoOrdenId == 1 && recordatorio != null)
                    {
                        recordatorio.Activo = false;
                    }
                    await _db.SaveChangesAsync();

                    return Ok(new
                    {
                        Success = true,
                        CitaId = cita.Id,
                        cita.FechaCita,
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

                var citas = await _db.Citas
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
                        VehiculoInfo = $"{c.Vehiculo.Marca} {c.Vehiculo.Modelo} {c.Vehiculo.Anio}",
                        TipoOrden = c.TipoOrden.NombreTipo
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

        [HttpGet("Trabajos-Citas")]
        public async Task<IActionResult> ObtenerTrabajosCitasPorFecha(
        [FromQuery] int tipoOrdenId,           // obligatorio
        [FromQuery] DateTime? fecha = null)   // // opcional, default mañana
        {
            try
            {
                var fechaConsulta = (fecha ?? DateTime.Today.AddDays(1)).Date;
                var fechaSiguiente = fechaConsulta.AddDays(1);

                var citas = await _db.Citas
                    .Include(o => o.Vehiculo)
                    .Include(o => o.Trabajos.Where(t => t.Activo))
                    .Where(o => o.TipoOrdenId == tipoOrdenId 
                             && o.Activo 
                             && o.FechaCita >= fechaConsulta
                             && o.FechaCita < fechaSiguiente)
                    .OrderBy(o => o.FechaCita)
                    .Select(o => new CitaConTrabajosDto
                    {
                        Id = o.Id,
                        TipoOrdenId = o.TipoOrdenId,
                        VehiculoId= o.VehiculoId,
                        VehiculoCompleto = $"{o.Vehiculo.Marca}  {o.Vehiculo.Modelo} {o.Vehiculo.Version} / {o.Vehiculo.Anio}",
                        VIN = o.Vehiculo.VIN,
                        FechaCita = o.FechaCita,
                        Trabajos = o.Trabajos
                            .Where(t => t.Activo)
                            .Select(t => new TrabajoCitaDto
                            {
                                Id = t.Id,
                                Trabajo = t.Trabajo,
                                IndicacionesTrabajo = t.IndicacionesTrabajo,
                                RefaccionesListas =t.RefaccionesListas,
                            }).ToList()
                    })
                    .ToListAsync();

                return Ok(citas);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener órdenes");
                return StatusCode(500, new { Message = "Error al obtener órdenes" });
            }
        }

        [HttpGet("{citaId}")]
        public async Task<IActionResult> ObtenerDetalleCita(int citaId)
        {
            try
            {
                var cita = await _db.Citas
                    .Include(c => c.Cliente)
                    .Include(c => c.Vehiculo)
                    .Include(c => c.TipoServicio)
                    .Include(c => c.TipoOrden)
                    .Where(c => c.Id == citaId && c.Activo)
                    .FirstOrDefaultAsync();

                if (cita == null)
                    return NotFound(new { Success = false, Message = "Cita no encontrada" });

                var trabajos = await _db.TrabajosPorCitas
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
                        cita.TipoOrdenId,
                        cita.TipoServicioId,
                        ClienteId = cita.Cliente.Id,
                        VehiculoId = cita.Vehiculo.Id,
                        NombreCliente = cita.Cliente?.NombreCompleto ?? "No especificado",
                        TelefonoCliente = cita.Cliente?.TelefonoMovil ?? "",
                        DireccionCliente = cita.Cliente != null
                            ? $"{cita.Cliente.Colonia}, Mpio.{cita.Cliente.Municipio}, Edo.{cita.Cliente.Estado}"
                            : "Dirección no disponible",
                        RfcCliente = cita.Cliente?.RFC ?? "",

                        VehiculoCompleto = cita.Vehiculo != null
                            ? $"{cita.Vehiculo.Marca} {cita.Vehiculo.Modelo} {cita.Vehiculo.Anio} - {cita.Vehiculo.Color}"
                            : "Vehículo no especificado",

                        VinVehiculo = cita.Vehiculo?.VIN ?? "",
                        PlacasVehiculo = cita.Vehiculo?.Placas ?? "",

                        TipoOrdenNombre = cita.TipoOrden.NombreTipo ?? "Sin tipo de orden",
                        TipoServicioNombre = cita.TipoServicio?.NombreServicio ?? "Sin especificar",

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
        /// Reagendar una cita existente
        /// PUT api/Citas/reagendar/{citaId}
        /// </summary>
        [HttpPut("reagendar/{citaId}")]
        public async Task<IActionResult> ReagendarCita(
            int citaId,
            [FromBody] ReagendarCitaRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { Success = false, Message = "Datos inválidos" });

            try
            {
                // 1. Validar que la cita existe y está activa
                var cita = await _db.Citas.FindAsync(citaId);

                if (cita == null || !cita.Activo)
                    return NotFound(new { Success = false, Message = "Cita no encontrada" });

                // 2. ✅ VALIDACIÓN: La nueva fecha debe ser en el futuro
                var ahora = DateTime.Now;
                if (request.NuevaFechaCita <= ahora)
                {
                    return BadRequest(new
                    {
                        Success = false,
                        Message = "La nueva fecha debe ser posterior a la fecha y hora actual"
                    });
                }

                // 3. ✅ VALIDACIÓN ADICIONAL: No permitir reagendar para hoy si ya pasó la hora
                if (request.NuevaFechaCita.Date == DateTime.Today &&
                    request.NuevaFechaCita.TimeOfDay <= ahora.TimeOfDay)
                {
                    return BadRequest(new
                    {
                        Success = false,
                        Message = "No se puede reagendar para un horario que ya pasó"
                    });
                }

                // 4. ✅ VALIDACIÓN: Verificar que no haya conflicto de horarios
                var horaInicio = request.NuevaFechaCita;
                var horaFin = request.NuevaFechaCita.AddMinutes(30); // Slots de 30 minutos

                var existeConflicto = await _db.Citas
                    .Where(c => c.Activo
                             && c.Id != citaId // Excluir la cita actual
                             && c.FechaCita >= horaInicio
                             && c.FechaCita < horaFin)
                    .AnyAsync();

                if (existeConflicto)
                {
                    return BadRequest(new
                    {
                        Success = false,
                        Message = "Ya existe una cita programada en ese horario. Por favor, elija otro horario."
                    });
                }

                // 5. Guardar fecha anterior para log y respuesta
                var fechaAnterior = cita.FechaCita;

                // 6. Actualizar la fecha de la cita
                cita.FechaCita = request.NuevaFechaCita;

                await _db.SaveChangesAsync();

                _logger.LogInformation(
                    $"Cita {citaId} reagendada de {fechaAnterior:dd/MM/yyyy HH:mm} a {request.NuevaFechaCita:dd/MM/yyyy HH:mm}");

                return Ok(new
                {
                    Success = true,
                    Message = "Cita reagendada exitosamente",
                    CitaId = cita.Id,
                    FechaAnterior = fechaAnterior,
                    FechaNueva = request.NuevaFechaCita
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al reagendar cita {citaId}");
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "Error al reagendar la cita. Por favor, intente nuevamente."
                });
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
                var cita = await _db.Citas.FindAsync(citaId);

                if (cita == null || !cita.Activo)
                    return NotFound(new { Success = false, Message = "Cita no encontrada" });

                cita.Activo = false;

                var trabajos = await _db.TrabajosPorCitas
                    .Where(t => t.CitaId == citaId && t.Activo)
                    .ToListAsync();

                foreach (var trabajo in trabajos)
                {
                    trabajo.Activo = false; // Cancelado
                }
                var recordatorio = await _db.ProximosServicios.FindAsync(cita.VehiculoId);

                if (cita.TipoOrdenId == 1 && recordatorio != null)
                {
                    recordatorio.Activo = true;
                    recordatorio.PrimerRecordatorio = true;
                    recordatorio.SegundoRecordatorio = true;
                    recordatorio.TercerRecordatorio = false;
                }

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