using System.ComponentModel.DataAnnotations;

namespace CarSlineAPI.Models.DTOs
{
    /// <summary>
    /// Request para guardar checklist de servicio
    /// </summary>
    public class GuardarCheckListRequest
    {
        [Required]
        public int TrabajoId { get; set; }

        [Required]
        public int OrdenGeneralId { get; set; }

        [Required]
        public string Trabajo { get; set; } = string.Empty;

        [Required]
        public string ComentariosTecnico { get; set; } = string.Empty;

        // Sistema de Dirección
        [Required]
        public string Bieletas { get; set; } = string.Empty;

        [Required]
        public string Terminales { get; set; } = string.Empty;

        [Required]
        public string CajaDireccion { get; set; } = string.Empty;

        [Required]
        public string Volante { get; set; } = string.Empty;

        // Sistema de Suspensión
        [Required]
        public string AmortiguadoresDelanteros { get; set; } = string.Empty;

        [Required]
        public string AmortiguadoresTraseros { get; set; } = string.Empty;

        [Required]
        public string BarraEstabilizadora { get; set; } = string.Empty;

        [Required]
        public string Horquillas { get; set; } = string.Empty;

        // Neumáticos
        [Required]
        public string NeumaticosDelanteros { get; set; } = string.Empty;

        [Required]
        public string NeumaticosTraseros { get; set; } = string.Empty;

        [Required]
        public string Balanceo { get; set; } = string.Empty;

        [Required]
        public string Alineacion { get; set; } = string.Empty;

        // Luces
        [Required]
        public string LucesAltas { get; set; } = string.Empty;

        [Required]
        public string LucesBajas { get; set; } = string.Empty;

        [Required]
        public string LucesAntiniebla { get; set; } = string.Empty;

        [Required]
        public string LucesReversa { get; set; } = string.Empty;

        [Required]
        public string LucesDireccionales { get; set; } = string.Empty;

        [Required]
        public string LucesIntermitentes { get; set; } = string.Empty;

        // Sistema de Frenos
        [Required]
        public string DiscosTamboresDelanteros { get; set; } = string.Empty;

        [Required]
        public string DiscosTamboresTraseros { get; set; } = string.Empty;

        [Required]
        public string BalatasDelanteras { get; set; } = string.Empty;

        [Required]
        public string BalatasTraseras { get; set; } = string.Empty;

        // Piezas Reemplazadas
        public bool ReemplazoAceiteMotor { get; set; }
        public bool ReemplazoFiltroAceite { get; set; }
        public bool ReemplazoFiltroAireMotor { get; set; }
        public bool ReemplazoFiltroAirePolen { get; set; }

        // Revisión de Niveles
        public bool NivelLiquidoFrenos { get; set; } = true;
        public bool NivelAnticongelante { get; set; } = true;
        public bool NivelDepositoLimpiaparabrisas { get; set; } = true;
        public bool NivelAceiteMotor { get; set; } = true;

        // Trabajos Realizados
        public bool DescristalizacionTamboresDiscos { get; set; } = true;
        public bool AjusteFrenos { get; set; } = true;
        public bool CalibracionPresionNeumaticos { get; set; } = true;
        public bool RotacionNeumaticos { get; set; } =false;
        public bool TorqueNeumaticos { get; set; } = true;
    }

    /// <summary>
    /// DTO de respuesta del checklist
    /// </summary>
    public class CheckListDto
    {
        public int Id { get; set; }
        public int TrabajoId { get; set; }
        public int OrdenGeneralId { get; set; }
        public string Trabajo { get; set; } = string.Empty;

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

        // Revisión de Niveles
        public bool NivelLiquidoFrenos { get; set; }
        public bool NivelAnticongelante { get; set; }
        public bool NivelDepositoLimpiaparabrisas { get; set; }
        public bool NivelAceiteMotor { get; set; }

        // Trabajos Realizados
        public bool DescristalizacionTamboresDiscos { get; set; }
        public bool AjusteFrenos { get; set; }
        public bool CalibracionPresionNeumaticos { get; set; }
        public bool TorqueNeumaticos { get; set; }
    }

    /// <summary>
    /// Respuesta genérica para operaciones de checklist
    /// </summary>
    public class CheckListResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public CheckListDto? CheckList { get; set; }
    }
}