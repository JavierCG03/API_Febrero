using CarSlineAPI.Data;
using CarSlineAPI.Models.DTOs;
using CarSlineAPI.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarSlineAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RefaccionesController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<RefaccionesController> _logger;

        public RefaccionesController(ApplicationDbContext db, ILogger<RefaccionesController> logger)
        {
            _db = db;
            _logger = logger;
        }

        [HttpGet("todos")]
        public async Task<IActionResult> ObtenerTodas()
        {
            var refacciones = await _db.Refacciones
                .Where(r => r.Activo)
                .OrderByDescending(r => r.FechaUltimaModificacion)
                .Select(r => new RefaccionDto
                {
                    Id = r.Id,
                    NumeroParte = r.NumeroParte,
                    TipoRefaccion = r.TipoRefaccion,
                    MarcaVehiculo = r.MarcaVehiculo,
                    Ubicacion = r.Ubicacion,
                    Modelo = r.Modelo,
                    Anio = r.Anio,
                    Cantidad = r.Cantidad,
                    FechaRegistro = r.FechaRegistro,
                    FechaUltimaModificacion = r.FechaUltimaModificacion
                })
                .ToListAsync();

            return Ok(refacciones);
        }

        /// <summary>
        /// ✅ NUEVO: Obtener refacciones CON PAGINACIÓN Y BÚSQUEDA
        /// GET api/Refacciones/paginado?pagina=1&porPagina=10&busqueda=filtro
        /// </summary>
        [HttpGet("paginado")]
        [ProducesResponseType(typeof(RefaccionesPaginadasResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> ObtenerRefaccionesPaginadas(
            [FromQuery] int pagina = 1,
            [FromQuery] int porPagina = 10,
            [FromQuery] string? busqueda = null)
        {
            try
            {
                // Validar parámetros
                pagina = Math.Max(1, pagina);
                porPagina = Math.Clamp(porPagina, 5, 50); // Entre 5 y 50 items

                var query = _db.Refacciones.Where(r => r.Activo);

                // ✅ Aplicar búsqueda si existe
                if (!string.IsNullOrWhiteSpace(busqueda))
                {
                    var busquedaUpper = busqueda.ToUpper();
                    query = query.Where(r =>
                        r.NumeroParte.ToUpper().Contains(busquedaUpper) ||
                        r.TipoRefaccion.ToUpper().Contains(busquedaUpper) ||
                        (r.MarcaVehiculo != null && r.MarcaVehiculo.ToUpper().Contains(busquedaUpper)) ||
                        (r.Modelo != null && r.Modelo.ToUpper().Contains(busquedaUpper)||
                        (r.Ubicacion != null && r.Ubicacion.ToUpper().Contains(busquedaUpper)))
                    );
                }

                // ✅ Contar total ANTES de paginar
                var totalItems = await query.CountAsync();
                var totalPaginas = (int)Math.Ceiling(totalItems / (double)porPagina);

                // ✅ Obtener solo la página solicitada
                var refacciones = await query
                    .OrderByDescending(r => r.FechaUltimaModificacion) // Más recientes primero
                    .Skip((pagina - 1) * porPagina)
                    .Take(porPagina)
                    .Select(r => new RefaccionDto
                    {
                        Id = r.Id,
                        NumeroParte = r.NumeroParte,
                        TipoRefaccion = r.TipoRefaccion,
                        MarcaVehiculo = r.MarcaVehiculo,
                        Modelo = r.Modelo,
                        Ubicacion = r.Ubicacion,
                        Anio = r.Anio,
                        Cantidad = r.Cantidad,
                        FechaRegistro = r.FechaRegistro,
                        FechaUltimaModificacion = r.FechaUltimaModificacion
                    })
                    .ToListAsync();

                return Ok(new RefaccionesPaginadasResponse
                {
                    Success = true,
                    Refacciones = refacciones,
                    PaginaActual = pagina,
                    TotalPaginas = totalPaginas,
                    TotalItems = totalItems,
                    PorPagina = porPagina,
                    TienePaginaAnterior = pagina > 1,
                    TienePaginaSiguiente = pagina < totalPaginas
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener refacciones paginadas");
                return StatusCode(500, new { Message = "Error al obtener refacciones" });
            }
        }

        /// <summary>
        /// ✅ BÚSQUEDA RÁPIDA - Solo para autocompletado
        /// GET api/Refacciones/buscar-rapido?termino=filtro
        /// </summary>
        [HttpGet("buscar-rapido")]
        [ProducesResponseType(typeof(List<RefaccionDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> BusquedaRapida([FromQuery] string termino)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(termino) || termino.Length < 2)
                {
                    return Ok(new List<RefaccionDto>());
                }

                var terminoUpper = termino.ToUpper();

                var refacciones = await _db.Refacciones
                    .Where(r => r.Activo && (
                        r.NumeroParte.ToUpper().Contains(terminoUpper) ||
                        r.TipoRefaccion.ToUpper().Contains(terminoUpper)
                    ))
                    .OrderBy(r => r.NumeroParte)
                    .Take(15) // Solo 15 resultados máximo
                    .Select(r => new RefaccionDto
                    {
                        Id = r.Id,
                        NumeroParte = r.NumeroParte,
                        TipoRefaccion = r.TipoRefaccion,
                        MarcaVehiculo = r.MarcaVehiculo,
                        Ubicacion = r.Ubicacion,
                        Modelo = r.Modelo,
                        Anio = r.Anio,
                        Cantidad = r.Cantidad,
                        FechaRegistro = r.FechaRegistro,
                        FechaUltimaModificacion = r.FechaUltimaModificacion
                    })
                    .ToListAsync();

                return Ok(refacciones);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en búsqueda rápida");
                return Ok(new List<RefaccionDto>());
            }
        }

        /// <summary>
        /// Buscar refacción por número de parte (EXACTO)
        /// </summary>
        [HttpGet("buscar/{numeroParte}")]
        [ProducesResponseType(typeof(RefaccionDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> BuscarPorNumeroParte(string numeroParte)
        {
            try
            {
                var refaccion = await _db.Refacciones
                    .Where(r => r.NumeroParte == numeroParte.ToUpper() && r.Activo)
                    .Select(r => new RefaccionDto
                    {
                        Id = r.Id,
                        NumeroParte = r.NumeroParte,
                        TipoRefaccion = r.TipoRefaccion,
                        MarcaVehiculo = r.MarcaVehiculo,
                        Ubicacion = r.Ubicacion,
                        Modelo = r.Modelo,
                        Anio = r.Anio,
                        Cantidad = r.Cantidad,
                        FechaRegistro = r.FechaRegistro,
                        FechaUltimaModificacion = r.FechaUltimaModificacion
                    })
                    .FirstOrDefaultAsync();

                if (refaccion == null)
                    return NotFound(new { Message = "Refacción no encontrada" });

                return Ok(refaccion);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al buscar refacción");
                return StatusCode(500, new { Message = "Error al buscar refacción" });
            }
        }

        /// <summary>
        /// Crear nueva refacción
        /// </summary>
        [HttpPost("crear")]
        [ProducesResponseType(typeof(RefaccionResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CrearRefaccion([FromBody] CrearRefaccionRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(new RefaccionResponse
                {
                    Success = false,
                    Message = "Datos inválidos"
                });

            try
            {
                var existe = await _db.Refacciones
                    .AnyAsync(r => r.NumeroParte == request.NumeroParte.ToUpper());

                if (existe)
                    return BadRequest(new RefaccionResponse
                    {
                        Success = false,
                        Message = "Ya existe una refacción con ese número de parte"
                    });

                var refaccion = new Refaccion
                {
                    NumeroParte = request.NumeroParte.ToUpper(),
                    TipoRefaccion = request.TipoRefaccion,
                    MarcaVehiculo = request.MarcaVehiculo,
                    Modelo = request.Modelo,
                    Ubicacion= request.Ubicacion,
                    Anio = request.Anio,
                    Cantidad = request.Cantidad,
                    FechaRegistro = DateTime.Now,
                    FechaUltimaModificacion = DateTime.Now,
                    Activo = true
                };

                _db.Refacciones.Add(refaccion);
                await _db.SaveChangesAsync();

                _logger.LogInformation($"Refacción creada: {refaccion.NumeroParte}");

                return Ok(new RefaccionResponse
                {
                    Success = true,
                    Message = "Refacción creada exitosamente",
                    Refaccion = new RefaccionDto
                    {
                        Id = refaccion.Id,
                        NumeroParte = refaccion.NumeroParte,
                        TipoRefaccion = refaccion.TipoRefaccion,
                        MarcaVehiculo = refaccion.MarcaVehiculo,
                        Ubicacion = refaccion.Ubicacion,
                        Modelo = refaccion.Modelo,
                        Anio = refaccion.Anio,
                        Cantidad = refaccion.Cantidad,
                        FechaRegistro = refaccion.FechaRegistro,
                        FechaUltimaModificacion = refaccion.FechaUltimaModificacion
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear refacción");
                return StatusCode(500, new RefaccionResponse
                {
                    Success = false,
                    Message = "Error al crear refacción"
                });
            }
        }

        /// <summary>
        /// Aumentar cantidad de refacción
        /// </summary>
        [HttpPut("aumentar/{id}/{cantidad}")]
        [ProducesResponseType(typeof(RefaccionResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> AumentarCantidad(int id, int cantidad)
        {
            if (cantidad <= 0)
                return BadRequest(new RefaccionResponse
                {
                    Success = false,
                    Message = "La cantidad debe ser mayor a 0"
                });

            try
            {
                var refaccion = await _db.Refacciones.FindAsync(id);

                if (refaccion == null || !refaccion.Activo)
                    return NotFound(new RefaccionResponse
                    {
                        Success = false,
                        Message = "Refacción no encontrada"
                    });

                refaccion.Cantidad += cantidad;
                refaccion.FechaUltimaModificacion = DateTime.Now;

                await _db.SaveChangesAsync();

                _logger.LogInformation($"Cantidad aumentada: {refaccion.NumeroParte} +{cantidad}");

                return Ok(new RefaccionResponse
                {
                    Success = true,
                    Message = $"Cantidad aumentada: +{cantidad}",
                    Refaccion = new RefaccionDto
                    {
                        Id = refaccion.Id,
                        NumeroParte = refaccion.NumeroParte,
                        TipoRefaccion = refaccion.TipoRefaccion,
                        MarcaVehiculo = refaccion.MarcaVehiculo,
                        Ubicacion= refaccion.Ubicacion,
                        Modelo = refaccion.Modelo,
                        Anio = refaccion.Anio,
                        Cantidad = refaccion.Cantidad,
                        FechaRegistro = refaccion.FechaRegistro,
                        FechaUltimaModificacion = refaccion.FechaUltimaModificacion
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al aumentar cantidad");
                return StatusCode(500, new RefaccionResponse
                {
                    Success = false,
                    Message = "Error al aumentar cantidad"
                });
            }
        }

        /// <summary>
        /// Disminuir cantidad de refacción
        /// </summary>
        [HttpPut("disminuir/{id}/{cantidad}")]
        [ProducesResponseType(typeof(RefaccionResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DisminuirCantidad(int id, int cantidad)
        {
            if (cantidad <= 0)
                return BadRequest(new RefaccionResponse
                {
                    Success = false,
                    Message = "La cantidad debe ser mayor a 0"
                });

            try
            {
                var refaccion = await _db.Refacciones.FindAsync(id);

                if (refaccion == null || !refaccion.Activo)
                    return NotFound(new RefaccionResponse
                    {
                        Success = false,
                        Message = "Refacción no encontrada"
                    });

                if (refaccion.Cantidad < cantidad)
                    return BadRequest(new RefaccionResponse
                    {
                        Success = false,
                        Message = $"Cantidad insuficiente. Disponible: {refaccion.Cantidad}"
                    });

                refaccion.Cantidad -= cantidad;
                refaccion.FechaUltimaModificacion = DateTime.Now;

                await _db.SaveChangesAsync();

                _logger.LogInformation($"Cantidad disminuida: {refaccion.NumeroParte} -{cantidad}");

                return Ok(new RefaccionResponse
                {
                    Success = true,
                    Message = $"Cantidad disminuida: -{cantidad}",
                    Refaccion = new RefaccionDto
                    {
                        Id = refaccion.Id,
                        NumeroParte = refaccion.NumeroParte,
                        TipoRefaccion = refaccion.TipoRefaccion,
                        MarcaVehiculo = refaccion.MarcaVehiculo,
                        Ubicacion = refaccion.Ubicacion,
                        Modelo = refaccion.Modelo,
                        Anio = refaccion.Anio,
                        Cantidad = refaccion.Cantidad,
                        FechaRegistro = refaccion.FechaRegistro,
                        FechaUltimaModificacion = refaccion.FechaUltimaModificacion
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al disminuir cantidad");
                return StatusCode(500, new RefaccionResponse
                {
                    Success = false,
                    Message = "Error al disminuir cantidad"
                });
            }
        }

        /// <summary>
        /// Eliminar refacción (borrado físico)
        /// </summary>
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(RefaccionResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> EliminarRefaccion(int id)
        {
            try
            {
                var refaccion = await _db.Refacciones.FindAsync(id);

                if (refaccion == null)
                    return NotFound(new RefaccionResponse
                    {
                        Success = false,
                        Message = "Refacción no encontrada"
                    });

                _db.Refacciones.Remove(refaccion);
                await _db.SaveChangesAsync();

                _logger.LogInformation($"Refacción eliminada: {refaccion.NumeroParte}");

                return Ok(new RefaccionResponse
                {
                    Success = true,
                    Message = "Refacción eliminada exitosamente"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar refacción");
                return StatusCode(500, new RefaccionResponse
                {
                    Success = false,
                    Message = "Error al eliminar refacción"
                });
            }
        }
    }
}