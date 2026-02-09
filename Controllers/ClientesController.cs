using CarSlineAPI.Data;
using CarSlineAPI.Models.DTOs;
using CarSlineAPI.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarSlineAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ClientesController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<ClientesController> _logger;

        public ClientesController(ApplicationDbContext db, ILogger<ClientesController> logger)
        {
            _db = db;
            _logger = logger;
        }

        [HttpPost("crear")]
        [ProducesResponseType(typeof(ClienteResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CrearCliente([FromBody] ClienteRequest req)
        {
            if (!ModelState.IsValid) return BadRequest(new { Success = false, Message = "Datos inválidos" });

            var cliente = new Cliente
            {
                NombreCompleto = req.NombreCompleto,
                RFC = req.RFC,
                TelefonoMovil = req.TelefonoMovil,
                TelefonoCasa = req.TelefonoCasa,
                CorreoElectronico = req.CorreoElectronico,
                Colonia = req.Colonia,
                Calle = req.Calle,
                NumeroExterior = req.NumeroExterior,
                Municipio = req.Municipio,
                Estado = req.Estado,
                Pais = string.IsNullOrWhiteSpace(req.Pais) ? "México" : req.Pais,
                CodigoPostal = req.CodigoPostal,
                Activo = true
            };

            try
            {
                _db.Clientes.Add(cliente);
                await _db.SaveChangesAsync();
                return Ok(new { Success = true, ClienteId = cliente.Id });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error al crear cliente");
                return StatusCode(500, new { Success = false, Message = "Error al registrar cliente" });
            }
        }

        [HttpPut("actualizar/{id}")]
        [ProducesResponseType(typeof(ClienteResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ActualizarCliente(int id, [FromBody] ClienteRequest req)
        {
            if (!ModelState.IsValid)
                return BadRequest(new ClienteResponse
                {
                    Success = false,
                    Message = "Datos inválidos"
                });

            try
            {
                var cliente = await _db.Clientes.FindAsync(id);

                if (cliente == null || !cliente.Activo)
                {
                    _logger.LogWarning($"Cliente con ID {id} no encontrado");
                    return NotFound(new ClienteResponse
                    {
                        Success = false,
                        Message = "Cliente no encontrado"
                    });
                }

                cliente.NombreCompleto = req.NombreCompleto;
                cliente.RFC = req.RFC;
                cliente.TelefonoMovil = req.TelefonoMovil;
                cliente.TelefonoCasa = req.TelefonoCasa;
                cliente.CorreoElectronico = req.CorreoElectronico;
                cliente.Colonia = req.Colonia;
                cliente.Calle = req.Calle;
                cliente.NumeroExterior = req.NumeroExterior;
                cliente.Municipio = req.Municipio;
                cliente.Estado = req.Estado;
                cliente.Pais = string.IsNullOrWhiteSpace(req.Pais) ? "México" : req.Pais;
                cliente.CodigoPostal = req.CodigoPostal;

                await _db.SaveChangesAsync();

                _logger.LogInformation($"Cliente ID {id} actualizado exitosamente");

                return Ok(new ClienteResponse
                {
                    Success = true,
                    Message = "Cliente actualizado exitosamente",
                    ClienteId = cliente.Id
                });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, $"Error al actualizar cliente ID {id}");
                return StatusCode(500, new ClienteResponse
                {
                    Success = false,
                    Message = "Error al actualizar cliente"
                });
            }
        }

        /// <summary>
        /// NUEVO: Buscar clientes por nombre (retorna lista si hay múltiples coincidencias)
        /// </summary>
        [HttpGet("buscar-nombre/{nombre}")]
        [ProducesResponseType(typeof(BuscarClientesResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> BuscarPorNombre(string nombre)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(nombre) || nombre.Length < 3)
                {
                    return Ok(new BuscarClientesResponse
                    {
                        Success = false,
                        Message = "Ingresa al menos 3 caracteres para buscar",
                        Clientes = new List<ClienteDto>()
                    });
                }

                // Búsqueda parcial (LIKE)
                var clientes = await _db.Clientes
                    .Where(c => c.Activo && c.NombreCompleto.Contains(nombre))
                    .OrderBy(c => c.NombreCompleto)
                    .Take(20) // Limitar a 20 resultados
                    .Select(c => new ClienteDto
                    {
                        Id = c.Id,
                        NombreCompleto = c.NombreCompleto,
                        RFC = c.RFC,
                        TelefonoMovil = c.TelefonoMovil,
                        TelefonoCasa = c.TelefonoCasa,
                        CorreoElectronico = c.CorreoElectronico,
                        Colonia = c.Colonia,
                        Calle = c.Calle,
                        NumeroExterior = c.NumeroExterior,
                        Municipio = c.Municipio,
                        Estado = c.Estado,
                        Pais = c.Pais,
                        CodigoPostal = c.CodigoPostal
                    })
                    .ToListAsync();

                if (!clientes.Any())
                {
                    return Ok(new BuscarClientesResponse
                    {
                        Success = false,
                        Message = $"No se encontraron clientes con el nombre '{nombre}'",
                        Clientes = new List<ClienteDto>()
                    });
                }

                return Ok(new BuscarClientesResponse
                {
                    Success = true,
                    Message = $"Se encontraron {clientes.Count} cliente(s)",
                    Clientes = clientes
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al buscar clientes por nombre: {nombre}");
                return StatusCode(500, new BuscarClientesResponse
                {
                    Success = false,
                    Message = "Error al buscar clientes",
                    Clientes = new List<ClienteDto>()
                });
            }
        }

        /// <summary>
        /// MANTENER: Buscar por teléfono (búsqueda exacta, retorna un solo cliente)
        /// </summary>
        [HttpGet("buscar-telefono/{telefono}")]
        public async Task<IActionResult> BuscarPorTelefono(string telefono)
        {
            var cliente = await _db.Clientes
                .Where(c => c.TelefonoMovil == telefono && c.Activo)
                .FirstOrDefaultAsync();

            if (cliente == null)
                return Ok(new { Success = false, Message = "Cliente no encontrado" });

            return Ok(new
            {
                Success = true,
                Cliente = new
                {
                    cliente.Id,
                    cliente.RFC,
                    cliente.NombreCompleto,
                    cliente.TelefonoMovil,
                    cliente.TelefonoCasa,
                    cliente.CorreoElectronico,
                    cliente.Colonia,
                    cliente.Calle,
                    cliente.NumeroExterior,
                    cliente.Municipio,
                    cliente.Estado,
                    cliente.Pais,
                    cliente.CodigoPostal
                }
            });
        }

        /// <summary>
        /// NUEVO: Obtener cliente por ID (para cargar datos después de seleccionar de la lista)
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ClienteResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ObtenerClientePorId(int id)
        {
            try
            {
                var cliente = await _db.Clientes
                    .Where(c => c.Id == id && c.Activo)
                    .FirstOrDefaultAsync();

                if (cliente == null)
                {
                    return NotFound(new ClienteResponse
                    {
                        Success = false,
                        Message = "Cliente no encontrado"
                    });
                }

                return Ok(new ClienteResponse
                {
                    Success = true,
                    Message = "Cliente encontrado",
                    ClienteId = cliente.Id,
                    Cliente = new ClienteDto
                    {
                        Id = cliente.Id,
                        NombreCompleto = cliente.NombreCompleto,
                        RFC = cliente.RFC,
                        TelefonoMovil = cliente.TelefonoMovil,
                        TelefonoCasa = cliente.TelefonoCasa ?? "",
                        CorreoElectronico = cliente.CorreoElectronico ?? "",
                        Colonia = cliente.Colonia ?? "",
                        Calle = cliente.Calle ?? "",
                        NumeroExterior = cliente.NumeroExterior ?? "",
                        Municipio = cliente.Municipio ?? "",
                        Estado = cliente.Estado ?? "",
                        Pais = cliente.Pais ?? "México",
                        CodigoPostal = cliente.CodigoPostal ?? ""
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al obtener cliente ID {id}");
                return StatusCode(500, new ClienteResponse
                {
                    Success = false,
                    Message = "Error al obtener cliente"
                });
            }
        }
    }
}