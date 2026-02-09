using CarSlineAPI.Data;
using CarSlineAPI.Models.DTOs;
using CarSlineAPI.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarSlineAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VehiculosController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<VehiculosController> _logger;

        public VehiculosController(ApplicationDbContext db, ILogger<VehiculosController> logger)
        {
            _db = db;
            _logger = logger;
        }
        [HttpPost("crear")]
        public async Task<IActionResult> CrearVehiculo([FromBody] VehiculoRequest req)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { Success = false, Message = "Datos inválidos" });

            var vin = req.VIN?.ToUpperInvariant();

            // ✅ Verificación previa
            var existeVin = await _db.Vehiculos.AnyAsync(v => v.VIN == vin);
            if (existeVin)
                return BadRequest(new { Success = false, Message = "El VIN ya está registrado, usa el buscador" });

            var veh = new Vehiculo
            {
                ClienteId = req.ClienteId,
                VIN = vin ?? string.Empty,
                Marca = req.Marca,
                Modelo = req.Modelo,
                Version = req.Version,
                Anio = req.Anio,
                Color = req.Color,
                Placas = req.Placas?.ToUpperInvariant(),
                KilometrajeInicial = req.KilometrajeInicial,
                Activo = true
            };

            try
            {
                _db.Vehiculos.Add(veh);
                await _db.SaveChangesAsync();
                return Ok(new { Success = true, VehiculoId = veh.Id });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error al crear vehículo");
                return StatusCode(500, new { Success = false, Message = "Error al registrar vehículo" });
            }
        }
        [HttpPut("actualizar-placas/{id}")]
        [ProducesResponseType(typeof(VehiculoResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ActualizarPlacasVehiculo(int id, [FromBody] VehiculoRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Placas))
            {
                return BadRequest(new VehiculoResponse
                {
                    Success = false,
                    Message = "Las placas son requeridas"
                });
            }

            try
            {
                var vehiculo = await _db.Vehiculos.FindAsync(id);

                if (vehiculo == null || !vehiculo.Activo)
                {
                    _logger.LogWarning($"Vehículo con ID {id} no encontrado");
                    return NotFound(new VehiculoResponse
                    {
                        Success = false,
                        Message = "Vehículo no encontrado"
                    });
                }

                vehiculo.Placas = req.Placas.ToUpperInvariant();
                await _db.SaveChangesAsync();

                _logger.LogInformation($"Placas del vehículo ID {id} actualizadas a: {vehiculo.Placas}");

                return Ok(new VehiculoResponse
                {
                    Success = true,
                    Message = "Placas actualizadas exitosamente",
                    VehiculoId = vehiculo.Id
                });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, $"Error al actualizar placas del vehículo ID {id}");
                return StatusCode(500, new VehiculoResponse
                {
                    Success = false,
                    Message = "Error al actualizar placas"
                });
            }
        }

        /// <summary>
        /// MANTENER: Buscar por VIN completo (17 caracteres)
        /// </summary>
        [HttpGet("buscar-vin/{vin}")]
        public async Task<IActionResult> BuscarPorVIN(string vin)
        {
            var veh = await _db.Vehiculos
                .Include(v => v.Cliente)
                .Where(v => v.VIN == vin.ToUpperInvariant() && v.Activo)
                .FirstOrDefaultAsync();

            if (veh == null)
                return Ok(new { Success = false, Message = "Vehículo no encontrado" });

            return Ok(new
            {
                Success = true,
                Vehiculo = new
                {
                    veh.Id,
                    veh.ClienteId,
                    veh.VIN,
                    veh.Marca,
                    veh.Version,
                    veh.Modelo,
                    veh.Anio,
                    veh.Color,
                    veh.Placas,
                    veh.KilometrajeInicial,
                    NombreCliente = veh.Cliente?.NombreCompleto ?? ""
                }
            });
        }

        [HttpGet("buscarClienteId/{ClienteId}")]
        [ProducesResponseType(typeof(BuscarVehiculosResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> BuscarVehiculosPorClienteId(int ClienteId)
        {
            try
            {
                if (ClienteId<0)
                {
                    return Ok(new BuscarVehiculosResponse
                    {
                        Success = false,
                        Message = "Debes ingresar exactamente 4 caracteres",
                        Vehiculos = new List<VehiculoDto>()
                    });
                }
                // Buscar vehículos que le pertenecen al cliente
                var vehiculos = await _db.Vehiculos
                    .Include(v => v.Cliente)
                    .Where(v => v.Activo && v.ClienteId == ClienteId)
                    .OrderBy(v => v.Marca)
                    .ThenBy(v => v.Modelo)
                    .Take(10) // Limitar resultados
                    .Select(v => new VehiculoDto
                    {
                        Id = v.Id,
                        ClienteId = v.ClienteId,
                        VIN = v.VIN,
                        Marca = v.Marca ?? "",
                        Modelo = v.Modelo ?? "",
                        Version = v.Version ?? "",
                        Anio = v.Anio ?? 0,
                        Color = v.Color ?? "",
                        Placas = v.Placas ?? "",
                        KilometrajeInicial = v.KilometrajeInicial,
                        NombreCliente = v.Cliente != null ? v.Cliente.NombreCompleto : ""
                    })
                    .ToListAsync();

                if (!vehiculos.Any())
                {
                    return Ok(new BuscarVehiculosResponse
                    {
                        Success = false,
                        Message = "No se encontraron vehículos asignados a este Cliente ",
                        Vehiculos = new List<VehiculoDto>()
                    });
                }

                return Ok(new BuscarVehiculosResponse
                {
                    Success = true,
                    Message = $"Se encontraron {vehiculos.Count} vehículo(s)",
                    Vehiculos = vehiculos
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al buscar vehiculos por ese cliente");
                return StatusCode(500, new BuscarVehiculosResponse
                {
                    Success = false,
                    Message = "Error al buscar vehículos",
                    Vehiculos = new List<VehiculoDto>()
                });
            }
        }

        /// <summary>
        /// NUEVO: Buscar vehículos por los últimos 4 dígitos del VIN
        /// Retorna lista si hay múltiples coincidencias
        /// </summary>
        [HttpGet("buscar-vin-ultimos/{ultimos4}")]
        [ProducesResponseType(typeof(BuscarVehiculosResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> BuscarPorUltimos4VIN(string ultimos4)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(ultimos4) || ultimos4.Length != 4)
                {
                    return Ok(new BuscarVehiculosResponse
                    {
                        Success = false,
                        Message = "Debes ingresar exactamente 4 caracteres",
                        Vehiculos = new List<VehiculoDto>()
                    });
                }

                string ultimos4Upper = ultimos4.ToUpperInvariant();

                // Buscar vehículos cuyo VIN termine en los 4 caracteres especificados
                var vehiculos = await _db.Vehiculos
                    .Include(v => v.Cliente)
                    .Where(v => v.Activo && v.VIN.EndsWith(ultimos4Upper))
                    .OrderBy(v => v.Marca)
                    .ThenBy(v => v.Modelo)
                    .Take(10) // Limitar resultados
                    .Select(v => new VehiculoDto
                    {
                        Id = v.Id,
                        ClienteId = v.ClienteId,
                        VIN = v.VIN,
                        Marca = v.Marca ?? "",
                        Modelo = v.Modelo ?? "",
                        Version = v.Version ?? "",
                        Anio = v.Anio ?? 0,
                        Color = v.Color ?? "",
                        Placas = v.Placas ?? "",
                        KilometrajeInicial = v.KilometrajeInicial,
                        NombreCliente = v.Cliente != null ? v.Cliente.NombreCompleto : ""
                    })
                    .ToListAsync();

                if (!vehiculos.Any())
                {
                    return Ok(new BuscarVehiculosResponse
                    {
                        Success = false,
                        Message = $"No se encontraron vehículos con VIN terminado en '{ultimos4Upper}'",
                        Vehiculos = new List<VehiculoDto>()
                    });
                }

                return Ok(new BuscarVehiculosResponse
                {
                    Success = true,
                    Message = $"Se encontraron {vehiculos.Count} vehículo(s)",
                    Vehiculos = vehiculos
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al buscar vehículos por últimos 4 VIN: {ultimos4}");
                return StatusCode(500, new BuscarVehiculosResponse
                {
                    Success = false,
                    Message = "Error al buscar vehículos",
                    Vehiculos = new List<VehiculoDto>()
                });
            }
        }

        /// <summary>
        /// NUEVO: Obtener vehículo por ID (para cargar datos después de seleccionar de la lista)
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(VehiculoResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ObtenerVehiculoPorId(int id)
        {
            try
            {
                var vehiculo = await _db.Vehiculos
                    .Include(v => v.Cliente)
                    .Where(v => v.Id == id && v.Activo)
                    .FirstOrDefaultAsync();

                if (vehiculo == null)
                {
                    return NotFound(new VehiculoResponse
                    {
                        Success = false,
                        Message = "Vehículo no encontrado"
                    });
                }

                return Ok(new VehiculoResponse
                {
                    Success = true,
                    Message = "Vehículo encontrado",
                    VehiculoId = vehiculo.Id,
                    Vehiculo = new VehiculoDto
                    {
                        Id = vehiculo.Id,
                        ClienteId = vehiculo.ClienteId,
                        VIN = vehiculo.VIN,
                        Marca = vehiculo.Marca ?? "",
                        Modelo = vehiculo.Modelo ?? "",
                        Version = vehiculo.Version ?? "",
                        Anio = vehiculo.Anio ?? 0,
                        Color = vehiculo.Color ?? "",
                        Placas = vehiculo.Placas ?? "",
                        KilometrajeInicial = vehiculo.KilometrajeInicial,
                        NombreCliente = vehiculo.Cliente?.NombreCompleto ?? ""
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al obtener vehículo ID {id}");
                return StatusCode(500, new VehiculoResponse
                {
                    Success = false,
                    Message = "Error al obtener vehículo"
                });
            }
        }
    }
}