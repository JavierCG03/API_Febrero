using System.ComponentModel.DataAnnotations;

namespace CarSlineAPI.Models.DTOs
{
    /// <summary>
    /// DTO para crear un trabajo en una orden
    /// </summary>
    public class CrearTrabajoRequest
    {
        [Required(ErrorMessage = "La descripción del trabajo es requerida")]
        [MaxLength(1000)]
        public string Trabajo { get; set; } = string.Empty;

        public int? TecnicoAsignadoId { get; set; }
    }

    /// <summary>
    /// DTO para actualizar estado de trabajo
    /// </summary>
    public class ActualizarEstadoTrabajoRequest
    {
        [Required]
        public int TrabajoId { get; set; }

        [Required]
        [Range(1, 5)]
        public int NuevoEstado { get; set; }

        public string? Comentarios { get; set; }
    }

    /// <summary>
    /// DTO para asignar técnico a trabajo
    /// </summary>
    public class AsignarTecnicoTrabajoRequest
    {
        [Required]
        public int TrabajoId { get; set; }

        [Required]
        public int TecnicoId { get; set; }
    }

    /// <summary>
    /// DTO de trabajo individual
    /// </summary>
    public class TrabajoDto
    {
        public int Id { get; set; }
        public int OrdenGeneralId { get; set; }
        public string Trabajo { get; set; } = string.Empty;
        public int? TecnicoAsignadoId { get; set; }
        public string? TecnicoNombre { get; set; }
        public DateTime? FechaHoraAsignacionTecnico { get; set; }
        public DateTime? FechaHoraInicio { get; set; }
        public DateTime? FechaHoraTermino { get; set; }
        public string? IndicacionesTrabajo { get; set; }
        public string? ComentariosTecnico { get; set; }
        public string? ComentariosJefeTaller { get; set; }
        public int EstadoTrabajo { get; set; }
        public string? EstadoTrabajoNombre { get; set; }
        public string? ColorEstado { get; set; }
        public DateTime FechaCreacion { get; set; }

        // Propiedades calculadas
        public bool EsPendiente => EstadoTrabajo == 1;
        public bool EnProceso => EstadoTrabajo == 2;
        public bool EstaCompletado => EstadoTrabajo == 3;
        public bool EstaPausado => EstadoTrabajo == 4;
        public bool EstaCancelado => EstadoTrabajo == 5;

        public string DuracionFormateada
        {
            get
            {
                if (!FechaHoraInicio.HasValue || !FechaHoraTermino.HasValue)
                    return "-";

                var duracion = FechaHoraTermino.Value - FechaHoraInicio.Value;
                return $"{duracion.Hours}h {duracion.Minutes}m";
            }
        }

    }
    public class FijarManoObraRequest
    {
        [Required(ErrorMessage = "El costo de mano de obra es requerido")]
        [Range(0, double.MaxValue, ErrorMessage = "El costo debe ser mayor o igual a 0")]
        public decimal CostoManoObra { get; set; }
    }
    public class ManoObraResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public decimal CostoManoObra { get; set; }
    }
    /// <summary>
    /// DTO completo de orden con trabajos
    /// </summary>
    public class OrdenConTrabajosDto
    {
        public int Id { get; set; }
        public string NumeroOrden { get; set; } = string.Empty;
        public int TipoOrdenId { get; set; }
        public string TipoOrden { get; set; } = string.Empty;
        public int ClienteId { get; set; }
        public string ClienteNombre { get; set; } = string.Empty;
        public string ClienteTelefono { get; set; } = string.Empty;
        public string TipoServicio { get; set; } = string.Empty;
        public int VehiculoId { get; set; }
        public string VehiculoCompleto { get; set; } = string.Empty;
        public string VIN { get; set; } = string.Empty;
        public string Placas { get; set; } = string.Empty;
        public string AsesorNombre { get; set; } = string.Empty;
        public int KilometrajeActual { get; set; }
        public DateTime FechaCreacion { get; set; }
        public DateTime FechaHoraPromesaEntrega { get; set; }
        public DateTime? HoraFin { get; set; }
        public int EstadoOrdenId { get; set; }
        public string EstadoOrden { get; set; } = string.Empty;
        public decimal? CostoTotal { get; set; }
        public int TotalTrabajos { get; set; }
        public int TrabajosCompletados { get; set; }
        public decimal ProgresoGeneral { get; set; }
        public string? ObservacionesAsesor { get; set; }
        public bool TieneEvidencia { get; set; }

        // Lista de trabajos
        public List<TrabajoDto> Trabajos { get; set; } = new();

        // Propiedades calculadas
        public string ProgresoTexto => $"{TrabajosCompletados}/{TotalTrabajos}";
        public string ProgresoFormateado => $"{ProgresoGeneral:F1}%";
        public bool TieneTrabajosEnProceso => Trabajos.Any(t => t.EnProceso);
        public bool TieneTrabajosCompletados => Trabajos.Any(t => t.EstaCompletado);
    }



    /// <summary>
    /// Request para crear orden con lista de trabajos
    /// </summary>
    public class CrearOrdenConTrabajosRequest
    {
        [Required]
        public int TipoOrdenId { get; set; }

        [Required]
        public int ClienteId { get; set; }

        [Required]
        public int VehiculoId { get; set; }

        [Required]
        public int KilometrajeActual { get; set; }

        [Required]
        public DateTime FechaHoraPromesaEntrega { get; set; }

        public string? ObservacionesAsesor { get; set; }

        public int? TipoServicioId { get; set; }

        [Required]
        [MinLength(1, ErrorMessage = "Debe agregar al menos un trabajo")]
        public List<TrabajoCrearDto> Trabajos { get; set; } = new();

    }
    public class TrabajoCrearDto
    {
        [Required]
        public string Trabajo { get; set; } = string.Empty;

        public string? Indicaciones { get; set; }
    }
    /// <summary>
    /// Response genérico para trabajos
    /// </summary>
    public class TrabajoResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public TrabajoDto? Trabajo { get; set; }
    }

    // DTO específ
    // ico para "Mis Trabajos"
    public class MiTrabajoDto
    {
        public int Id { get; set; }
        public int OrdenGeneralId { get; set; }
        public string NumeroOrden { get; set; } = string.Empty;
        public int TipoOrden { get; set; }
        public string Trabajo { get; set; } = string.Empty;

        // Vehículo
        public string VehiculoCompleto { get; set; } = string.Empty;
        public string VIN { get; set; } = string.Empty;
        public string Placas { get; set; } = string.Empty;

        // Trabajo
        public DateTime? FechaHoraAsignacionTecnico { get; set; }
        public DateTime? FechaHoraInicio { get; set; }
        public DateTime? FechaHoraTermino { get; set; }
        public string? IndicacionesTrabajo { get; set; }
        public string? ComentariosTecnico { get; set; }

        // Estado
        public int EstadoTrabajo { get; set; }
        public string? EstadoTrabajoNombre { get; set; }

        // Fechas
        public DateTime FechaCreacion { get; set; }
        public DateTime FechaPromesaEntrega { get; set; }

        // Propiedades calculadas
        public bool EsPendiente => EstadoTrabajo == 1;
        public bool EstaAsignado => EstadoTrabajo == 2;
        public bool EnProceso => EstadoTrabajo == 3;
        public bool EstaCompletado => EstadoTrabajo == 4;
        public bool EstaPausado => EstadoTrabajo == 5;
        public bool EstaCancelado => EstadoTrabajo == 6;


        public string DuracionFormateada
        {
            get
            {
                if (!FechaHoraInicio.HasValue || !FechaHoraTermino.HasValue)
                    return "-";

                var duracion = FechaHoraTermino.Value - FechaHoraInicio.Value;
                return $"{duracion.Hours}h {duracion.Minutes}m";
            }
        }

        public string TiempoTranscurrido
        {
            get
            {
                if (!FechaHoraInicio.HasValue)
                    return "-";

                var fechaFin = FechaHoraTermino ?? DateTime.Now;
                var duracion = fechaFin - FechaHoraInicio.Value;

                if (duracion.TotalHours >= 1)
                    return $"{(int)duracion.TotalHours}h {duracion.Minutes}m";
                else
                    return $"{duracion.Minutes}m";
            }
        }

    }
    /// <summary>
    /// DTO simplificado para lista de trabajos del Jefe de Taller
    /// </summary>
    public class TrabajoSimpleDto
    {
        public string Trabajo { get; set; } = string.Empty;
        public DateTime FechaHoraPromesaEntrega { get; set; }
        public string TecnicoNombre { get; set; } = string.Empty;
        public string EstadoTrabajoNombre { get; set; } = string.Empty;
        public DateTime? FechaHoraAsignacionTecnico { get; set; }
    }
    public class TrabajoSimpleResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Trabajo { get; set; } = string.Empty;
        public string VehiculoCompleto { get; set; } = string.Empty;
        public string VIN { get; set; } = string.Empty;

    }
}