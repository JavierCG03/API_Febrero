using System.ComponentModel.DataAnnotations;

namespace CarSlineAPI.Models.DTOs
{
    public class CrearCitaConTrabajosRequest
    {
        [Required]
        public int TipoOrdenId { get; set; }

        [Required]
        public int ClienteId { get; set; }

        [Required]
        public int VehiculoId { get; set; }


        [Required]
        public DateTime FechaCita { get; set; }

        public int? TipoServicioId { get; set; }

        [Required]
        [MinLength(1, ErrorMessage = "Debe agregar al menos un trabajo")]
        public List<TrabajoCrearDto> Trabajos { get; set; } = new();

    }
    /// <summary>
    /// Request para reagendar una cita existente
    /// </summary>
    public class ReagendarCitaRequest
    {
        [Required(ErrorMessage = "La nueva fecha es requerida")]
        public DateTime NuevaFechaCita { get; set; }
    }

    /// <summary>
    /// Response al reagendar una cita
    /// </summary>
    public class ReagendarCitaResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int CitaId { get; set; }
        public DateTime FechaAnterior { get; set; }
        public DateTime FechaNueva { get; set; }
    }

    /// <summary>
    /// DTO completo de orden con trabajos
    /// </summary>
    public class CitaConTrabajosDto
    {
        public int Id { get; set; }
        public int TipoOrdenId { get; set; }
        public int VehiculoId { get; set; }
        public string VehiculoCompleto{ get; set; } = string.Empty;
        public string VIN { get; set; } = string.Empty;
        public DateTime FechaCita { get; set; }

        // Lista de trabajos
        public List<TrabajoCitaDto> Trabajos { get; set; } = new();
    }

    public class TrabajoCitaDto
    {
        public int Id { get; set; }
        public string Trabajo { get; set; } = string.Empty;
        public string? IndicacionesTrabajo { get; set; }
        public bool RefaccionesListas { get; set; }

    }
    public class RefaccionTrabajoCitaDto
    {
        public int Id { get; set; }
        public int TrabajoCitaId { get; set; }
        public int TrabajoOrdenId { get; set; }
        public string Refaccion { get; set; } = string.Empty;
        public int Cantidad { get; set; }
        public decimal Precio { get; set; }
        public decimal PrecioVenta { get; set; }
        public decimal Total { get; set; }
        public bool Activo { get; set; }
        public DateTime FechaCompra { get; set; }
    }
}
