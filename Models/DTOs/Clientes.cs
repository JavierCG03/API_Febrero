using System.ComponentModel.DataAnnotations;

namespace CarSlineAPI.Models.DTOs
{
    // ============================================
    // CLIENTES - DTOs ACTUALIZADOS
    // ============================================

    public class ClienteRequest
    {
        public string NombreCompleto { get; set; } = string.Empty;
        public string RFC { get; set; } = string.Empty;
        public string TelefonoMovil { get; set; } = string.Empty;
        public string? TelefonoCasa { get; set; }
        public string? CorreoElectronico { get; set; }
        public string? Colonia { get; set; }
        public string? Calle { get; set; }
        public string? NumeroExterior { get; set; }
        public string? Municipio { get; set; }
        public string? Estado { get; set; }
        public string? Pais { get; set; }
        public string? CodigoPostal { get; set; }
    }

    public class ClienteResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int? ClienteId { get; set; }
        public ClienteDto? Cliente { get; set; }
    }

    public class ClienteDto
    {
        public int Id { get; set; }
        public string NombreCompleto { get; set; } = string.Empty;
        public string RFC { get; set; } = string.Empty;
        public string TelefonoMovil { get; set; } = string.Empty;
        public string TelefonoCasa { get; set; } = string.Empty;
        public string CorreoElectronico { get; set; } = string.Empty;
        public string Colonia { get; set; } = string.Empty;
        public string Calle { get; set; } = string.Empty;
        public string NumeroExterior { get; set; } = string.Empty;
        public string Municipio { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public string Pais { get; set; } = "México";
        public string CodigoPostal { get; set; } = string.Empty;

        // ✅ NUEVO: Para mostrar en lista de selección
        public string InfoResumen => $"{NombreCompleto} - Tel: {TelefonoMovil} - RFC: {RFC}";
    }

    /// <summary>
    /// NUEVO: Respuesta para búsqueda de múltiples clientes
    /// </summary>
    public class BuscarClientesResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<ClienteDto> Clientes { get; set; } = new();
    }
}