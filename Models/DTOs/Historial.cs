namespace CarSlineAPI.Models.DTOs
{

    public class HistorialServicioDto
    {
        public string NumeroOrden { get; set; } = string.Empty;
        public DateTime FechaServicio { get; set; }
        public string TipoServicio { get; set; } = string.Empty;
        public int KilometrajeRegistrado { get; set; }
        public string ObservacionesAsesor { get; set; } = string.Empty;

        // Propiedades calculadas para la vista
        public string FechaFormateada => FechaServicio.ToString("dd/MMM/yyyy");
    }
    public class HistorialOrdenDto
    {
        public int OrdenId { get; set; }
        public string NumeroOrden { get; set; } = string.Empty;
        public DateTime FechaOrden { get; set; }
        public string TipoOrden { get; set; } = string.Empty;
        public string EstadoOrden { get; set; }
        public int KilometrajeRegistrado { get; set; }
        public string ObservacionesAsesor { get; set; } = string.Empty;

    }
    public class HistorialGeneralResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<HistorialOrdenDto> Historial { get; set; } = new();
        public bool TieneHistorial => Historial != null && Historial.Any();
    }

    public class HistorialVehiculoResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<HistorialServicioDto> Historial { get; set; } = new();

        // Estadísticas del historial
        public int UltimoKilometraje { get; set; }
        public string UltimoServicio { get; set; }
        public DateTime? UltimaFechaServicio { get; set; }

        // Propiedades calculadas
        public string UltimaFechaFormateada => UltimaFechaServicio?.ToString("dd/MMM/yyyy") ?? "Sin servicios";
        public bool TieneHistorial => Historial != null && Historial.Any();
    }
}