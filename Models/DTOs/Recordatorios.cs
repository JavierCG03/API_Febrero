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
        public int Clienteid { get; set; }
        public int Vehiculoid { get; set; }

        // Informacion del Cliente
        public string ClienteNombre { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public string TelefonoCasa { get; set; } = string.Empty;
        public string Correo { get; set; } = string.Empty;

        // Informacion del Vehiculo
        public string InfoVehiculo { get; set; } = string.Empty;
        public string VIN { get; set; } = string.Empty;
        public string Placas { get; set; } = string.Empty;

        // Información del último servicio
        public string UltimoServicioRealizado { get; set; } = string.Empty;
        public DateTime FechaUltimoServicio { get; set; }
        public int UltimoKilometraje { get; set; }

        // Información del próximo servicio
        public string TipoProximoServicio { get; set; } = string.Empty;
        public DateTime? FechaProximoServicio { get; set; }
        public int? KilometrajeProximoServicio { get; set; }


        public int? DiasParaProximoServicio => FechaProximoServicio.HasValue
            ? (FechaProximoServicio.Value.Date - DateTime.Today).Days
            : null;

        //public string FechaUltimoServicioFormateada => FechaUltimoServicio.ToString("dd/MMM/yyyy");
        //public string FechaProximoServicioFormateada => FechaProximoServicio?.ToString("dd/MMM/yyyy") ?? "Sin fecha";
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

    public class ObtenerRecordatorioResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<RecordatorioServicioDto> Recordatorios { get; set; } = new();

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