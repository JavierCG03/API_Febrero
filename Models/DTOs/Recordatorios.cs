namespace CarSlineAPI.Models.DTOs
{
    /// <summary>
    /// DTO para mostrar recordatorios de servicios pendientes
    /// </summary>
    /// 

    public class RecordatorioServicioSimpleDto
    {
        public int Id { get; set; }
        public string ClienteNombre { get; set; } = string.Empty;
        public DateTime FechaProximoServicio { get; set; }
        public string ProximoServicioNombre { get; set; } = string.Empty;
    }
    public class RecordatorioServicioDto
    {
        public int Id { get; set; }
        public int ClienteId { get; set; }
        public string ClienteNombre { get; set; } = string.Empty;
        public string ClienteTelefono { get; set; } = string.Empty;
        public int VehiculoId { get; set; }
        public string VehiculoInfo { get; set; } = string.Empty;
        public string VIN { get; set; } = string.Empty;

        // Información del último servicio
        public string UltimoServicioRealizado { get; set; } = string.Empty;
        public DateTime FechaUltimoServicio { get; set; }
        public int UltimoKilometraje { get; set; }

        // Información del próximo servicio
        public string TipoProximoServicio { get; set; } = string.Empty;
        public DateTime? FechaProximoServicio { get; set; }
        public int? KilometrajeProximoServicio { get; set; }

        // Fechas de recordatorios
        public DateTime? FechaPrimerRecordatorio { get; set; }
        public DateTime? FechaSegundoRecordatorio { get; set; }
        public DateTime? FechaTercerRecordatorio { get; set; }

        // Información adicional para la UI
        public int DiasDesdeUltimoServicio => (DateTime.Today - FechaUltimoServicio.Date).Days;
        public int? DiasParaProximoServicio => FechaProximoServicio.HasValue
            ? (FechaProximoServicio.Value.Date - DateTime.Today).Days
            : null;
        public int KilometrosRecorridos => 0; // Se puede calcular con kilometraje actual si está disponible

        public string FechaUltimoServicioFormateada => FechaUltimoServicio.ToString("dd/MMM/yyyy");
        public string FechaProximoServicioFormateada => FechaProximoServicio?.ToString("dd/MMM/yyyy") ?? "Sin fecha";
    }

    /// <summary>
    /// Response para obtener recordatorios
    /// </summary>
    public class ObtenerRecordatoriosResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string NombreRecordatorio { get; set; } = string.Empty; // "Primer Recordatorio", etc.
        public List<RecordatorioServicioSimpleDto> Recordatorios { get; set; } = new();

    }

    /// <summary>
    /// Request para marcar recordatorio como enviado
    /// </summary>
    public class MarcarRecordatorioRequest
    {
        public int ProximoServicioId { get; set; }
        public int TipoRecordatorio { get; set; } // 1, 2 o 3
    }

    /// <summary>
    /// Response genérico para operaciones de recordatorios
    /// </summary>
    public class RecordatorioResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}