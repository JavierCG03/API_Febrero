namespace CarSlineAPI.Models.DTOs
{
    /// <summary>
    /// DTO completo para generar PDF de orden de servicio
    /// </summary>
    public class OrdenPdfDto
    {
        // Información de la Orden
        public string NumeroOrden { get; set; } = string.Empty;
        public string TipoOrden { get; set; } = string.Empty;
        public DateTime FechaCreacion { get; set; }
        public DateTime FechaPromesaEntrega { get; set; }
        public DateTime? FechaFinalizacion { get; set; }
        public string EstadoOrden { get; set; } = string.Empty;

        // Cliente
        public ClientePdfDto Cliente { get; set; } = new();

        // Vehículo
        public VehiculoPdfDto Vehiculo { get; set; } = new();

        // Asesor
        public string AsesorNombre { get; set; } = string.Empty;

        // Trabajos
        public List<TrabajoPdfDto> Trabajos { get; set; } = new();

        // Checklist (si existe)
        public CheckListPdfDto? CheckList { get; set; }

        // Costos
        public decimal TotalRefacciones { get; set; }
        public decimal TotalManoObra { get; set; }
        public decimal CostoTotal { get; set; }
        public decimal CostoTotal_IVA { get; set; }
        // Observaciones
        public string? ObservacionesAsesor { get; set; }
        public string? ObservacionesJefeTaller { get; set; }

        // Progreso
        public int TotalTrabajos { get; set; }
        public int TrabajosCompletados { get; set; }
        public decimal ProgresoGeneral { get; set; }
    }

    public class ClientePdfDto
    {
        public string NombreCompleto { get; set; } = string.Empty;
        public string RFC { get; set; } = string.Empty;
        public string TelefonoMovil { get; set; } = string.Empty;
        public string? CorreoElectronico { get; set; }
        public string DireccionCompleta { get; set; } = string.Empty;
    }

    public class VehiculoPdfDto
    {
        public string VehiculoCompleto { get; set; } = string.Empty;
        public string VIN { get; set; } = string.Empty;
        public string? Placas { get; set; }
        public string Color { get; set; } = string.Empty;
        public int KilometrajeActual { get; set; }
    }

    public class TrabajoPdfDto
    {
        public string Trabajo { get; set; } = string.Empty;
        public string? TecnicoNombre { get; set; }
        public string EstadoTrabajo { get; set; } = string.Empty;
        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaTermino { get; set; }
        public string? Duracion { get; set; }
        public decimal CostoManoObra { get; set; }
        public decimal TotalRefacciones { get; set; }
        public List<RefaccionPdfDto> Refacciones { get; set; } = new();
        public string? ComentariosTecnico { get; set; }
    }

    public class RefaccionPdfDto
    {
        public string Refaccion { get; set; } = string.Empty;
        public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal Total { get; set; }
    }

    public class CheckListPdfDto
    {
        // Sistema de Dirección
        public string Bieletas { get; set; } = string.Empty;
        public string Terminales { get; set; } = string.Empty;
        public string CajaDireccion { get; set; } = string.Empty;
        public string Volante { get; set; } = string.Empty;

        // Sistema de Suspensión
        public string AmortiguadoresDelanteros { get; set; } = string.Empty;
        public string AmortiguadoresTraseros { get; set; } = string.Empty;
        public string BarraEstabilizadora { get; set; } = string.Empty;
        public string Horquillas { get; set; } = string.Empty;

        // Neumáticos
        public string NeumaticosDelanteros { get; set; } = string.Empty;
        public string NeumaticosTraseros { get; set; } = string.Empty;
        public string Balanceo { get; set; } = string.Empty;
        public string Alineacion { get; set; } = string.Empty;

        // Luces
        public string LucesAltas { get; set; } = string.Empty;
        public string LucesBajas { get; set; } = string.Empty;
        public string LucesAntiniebla { get; set; } = string.Empty;
        public string LucesReversa { get; set; } = string.Empty;
        public string LucesDireccionales { get; set; } = string.Empty;
        public string LucesIntermitentes { get; set; } = string.Empty;

        // Sistema de Frenos
        public string DiscosTamboresDelanteros { get; set; } = string.Empty;
        public string DiscosTamboresTraseros { get; set; } = string.Empty;
        public string BalatasDelanteras { get; set; } = string.Empty;
        public string BalatasTraseras { get; set; } = string.Empty;

        // Piezas Reemplazadas
        public bool ReemplazoAceiteMotor { get; set; }
        public bool ReemplazoFiltroAceite { get; set; }
        public bool ReemplazoFiltroAireMotor { get; set; }
        public bool ReemplazoFiltroAirePolen { get; set; }

        // Trabajos Realizados
        public bool DescristalizacionTamboresDiscos { get; set; }
        public bool AjusteFrenos { get; set; }
        public bool CalibracionPresionNeumaticos { get; set; }
        public bool TorqueNeumaticos { get; set; }
        public bool RotacionNeumaticos { get; set; }
    }
    public class GuardarPdfResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? RutaArchivo { get; set; }
        public string? NombreArchivo { get; set; }
        public string? NumeroOrden { get; set; }
    }

    public class PdfPreviewResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? PdfBase64 { get; set; }
        public string? NumeroOrden { get; set; }
        public int TamanoBytes { get; set; }
    }
}