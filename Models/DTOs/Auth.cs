using System.ComponentModel.DataAnnotations;

namespace CarSlineAPI.Models.DTOs
{
    /// DTO para solicitud de login
    public class LoginRequest
    {
        [Required(ErrorMessage = "El nombre de usuario es requerido")]
        public string NombreUsuario { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es requerida")]
        [MinLength(6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres")]
        public string Password { get; set; } = string.Empty;
    }

    /// DTO para crear usuario (solo Admin)
    public class CrearUsuarioRequest
    {
        [Required(ErrorMessage = "El nombre completo es requerido")]
        [MaxLength(150)]
        public string NombreCompleto { get; set; } = string.Empty;

        [Required(ErrorMessage = "El nombre de usuario es requerido")]
        [MaxLength(50)]
        public string NombreUsuario { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es requerida")]
        [MinLength(6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "El rol es requerido")]
        [Range(2, 10, ErrorMessage = "El rol debe ser entre 2 y 10 (no puede crear administradores)")]
        public int RolId { get; set; }
    }

    /// DTO para respuesta de autenticación
    public class AuthResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public UsuarioDto? Usuario { get; set; }
        public string? Token { get; set; }
    }


    /// DTO del usuario (sin información sensible)
    public class UsuarioDto
    {
        public int Id { get; set; }
        public string NombreCompleto { get; set; } = string.Empty;
        public string NombreUsuario { get; set; } = string.Empty;
        public int RolId { get; set; }
        public string NombreRol { get; set; } = string.Empty;
        public string? DescripcionRol { get; set; }
        public DateTime FechaCreacion { get; set; }
        public DateTime? UltimoAcceso { get; set; }
    }


    /// DTO para respuesta de crear usuario
    public class CrearUsuarioResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public UsuarioDto? Usuario { get; set; }
    }


    /// DTO para información de rol
    public class RolDto
    {
        public int Id { get; set; }
        public string NombreRol { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
    }


}
