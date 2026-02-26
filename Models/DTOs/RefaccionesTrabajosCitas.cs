using System.ComponentModel.DataAnnotations;

namespace CarSlineAPI.Models.DTOs
{
    // ============================================
    // REQUEST DTOs
    // ============================================

    /// <summary>
    /// DTO para agregar una refacción individual a un trabajo de cita
    /// </summary>
    public class AgregarRefaccionCitaDto
    {
        [Required(ErrorMessage = "El nombre de la refacción es requerido")]
        [MaxLength(255)]
        public string Refaccion { get; set; } = string.Empty;

        [Required(ErrorMessage = "La cantidad es requerida")]
        [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser mayor a 0")]
        public int Cantidad { get; set; }

        /// <summary>
        /// Precio de costo de la refacción (precio de compra)
        /// </summary>
        [Required(ErrorMessage = "El precio es requerido")]
        [Range(0.01, double.MaxValue, ErrorMessage = "El precio debe ser mayor a 0")]
        public decimal Precio { get; set; }

        /// <summary>
        /// Precio de venta al cliente (opcional al agregar, puede definirse después)
        /// </summary>
        public decimal? PrecioVenta { get; set; }
    }

    /// <summary>
    /// Request para agregar múltiples refacciones a un trabajo de cita
    /// </summary>
    public class AgregarRefaccionesCitaRequest
    {
        [Required(ErrorMessage = "El ID del trabajo de cita es requerido")]
        public int TrabajoCitaId { get; set; }

        [Required(ErrorMessage = "Debe agregar al menos una refacción")]
        [MinLength(1, ErrorMessage = "Debe agregar al menos una refacción")]
        public List<AgregarRefaccionCitaDto> Refacciones { get; set; } = new();
    }

    /// <summary>
    /// Request para actualizar el precio de venta de una refacción de cita
    /// </summary>
    public class ActualizarPrecioVentaRefaccionCitaRequest
    {
        [Required(ErrorMessage = "El precio de venta es requerido")]
        [Range(0.01, double.MaxValue, ErrorMessage = "El precio de venta debe ser mayor a 0")]
        public decimal PrecioVenta { get; set; }
    }

    // ============================================
    // RESPONSE DTOs
    // ============================================

    /// <summary>
    /// DTO de respuesta para una refacción de cita
    /// </summary>
    public class RefaccionPorCitaDto
    {
        public int Id { get; set; }
        public int TrabajoCitaId { get; set; }
        public string Refaccion { get; set; } = string.Empty;
        public int Cantidad { get; set; }

        /// <summary>Precio de costo / compra</summary>
        public decimal Precio { get; set; }

        /// <summary>Precio de venta al cliente (puede ser null si aún no se define)</summary>
        public decimal? PrecioVenta { get; set; }

        /// <summary>Total calculado con precio de costo</summary>
        public decimal TotalCosto => Cantidad * Precio;

        /// <summary>Total calculado con precio de venta (si está definido)</summary>
        public decimal? TotalVenta => PrecioVenta.HasValue ? Cantidad * PrecioVenta.Value : null;

        public DateTime FechaCompra { get; set; }

        /// <summary>
        /// false = pendiente de transferir a orden, true = ya fue pasada a refaccionestrabajo
        /// </summary>
        public bool Transferida { get; set; }

        /// <summary>ID del trabajo de orden al que fue transferida (si aplica)</summary>
        public int? TrabajoOrdenId { get; set; }
    }

    /// <summary>
    /// Respuesta al agregar refacciones a un trabajo de cita
    /// </summary>
    public class AgregarRefaccionesCitaResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<RefaccionPorCitaDto> RefaccionesAgregadas { get; set; } = new();
        public int CantidadRefacciones { get; set; }
        public decimal TotalCosto { get; set; }
    }

    /// <summary>
    /// Respuesta al eliminar una refacción de cita
    /// </summary>
    public class EliminarRefaccionCitaResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// Respuesta al obtener refacciones de un trabajo de cita
    /// </summary>
    public class ObtenerRefaccionesCitaResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int TrabajoCitaId { get; set; }
        public string TrabajoCitaNombre { get; set; } = string.Empty;
        public List<RefaccionPorCitaDto> Refacciones { get; set; } = new();
        public decimal TotalCosto { get; set; }
        public decimal? TotalVenta { get; set; }
        public bool RefaccionesListas { get; set; }
    }
}