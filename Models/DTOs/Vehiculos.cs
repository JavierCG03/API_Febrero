namespace CarSlineAPI.Models.DTOs
{
    public class VehiculoRequest
    {
        public int ClienteId { get; set; }
        public string VIN { get; set; } = string.Empty;
        public string? Marca { get; set; }
        public string? Modelo { get; set; }
        public string? Version { get; set; }
        public int? Anio { get; set; }
        public string? Color { get; set; }
        public string? Placas { get; set; }
        public int KilometrajeInicial { get; set; }
    }
    public class VehiculoResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int? VehiculoId { get; set; }
        public VehiculoDto? Vehiculo { get; set; }
    }
    public class VehiculoDto
    {
        public int Id { get; set; }
        public int ClienteId { get; set; }
        public string VIN { get; set; } = string.Empty;
        public string Marca { get; set; } = string.Empty;
        public string Modelo { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public int Anio { get; set; }
        public string Color { get; set; } = string.Empty;
        public string Placas { get; set; } = string.Empty;
        public int KilometrajeInicial { get; set; }
        public string NombreCliente { get; set; } = string.Empty;

        // ✅ NUEVO: Para mostrar en lista de selección
        public string InfoResumen =>
            $"{Marca} {Modelo} {Anio} - VIN: ...{VIN.Substring(Math.Max(0, VIN.Length - 4))} - Cliente: {NombreCliente}";

        public string VehiculoCompleto => $"{Marca} {Modelo} {Anio} - {Color}";
        public string Ultimos4VIN => VIN.Length >= 4 ? VIN.Substring(VIN.Length - 4) : VIN;
    }

    /// <summary>
    /// NUEVO: Respuesta para búsqueda de múltiples vehículos
    /// </summary>
    public class BuscarVehiculosResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<VehiculoDto> Vehiculos { get; set; } = new();
    }
}
