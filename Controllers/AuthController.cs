using CarSlineAPI.Models.DTOs;
using CarSlineAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace CarSlineAPI.Controllers
{
    /// <summary>
    /// Controlador de autenticación y gestión de usuarios
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        /// <summary>
        /// Login de usuario
        /// POST api/Auth/login
        /// </summary>
        /// <param name="request">Credenciales de usuario</param>
        /// <returns>Información del usuario autenticado</returns>
        [HttpPost("login")]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new AuthResponse
                {
                    Success = false,
                    Message = "Datos inválidos"
                });
            }

            _logger.LogInformation($"Intento de login para: {request.NombreUsuario}");

            var response = await _authService.Login(request);

            if (!response.Success)
            {
                return Unauthorized(response);
            }

            return Ok(response);
        }

        /// <summary>
        /// Crear nuevo usuario (Solo Administrador)
        /// POST api/Auth/crear-usuario
        /// </summary>
        /// <param name="request">Datos del nuevo usuario</param>
        /// <param name="adminId">ID del administrador que crea el usuario</param>
        /// <returns>Información del usuario creado</returns>
        [HttpPost("crear-usuario")]
        [ProducesResponseType(typeof(CrearUsuarioResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<CrearUsuarioResponse>> CrearUsuario(
            [FromBody] CrearUsuarioRequest request,
            [FromHeader(Name = "X-Admin-Id")] int adminId)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new CrearUsuarioResponse
                {
                    Success = false,
                    Message = "Datos inválidos"
                });
            }

            if (adminId <= 0)
            {
                return Unauthorized(new CrearUsuarioResponse
                {
                    Success = false,
                    Message = "ID de administrador no válido"
                });
            }

            _logger.LogInformation($"Intento de crear usuario: {request.NombreUsuario} por Admin ID: {adminId}");

            var response = await _authService.CrearUsuario(request, adminId);

            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        /// <summary>
        /// Obtener roles disponibles
        /// GET api/Auth/roles
        /// </summary>
        /// <returns>Lista de roles (excepto Administrador)</returns>
        [HttpGet("roles")]
        [ProducesResponseType(typeof(List<RolDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<RolDto>>> ObtenerRoles()
        {
            _logger.LogInformation("Consultando roles disponibles");
            var roles = await _authService.ObtenerRolesDisponibles();
            return Ok(roles);
        }

        /// <summary>
        /// Obtener todos los usuarios (Solo Administrador)
        /// GET api/Auth/usuarios
        /// </summary>
        /// <param name="adminId">ID del administrador</param>
        /// <returns>Lista de usuarios</returns>
        [HttpGet("usuarios")]
        [ProducesResponseType(typeof(List<UsuarioDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<List<UsuarioDto>>> ObtenerUsuarios(
            [FromHeader(Name = "X-Admin-Id")] int adminId)
        {
            if (adminId <= 0)
            {
                return Unauthorized(new { message = "ID de administrador no válido" });
            }

            _logger.LogInformation($"Consultando usuarios por Admin ID: {adminId}");
            var usuarios = await _authService.ObtenerTodosLosUsuarios(adminId);
            return Ok(usuarios);
        }

        [HttpGet("Tecnicos")]
        [ProducesResponseType(typeof(List<UsuarioDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<UsuarioDto>>> ObtenerTecnicos()
        {
            var usuarios = await _authService.ObtenerUsuariosPorRol(5);
            return Ok(usuarios);
        }

        /// <summary>
        /// Verificar estado de la API
        /// GET api/Auth/health
        /// </summary>
        /// <returns>Estado de la API</returns>
        [HttpGet("health")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult Health()
        {
            return Ok(new
            {
                status = "OK",
                service = "CarSline API",
                timestamp = DateTime.Now,
                message = "API funcionando correctamente"
            });
        }
    }

}