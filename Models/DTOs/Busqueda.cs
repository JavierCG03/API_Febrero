namespace CarSlineAPI.Models.DTOs
{
    /// <summary>
    /// Tipos de resultado de búsqueda
    /// </summary>
    public enum TipoResultadoBusqueda
    {
        Cliente = 1,
        Vehiculo = 2,
        Orden = 3
    }

    /// <summary>
    /// Resultado individual de búsqueda unificada
    /// </summary>
    public class ResultadoBusquedaDto
    {
        public int Id { get; set; }
        public TipoResultadoBusqueda Tipo { get; set; }
        public string TipoTexto { get; set; } = string.Empty;
        public string TituloPrincipal { get; set; } = string.Empty;
        public string Subtitulo { get; set; } = string.Empty;
        public string Detalle { get; set; } = string.Empty;
        public string IconoColor { get; set; } = "#2196F3";
        public string Icono { get; set; } = "👤";
    }

    /// <summary>
    /// Respuesta de búsqueda unificada
    /// </summary>
    public class BusquedaUnificadaResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<ResultadoBusquedaDto> Resultados { get; set; } = new();
        public int TotalResultados { get; set; }
        public string TerminoBusqueda { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO simple de orden para búsqueda
    /// </summary>
    public class OrdenSimpleDto
    {
        public int Id { get; set; }
        public string NumeroOrden { get; set; } = string.Empty;
        public string ClienteNombre { get; set; } = string.Empty;
        public string VehiculoInfo { get; set; } = string.Empty;
        public DateTime FechaCreacion { get; set; }
        public int EstadoOrdenId { get; set; }
        public string EstadoOrden { get; set; } = string.Empty;
    }

    /// <summary>
    /// Respuesta de búsqueda de orden
    /// </summary>
    public class BuscarOrdenResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public OrdenSimpleDto? Orden { get; set; }
    }
}