using System.ComponentModel.DataAnnotations;

namespace CarSlineAPI.Models.DTOs
{
    /// <summary>
    /// DTO para agregar una refacción individual al trabajo
    /// </summary>
    public class AgregarRefaccionDto
    {
        [Required(ErrorMessage = "El nombre de la refacción es requerido")]
        [MaxLength(255)]
        public string Refaccion { get; set; } = string.Empty;

        [Required(ErrorMessage = "La cantidad es requerida")]
        [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser mayor a 0")]
        public int Cantidad { get; set; }

        [Required(ErrorMessage = "El precio unitario es requerido")]
        [Range(0.01, double.MaxValue, ErrorMessage = "El precio debe ser mayor a 0")]
        public decimal PrecioUnitario { get; set; }
    }

    /// <summary>
    /// Request para agregar múltiples refacciones a un trabajo
    /// </summary>
    public class AgregarRefaccionesTrabajoRequest
    {
        [Required(ErrorMessage = "El ID del trabajo es requerido")]
        public int TrabajoId { get; set; }

        [Required(ErrorMessage = "Debe agregar al menos una refacción")]
        [MinLength(1, ErrorMessage = "Debe agregar al menos una refacción")]
        public List<AgregarRefaccionDto> Refacciones { get; set; } = new();
    }

    /// <summary>
    /// DTO de respuesta para refacción agregada
    /// </summary>
    public class RefaccionTrabajoDto
    {
        public int Id { get; set; }
        public int TrabajoId { get; set; }
        public int OrdenGeneralId { get; set; }
        public string Refaccion { get; set; } = string.Empty;
        public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal Total { get; set; }
    }

    /// <summary>
    /// Respuesta del endpoint de refacciones
    /// </summary>
    public class AgregarRefaccionesResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<RefaccionTrabajoDto> RefaccionesAgregadas { get; set; } = new();
        public decimal TotalRefacciones { get; set; }
        public int CantidadRefacciones { get; set; }
    }

    /// <summary>
    /// Respuesta para obtener refacciones de un trabajo
    /// </summary>
    public class ObtenerRefaccionesTrabajoResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int TrabajoId { get; set; }
        public string NumeroOrden { get; set; } = string.Empty;
        public List<RefaccionTrabajoDto> Refacciones { get; set; } = new();
        public decimal TotalRefacciones { get; set; }
    }
}