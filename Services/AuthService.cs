using CarSlineAPI.Data;
using CarSlineAPI.Models.Entities;
using Microsoft.EntityFrameworkCore;
using CarSlineAPI.Models.DTOs;

namespace CarSlineAPI.Services
{
    /// <summary>
    /// Interfaz del servicio de autenticación
    /// </summary>
    public interface IAuthService
    {
        Task<AuthResponse> Login(LoginRequest request);
        Task<CrearUsuarioResponse> CrearUsuario(CrearUsuarioRequest request, int adminId);
        Task<List<RolDto>> ObtenerRolesDisponibles();
        Task<List<UsuarioDto>> ObtenerTodosLosUsuarios(int adminId);
        Task<List<UsuarioDto>> ObtenerUsuariosPorRol(int rolId);

    }

    /// <summary>
    /// Servicio de autenticación y gestión de usuarios
    /// </summary>
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AuthService> _logger;

        public AuthService(ApplicationDbContext context, ILogger<AuthService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Autenticar usuario
        /// </summary>
        public async Task<AuthResponse> Login(LoginRequest request)
        {
            try
            {
                // Buscar usuario por nombre de usuario
                var usuario = await _context.Usuarios
                    .Include(u => u.Rol)
                    .FirstOrDefaultAsync(u => u.NombreUsuario == request.NombreUsuario && u.Activo);

                if (usuario == null)
                {
                    _logger.LogWarning($"Intento de login fallido: Usuario '{request.NombreUsuario}' no encontrado");
                    return new AuthResponse
                    {
                        Success = false,
                        Message = "Usuario o contraseña incorrectos"
                    };
                }

                // Verificar contraseña
                bool passwordValida = VerificarPassword(request.Password, usuario.Password);

                if (!passwordValida)
                {
                    _logger.LogWarning($"Intento de login fallido: Contraseña incorrecta para usuario '{request.NombreUsuario}'");
                    return new AuthResponse
                    {
                        Success = false,
                        Message = "Usuario o contraseña incorrectos"
                    };
                }

                // Actualizar último acceso
                usuario.UltimoAcceso = DateTime.Now;
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Login exitoso: Usuario '{usuario.NombreUsuario}' - Rol: '{usuario.Rol?.NombreRol}'");

                // Retornar respuesta exitosa
                return new AuthResponse
                {
                    Success = true,
                    Message = "Login exitoso",
                    Usuario = new UsuarioDto
                    {
                        Id = usuario.Id,
                        NombreCompleto = usuario.NombreCompleto,
                        NombreUsuario = usuario.NombreUsuario,
                        RolId = usuario.RolId,
                        NombreRol = usuario.Rol?.NombreRol ?? "",
                        DescripcionRol = usuario.Rol?.Descripcion,
                        FechaCreacion = usuario.FechaCreacion,
                        UltimoAcceso = usuario.UltimoAcceso
                    },
                    Token = GenerarToken(usuario)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error en Login para usuario '{request.NombreUsuario}'");
                return new AuthResponse
                {
                    Success = false,
                    Message = $"Error en el servidor: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Crear nuevo usuario (solo administrador)
        /// </summary>
        public async Task<CrearUsuarioResponse> CrearUsuario(CrearUsuarioRequest request, int adminId)
        {
            try
            {
                // Verificar que quien crea el usuario es administrador
                var admin = await _context.Usuarios
                    .Include(u => u.Rol)
                    .FirstOrDefaultAsync(u => u.Id == adminId);

                if (admin == null || admin.Rol?.NombreRol != "Administrador")
                {
                    _logger.LogWarning($"Intento de crear usuario sin permisos. ID: {adminId}");
                    return new CrearUsuarioResponse
                    {
                        Success = false,
                        Message = "No tienes permisos para crear usuarios"
                    };
                }

                // Verificar si el nombre de usuario ya existe
                var existeUsuario = await _context.Usuarios
                    .AnyAsync(u => u.NombreUsuario == request.NombreUsuario);

                if (existeUsuario)
                {
                    _logger.LogWarning($"Intento de crear usuario duplicado: '{request.NombreUsuario}'");
                    return new CrearUsuarioResponse
                    {
                        Success = false,
                        Message = "El nombre de usuario ya está en uso"
                    };
                }

                // Verificar que el rol existe y no es administrador
                var rol = await _context.Roles.FindAsync(request.RolId);
                if (rol == null)
                {
                    return new CrearUsuarioResponse
                    {
                        Success = false,
                        Message = "El rol seleccionado no existe"
                    };
                }

                if (rol.NombreRol == "Administrador")
                {
                    _logger.LogWarning($"Intento de crear usuario administrador por usuario ID: {adminId}");
                    return new CrearUsuarioResponse
                    {
                        Success = false,
                        Message = "No se pueden crear usuarios administradores"
                    };
                }

                // Crear nuevo usuario
                var nuevoUsuario = new Usuario
                {
                    NombreCompleto = request.NombreCompleto,
                    NombreUsuario = request.NombreUsuario,
                    Password = HashPassword(request.Password),
                    RolId = request.RolId,
                    FechaCreacion = DateTime.Now,
                    Activo = true,
                    CreadoPorId = adminId
                };

                _context.Usuarios.Add(nuevoUsuario);
                await _context.SaveChangesAsync();

                // Recargar con rol
                await _context.Entry(nuevoUsuario).Reference(u => u.Rol).LoadAsync();

                _logger.LogInformation($"Usuario creado exitosamente: '{nuevoUsuario.NombreUsuario}' por Admin ID: {adminId}");

                return new CrearUsuarioResponse
                {
                    Success = true,
                    Message = "Usuario creado exitosamente",
                    Usuario = new UsuarioDto
                    {
                        Id = nuevoUsuario.Id,
                        NombreCompleto = nuevoUsuario.NombreCompleto,
                        NombreUsuario = nuevoUsuario.NombreUsuario,
                        RolId = nuevoUsuario.RolId,
                        NombreRol = nuevoUsuario.Rol?.NombreRol ?? "",
                        DescripcionRol = nuevoUsuario.Rol?.Descripcion,
                        FechaCreacion = nuevoUsuario.FechaCreacion
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al crear usuario '{request.NombreUsuario}'");
                return new CrearUsuarioResponse
                {
                    Success = false,
                    Message = $"Error al crear usuario: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Obtener roles disponibles (excepto Administrador)
        /// </summary>
        public async Task<List<RolDto>> ObtenerRolesDisponibles()
        {
            try
            {
                var roles = await _context.Roles
                    .Where(r => r.NombreRol != "Administrador")
                    .Select(r => new RolDto
                    {
                        Id = r.Id,
                        NombreRol = r.NombreRol,
                        Descripcion = r.Descripcion
                    })
                    .ToListAsync();

                return roles;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener roles disponibles");
                return new List<RolDto>();
            }
        }

        /// <summary>
        /// Obtener todos los usuarios (solo administrador)
        /// </summary>
        public async Task<List<UsuarioDto>> ObtenerTodosLosUsuarios(int adminId)
        {
            try
            {
                // Verificar que quien consulta es administrador
                var admin = await _context.Usuarios
                    .Include(u => u.Rol)
                    .FirstOrDefaultAsync(u => u.Id == adminId);

                if (admin == null || admin.Rol?.NombreRol != "Administrador")
                {
                    _logger.LogWarning($"Intento de consultar usuarios sin permisos. ID: {adminId}");
                    return new List<UsuarioDto>();
                }

                // Obtener todos los usuarios con sus roles
                var usuarios = await _context.Usuarios
                    .Include(u => u.Rol)
                    .Where(u => u.Activo)
                    .Select(u => new UsuarioDto
                    {
                        Id = u.Id,
                        NombreCompleto = u.NombreCompleto,
                        NombreUsuario = u.NombreUsuario,
                        RolId = u.RolId,
                        NombreRol = u.Rol != null ? u.Rol.NombreRol : "",
                        DescripcionRol = u.Rol != null ? u.Rol.Descripcion : "",
                        FechaCreacion = u.FechaCreacion,
                        UltimoAcceso = u.UltimoAcceso
                    })
                    .OrderByDescending(u => u.FechaCreacion)
                    .ToListAsync();

                return usuarios;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al obtener usuarios por Admin ID: {adminId}");
                return new List<UsuarioDto>();
            }
        }

        public async Task<List<UsuarioDto>> ObtenerUsuariosPorRol(int RolId)
        {
            try
            {
                // Obtener todos los usuarios con sus roles
                var usuarios = await _context.Usuarios
                    .Include(u => u.Rol)
                    .Where(u => u.Activo && u.RolId == RolId)
                    .Select(u => new UsuarioDto
                    {
                        Id = u.Id,
                        NombreCompleto = u.NombreCompleto,
                        NombreUsuario = u.NombreUsuario,
                        RolId = u.RolId,
                        NombreRol = u.Rol != null ? u.Rol.NombreRol : "",
                        DescripcionRol = u.Rol != null ? u.Rol.Descripcion : "",
                        FechaCreacion = u.FechaCreacion,
                        UltimoAcceso = u.UltimoAcceso
                    })
                    .OrderByDescending(u => u.FechaCreacion)
                    .ToListAsync();

                return usuarios;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al obtener usuarios ");
                return new List<UsuarioDto>();
            }
        }

        // ============================================
        // MÉTODOS PRIVADOS DE UTILIDAD
        // ============================================

        private string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        private bool VerificarPassword(string password, string hashedPassword)
        {
            try
            {
                return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
            }
            catch
            {
                return false;
            }
        }

        private string GenerarToken(Usuario usuario)
        {
            // Token simple basado en el ID, rol y timestamp
            // En producción, usar JWT
            return Convert.ToBase64String(
                System.Text.Encoding.UTF8.GetBytes($"{usuario.Id}:{usuario.RolId}:{DateTime.Now.Ticks}")
            );
        }
    }
}