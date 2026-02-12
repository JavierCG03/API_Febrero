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
}
