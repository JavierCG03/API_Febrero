using System.ComponentModel.DataAnnotations;

namespace CarSlineAPI.Models.DTOs
{
    // DTO para crear nueva refacción
    public class CrearRefaccionRequest
    {
        [Required(ErrorMessage = "El número de parte es requerido")]
        [MaxLength(50)]
        public string NumeroParte { get; set; } = string.Empty;

        [Required(ErrorMessage = "El tipo de refacción es requerido")]
        [MaxLength(100)]
        public string TipoRefaccion { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? MarcaVehiculo { get; set; }

        [MaxLength(10)]
        public string? Ubicacion { get; set; }


        [MaxLength(50)]
        public string? Modelo { get; set; }

        [Range(1900, 2100, ErrorMessage = "Año inválido")]
        public int? Anio { get; set; }

        [Required(ErrorMessage = "La cantidad es requerida")]
        [Range(0, int.MaxValue, ErrorMessage = "La cantidad debe ser mayor o igual a 0")]
        public int Cantidad { get; set; }
    }

    // DTO para actualizar cantidad
    public class ActualizarCantidadRequest
    {
        [Required]
        public int RefaccionId { get; set; }

        [Required]
        public int NuevaCantidad { get; set; }
    }

    // DTO de respuesta
    public class RefaccionDto
    {
        public int Id { get; set; }
        public string NumeroParte { get; set; } = string.Empty;
        public string TipoRefaccion { get; set; } = string.Empty;
        public string? Ubicacion { get; set; }
        public string? MarcaVehiculo { get; set; }
        public string? Modelo { get; set; }
        public int? Anio { get; set; }
        public int Cantidad { get; set; }
        public DateTime FechaRegistro { get; set; }
        public DateTime FechaUltimaModificacion { get; set; }

        // Propiedad calculada para descripción completa
        public string DescripcionCompleta => $"{TipoRefaccion}" +
            (string.IsNullOrEmpty(MarcaVehiculo) ? "" : $" - {MarcaVehiculo}") +
            (string.IsNullOrEmpty(Modelo) ? "" : $" {Modelo}") +
            (Anio.HasValue ? $" {Anio}" : "");
    }

    // DTO de respuesta genérica
    public class RefaccionResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public RefaccionDto? Refaccion { get; set; }
    }
    // ✅ NUEVO DTO para respuesta paginada
    public class RefaccionesPaginadasResponse
    {
        public bool Success { get; set; }
        public List<RefaccionDto> Refacciones { get; set; } = new();
        public int PaginaActual { get; set; }
        public int TotalPaginas { get; set; }
        public int TotalItems { get; set; }
        public int PorPagina { get; set; }
        public bool TienePaginaAnterior { get; set; }
        public bool TienePaginaSiguiente { get; set; }
    }
}