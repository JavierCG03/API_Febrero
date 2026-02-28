using System.ComponentModel.DataAnnotations;

namespace CarSlineAPI.Models.DTOs
{

    public class AgregarRefaccionCitaDto
    {
        [Required(ErrorMessage = "El nombre de la refacción es requerido")]
        [MaxLength(255)]
        public string Refaccion { get; set; } = string.Empty;

        [Required(ErrorMessage = "La cantidad es requerida")]
        [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser mayor a 0")]
        public int Cantidad { get; set; }

        // Renombrado de Precio → PrecioCompra
        [Required(ErrorMessage = "El precio de compra es requerido")]
        [Range(0.01, double.MaxValue, ErrorMessage = "El precio debe ser mayor a 0")]
        public decimal Precio { get; set; }

        public decimal? PrecioVenta { get; set; }
    }

    public class AgregarRefaccionesRequest
    {
        [Required(ErrorMessage = "El ID del trabajo es requerido")]
        public int TrabajoId { get; set; }

        public bool Orden { get; set; } = false;

        [Required]
        [MinLength(1, ErrorMessage = "Debe agregar al menos una refacción")]
        public List<AgregarRefaccionCitaDto> Refacciones { get; set; } = new();
    }

    // Request SIN CITA - TrabajoOrdenId requerido
    public class AgregarRefaccionesSinCitaRequest
    {
        [Required(ErrorMessage = "El ID del trabajo de orden es requerido")]
        public int TrabajoOrdenId { get; set; }

        [Required]
        [MinLength(1, ErrorMessage = "Debe agregar al menos una refacción")]
        public List<AgregarRefaccionCitaDto> Refacciones { get; set; } = new();
    }

    public class ActualizarPrecioVentaRefaccionCitaRequest
    {
        [Required(ErrorMessage = "El precio de venta es requerido")]
        [Range(0.01, double.MaxValue, ErrorMessage = "El precio de venta debe ser mayor a 0")]
        public decimal PrecioVenta { get; set; }
    }

    // ============================================
    // RESPONSE DTOs
    // ============================================

    public class RefaccionCompradaDto
    {
        public int Id { get; set; }

        // Nullable: null cuando viene de orden sin cita
        public int? TrabajoCitaId { get; set; }

        // Nullable: null cuando la cita aún no se convirtió a orden
        public int? TrabajoOrdenId { get; set; }

        public string Refaccion { get; set; } = string.Empty;
        public int Cantidad { get; set; }

        // Renombrado de Precio
        public decimal Precio { get; set; }
        public decimal? PrecioVenta { get; set; }

        public decimal TotalCosto => Cantidad * Precio;
        public decimal? TotalVenta => PrecioVenta.HasValue ? Cantidad * PrecioVenta.Value : null;

        public DateTime FechaCompra { get; set; }

        // true = ya vinculada a una orden de trabajo
        public bool Transferida { get; set; }
    }

    public class AgregarRefaccionesCitaResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<RefaccionCompradaDto> RefaccionesAgregadas { get; set; } = new();
        public int CantidadRefacciones { get; set; }
        public decimal TotalCosto { get; set; }
    }

    public class EliminarRefaccionCitaResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class ObtenerRefaccionesCitaResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int TrabajoId { get; set; }
        public string TrabajoNombre { get; set; } = string.Empty;     
        public decimal TotalCosto { get; set; }
        public decimal? TotalVenta { get; set; }
        public bool RefaccionesListas { get; set; }
        public List<RefaccionCompradaDto> Refacciones { get; set; } = new();
    }

    // Nuevo response para consultar refacciones de una orden sin cita
    public class ObtenerRefaccionesOrdenResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int TrabajoOrdenId { get; set; }
        public string TrabajoOrdenNombre { get; set; } = string.Empty;
        public List<RefaccionCompradaDto> Refacciones { get; set; } = new();
        public decimal TotalCosto { get; set; }
        public decimal? TotalVenta { get; set; }
    }
}