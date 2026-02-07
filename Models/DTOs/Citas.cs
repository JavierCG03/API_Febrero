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
}
