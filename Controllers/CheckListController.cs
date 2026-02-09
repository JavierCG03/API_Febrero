using CarSlineAPI.Data;
using CarSlineAPI.Models.DTOs;
using CarSlineAPI.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarSlineAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CheckListController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<CheckListController> _logger;

        public CheckListController(ApplicationDbContext db, ILogger<CheckListController> logger)
        {
            _db = db;
            _logger = logger;
        }

        /// <summary>
        /// Guardar checklist de servicio
        /// POST api/CheckList/guardar
        /// </summary>
        [HttpPost("guardar")]
        [ProducesResponseType(typeof(CheckListResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GuardarCheckList([FromBody] GuardarCheckListRequest request)
        {
            _logger.LogInformation("📥 RECIBIENDO CHECKLIST:");
            _logger.LogInformation($"TrabajoId: {request.TrabajoId}");
            _logger.LogInformation($"OrdenGeneralId: {request.OrdenGeneralId}");
            _logger.LogInformation($"Trabajo: {request.Trabajo}");

            if (!ModelState.IsValid)
            {
                var errors = string.Join(", ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));

                _logger.LogWarning($"❌ Modelo inválido: {errors}");

                return BadRequest(new CheckListResponse
                {
                    Success = false,
                    Message = $"Datos inválidos: {errors}"
                });
            }

            try
            {
                // Verificar que el trabajo existe
                var trabajo = await _db.TrabajosPorOrden
                    .FirstOrDefaultAsync(t => t.Id == request.TrabajoId && t.Activo);

                if (trabajo == null)
                {
                    _logger.LogWarning($"❌ Trabajo {request.TrabajoId} no encontrado");
                    return NotFound(new CheckListResponse
                    {
                        Success = false,
                        Message = "Trabajo no encontrado"
                    });
                }

                // Verificar si ya existe un checklist para este trabajo
                var checklistExistente = await _db.Set<CheckListServicio>()
                    .FirstOrDefaultAsync(c => c.TrabajoId == request.TrabajoId);

                if (checklistExistente != null)
                {
                    _logger.LogInformation($"♻️ Actualizando checklist existente ID {checklistExistente.Id}");
                    ActualizarCheckList(checklistExistente, request);
                }
                else
                {
                    _logger.LogInformation("✨ Creando nuevo checklist");
                    var nuevoCheckList = new CheckListServicio
                    {
                        TrabajoId = request.TrabajoId,
                        OrdenGeneralId = trabajo.OrdenGeneralId,
                        Trabajo = request.Trabajo,

                        // Sistema de Dirección
                        Bieletas = request.Bieletas,
                        Terminales = request.Terminales,
                        CajaDireccion = request.CajaDireccion,
                        Volante = request.Volante,

                        // Sistema de Suspensión
                        AmortiguadoresDelanteros = request.AmortiguadoresDelanteros,
                        AmortiguadoresTraseros = request.AmortiguadoresTraseros,
                        BarraEstabilizadora = request.BarraEstabilizadora,
                        Horquillas = request.Horquillas,

                        // Neumáticos
                        NeumaticosDelanteros = request.NeumaticosDelanteros,
                        NeumaticosTraseros = request.NeumaticosTraseros,
                        Balanceo = request.Balanceo,
                        Alineacion = request.Alineacion,

                        // Luces
                        LucesAltas = request.LucesAltas,
                        LucesBajas = request.LucesBajas,
                        LucesAntiniebla = request.LucesAntiniebla,
                        LucesReversa = request.LucesReversa,
                        LucesDireccionales = request.LucesDireccionales,
                        LucesIntermitentes = request.LucesIntermitentes,

                        // Sistema de Frenos
                        DiscosTamboresDelanteros = request.DiscosTamboresDelanteros,
                        DiscosTamboresTraseros = request.DiscosTamboresTraseros,
                        BalatasDelanteras = request.BalatasDelanteras,
                        BalatasTraseras = request.BalatasTraseras,

                        // Piezas Reemplazadas
                        ReemplazoAceiteMotor = request.ReemplazoAceiteMotor,
                        ReemplazoFiltroAceite = request.ReemplazoFiltroAceite,
                        ReemplazoFiltroAireMotor = request.ReemplazoFiltroAireMotor,
                        ReemplazoFiltroAirePolen = request.ReemplazoFiltroAirePolen,

                        // Revisión de Niveles
                        NivelLiquidoFrenos = request.NivelLiquidoFrenos,
                        NivelAnticongelante = request.NivelAnticongelante,
                        NivelDepositoLimpiaparabrisas = request.NivelDepositoLimpiaparabrisas,
                        NivelAceiteMotor = request.NivelAceiteMotor,

                        // Trabajos Realizados
                        DescristalizacionTamboresDiscos = request.DescristalizacionTamboresDiscos,
                        AjusteFrenos = request.AjusteFrenos,
                        CalibracionPresionNeumaticos = request.CalibracionPresionNeumaticos,
                        TorqueNeumaticos = request.TorqueNeumaticos,
                        RotacionNeumaticos = request.RotacionNeumaticos
                    };

                    _db.Set<CheckListServicio>().Add(nuevoCheckList);
                }

                // ✅ IMPORTANTE: Marcar el trabajo como completado
                trabajo.ComentariosTecnico = request.ComentariosTecnico;
                trabajo.EstadoTrabajo = 4; // 4 = Completado
                trabajo.FechaHoraTermino = DateTime.Now;

                await _db.SaveChangesAsync();

                _logger.LogInformation($"✅ Checklist guardado exitosamente para trabajo {request.TrabajoId}");

                return Ok(new CheckListResponse
                {
                    Success = true,
                    Message = "Checklist guardado exitosamente y trabajo completado"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error al guardar checklist para trabajo {request.TrabajoId}");
                return StatusCode(500, new CheckListResponse
                {
                    Success = false,
                    Message = $"Error al guardar checklist: {ex.Message}"
                });
            }
        }
        /// <summary>
        /// Obtener checklist por ID de trabajo
        /// GET api/CheckList/trabajo/{trabajoId}
        /// </summary>
        [HttpGet("trabajo/{trabajoId}")]
        [ProducesResponseType(typeof(CheckListResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ObtenerCheckListPorTrabajo(int trabajoId)
        {
            try
            {
                var checkList = await _db.Set<CheckListServicio>()
                    .FirstOrDefaultAsync(c => c.TrabajoId == trabajoId);

                if (checkList == null)
                {
                    return NotFound(new CheckListResponse
                    {
                        Success = false,
                        Message = "Checklist no encontrado"
                    });
                }

                return Ok(new CheckListResponse
                {
                    Success = true,
                    Message = "Checklist encontrado",
                    CheckList = MapearCheckListDto(checkList)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al obtener checklist para trabajo {trabajoId}");
                return StatusCode(500, new CheckListResponse
                {
                    Success = false,
                    Message = "Error al obtener checklist"
                });
            }
        }

        // ============================================
        // MÉTODOS PRIVADOS AUXILIARES
        // ============================================

        private void ActualizarCheckList(CheckListServicio checkList, GuardarCheckListRequest request)
        {
            // Sistema de Dirección
            checkList.Bieletas = request.Bieletas;
            checkList.Terminales = request.Terminales;
            checkList.CajaDireccion = request.CajaDireccion;
            checkList.Volante = request.Volante;

            // Sistema de Suspensión
            checkList.AmortiguadoresDelanteros = request.AmortiguadoresDelanteros;
            checkList.AmortiguadoresTraseros = request.AmortiguadoresTraseros;
            checkList.BarraEstabilizadora = request.BarraEstabilizadora;
            checkList.Horquillas = request.Horquillas;

            // Neumáticos
            checkList.NeumaticosDelanteros = request.NeumaticosDelanteros;
            checkList.NeumaticosTraseros = request.NeumaticosTraseros;
            checkList.Balanceo = request.Balanceo;
            checkList.Alineacion = request.Alineacion;

            // Luces
            checkList.LucesAltas = request.LucesAltas;
            checkList.LucesBajas = request.LucesBajas;
            checkList.LucesAntiniebla = request.LucesAntiniebla;
            checkList.LucesReversa = request.LucesReversa;
            checkList.LucesDireccionales = request.LucesDireccionales;
            checkList.LucesIntermitentes = request.LucesIntermitentes;

            // Sistema de Frenos
            checkList.DiscosTamboresDelanteros = request.DiscosTamboresDelanteros;
            checkList.DiscosTamboresTraseros = request.DiscosTamboresTraseros;
            checkList.BalatasDelanteras = request.BalatasDelanteras;
            checkList.BalatasTraseras = request.BalatasTraseras;

            // Piezas Reemplazadas
            checkList.ReemplazoAceiteMotor = request.ReemplazoAceiteMotor;
            checkList.ReemplazoFiltroAceite = request.ReemplazoFiltroAceite;
            checkList.ReemplazoFiltroAireMotor = request.ReemplazoFiltroAireMotor;
            checkList.ReemplazoFiltroAirePolen = request.ReemplazoFiltroAirePolen;

            // Revisión de Niveles
            checkList.NivelLiquidoFrenos = request.NivelLiquidoFrenos;
            checkList.NivelAnticongelante = request.NivelAnticongelante;
            checkList.NivelDepositoLimpiaparabrisas = request.NivelDepositoLimpiaparabrisas;
            checkList.NivelAceiteMotor = request.NivelAceiteMotor;

            // Trabajos Realizados
            checkList.DescristalizacionTamboresDiscos = request.DescristalizacionTamboresDiscos;
            checkList.AjusteFrenos = request.AjusteFrenos;
            checkList.CalibracionPresionNeumaticos = request.CalibracionPresionNeumaticos;
            checkList.TorqueNeumaticos = request.TorqueNeumaticos;
        }

        private CheckListDto MapearCheckListDto(CheckListServicio checkList)
        {
            return new CheckListDto
            {
                Id = checkList.Id,
                TrabajoId = checkList.TrabajoId,
                OrdenGeneralId = checkList.OrdenGeneralId,
                Trabajo = checkList.Trabajo,

                // Sistema de Dirección
                Bieletas = checkList.Bieletas,
                Terminales = checkList.Terminales,
                CajaDireccion = checkList.CajaDireccion,
                Volante = checkList.Volante,

                // Sistema de Suspensión
                AmortiguadoresDelanteros = checkList.AmortiguadoresDelanteros,
                AmortiguadoresTraseros = checkList.AmortiguadoresTraseros,
                BarraEstabilizadora = checkList.BarraEstabilizadora,
                Horquillas = checkList.Horquillas,

                // Neumáticos
                NeumaticosDelanteros = checkList.NeumaticosDelanteros,
                NeumaticosTraseros = checkList.NeumaticosTraseros,
                Balanceo = checkList.Balanceo,
                Alineacion = checkList.Alineacion,

                // Luces
                LucesAltas = checkList.LucesAltas,
                LucesBajas = checkList.LucesBajas,
                LucesAntiniebla = checkList.LucesAntiniebla,
                LucesReversa = checkList.LucesReversa,
                LucesDireccionales = checkList.LucesDireccionales,
                LucesIntermitentes = checkList.LucesIntermitentes,

                // Sistema de Frenos
                DiscosTamboresDelanteros = checkList.DiscosTamboresDelanteros,
                DiscosTamboresTraseros = checkList.DiscosTamboresTraseros,
                BalatasDelanteras = checkList.BalatasDelanteras,
                BalatasTraseras = checkList.BalatasTraseras,

                // Piezas Reemplazadas
                ReemplazoAceiteMotor = checkList.ReemplazoAceiteMotor,
                ReemplazoFiltroAceite = checkList.ReemplazoFiltroAceite,
                ReemplazoFiltroAireMotor = checkList.ReemplazoFiltroAireMotor,
                ReemplazoFiltroAirePolen = checkList.ReemplazoFiltroAirePolen,

                // Revisión de Niveles
                NivelLiquidoFrenos = checkList.NivelLiquidoFrenos,
                NivelAnticongelante = checkList.NivelAnticongelante,
                NivelDepositoLimpiaparabrisas = checkList.NivelDepositoLimpiaparabrisas,
                NivelAceiteMotor = checkList.NivelAceiteMotor,

                // Trabajos Realizados
                DescristalizacionTamboresDiscos = checkList.DescristalizacionTamboresDiscos,
                AjusteFrenos = checkList.AjusteFrenos,
                CalibracionPresionNeumaticos = checkList.CalibracionPresionNeumaticos,
                TorqueNeumaticos = checkList.TorqueNeumaticos
            };
        }
    }
}