using CarSlineAPI.Data;
using CarSlineAPI.Models.DTOs;
using CarSlineAPI.Models.Entities;
using CarSlineAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarSlineAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PdfController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly IPdfService _pdfService;
        private readonly ILogger<PdfController> _logger;

        public PdfController(
            ApplicationDbContext db,
            IPdfService pdfService,
            ILogger<PdfController> logger)
        {
            _db = db;
            _pdfService = pdfService;
            _logger = logger;
        }
        /// <summary>
        /// Generar y descargar PDF de una orden
        /// GET api/Pdf/orden/{ordenId}/descargar
        /// </summary>
        [HttpGet("orden/{ordenId}/descargar")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DescargarPdfOrden(int ordenId)
        {
            try
            {
                _logger.LogInformation($"📥 Solicitud de descarga PDF para orden {ordenId}");

                var ordenDto = await ObtenerDatosOrdenAsync(ordenId);

                if (ordenDto == null)
                {
                    return NotFound(new { Message = "Orden no encontrada" });
                }

                var pdfBytes = await _pdfService.GenerarPdfOrdenAsync(ordenDto);

                _logger.LogInformation($"✅ PDF generado exitosamente: {pdfBytes.Length} bytes");

                return File(
                    pdfBytes,
                    "application/pdf",
                    $"Orden_{ordenDto.NumeroOrden}_{DateTime.Now:yyyyMMdd}.pdf"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error al generar PDF para orden {ordenId}");
                return StatusCode(500, new { Message = $"Error al generar PDF: {ex.Message}" });
            }
        }

        /// <summary>
        /// Generar y guardar PDF de una orden en el servidor
        /// POST api/Pdf/orden/{ordenId}/guardar
        /// </summary>
        [HttpPost("orden/{ordenId}/preview")]
        [ProducesResponseType(typeof(PdfPreviewResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GuardarPdfOrden(int ordenId)
        {
            try
            {
                //_logger.LogInformation($"💾 Solicitud de guardar PDF para orden {ordenId}");

                var ordenDto = await ObtenerDatosOrdenAsync(ordenId);

                if (ordenDto == null)
                {
                    return NotFound(new { Message = "Orden no encontrada" });
                }

                var pdfBytes = await _pdfService.GuardarPdfOrdenAsync(
                    ordenDto,
                    ordenDto.NumeroOrden
                );


                return Ok(new PdfPreviewResponse
                {
                    Success = true,
                    PdfBase64 = Convert.ToBase64String(pdfBytes),
                    NumeroOrden = ordenDto.NumeroOrden
                });

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error en vista previa PDF orden {ordenId}");
                return StatusCode(500, new PdfPreviewResponse
                {
                    Success = false,
                    Message = $"Error: {ex.Message}"
                });
            }
        }

        private async Task<OrdenPdfDto?> ObtenerDatosOrdenAsync(int ordenId)
        {
            var orden = await _db.OrdenesGenerales
                .Include(o => o.Cliente)
                .Include(o => o.Vehiculo)
                .Include(o => o.Asesor)
                .Include(o => o.TipoOrden)
                .Include(o => o.EstadoOrden)
                .Include(o => o.Trabajos.Where(t => t.Activo))
                    .ThenInclude(t => t.TecnicoAsignado)
                .Include(o => o.Trabajos)
                    .ThenInclude(t => t.EstadoTrabajoNavegacion)
                .Where(o => o.Id == ordenId && o.Activo)
                .FirstOrDefaultAsync();

            if (orden == null) return null;

            // Obtener refacciones de cada trabajo
            var trabajosConRefacciones = new List<TrabajoPdfDto>();

            foreach (var trabajo in orden.Trabajos.Where(t => t.Activo))
            {
                var refacciones = await _db.Set<Refacciontrabajo>()
                    .Where(r => r.TrabajoId == trabajo.Id)
                    .Select(r => new RefaccionPdfDto
                    {
                        Refaccion = r.Refaccion,
                        Cantidad = r.Cantidad,
                        PrecioUnitario = r.PrecioUnitario,
                        Total = r.Cantidad * r.PrecioUnitario
                    })
                    .ToListAsync();

                var duracion = trabajo.FechaHoraInicio.HasValue && trabajo.FechaHoraTermino.HasValue
                    ? (trabajo.FechaHoraTermino.Value - trabajo.FechaHoraInicio.Value)
                    : (TimeSpan?)null;

                trabajosConRefacciones.Add(new TrabajoPdfDto
                {
                    Trabajo = trabajo.Trabajo,
                    TecnicoNombre = trabajo.TecnicoAsignado?.NombreCompleto,
                    EstadoTrabajo = trabajo.EstadoTrabajoNavegacion?.NombreEstado ?? "Pendiente",
                    FechaInicio = trabajo.FechaHoraInicio,
                    FechaTermino = trabajo.FechaHoraTermino,
                    Duracion = duracion.HasValue
                        ? $"{duracion.Value.Hours}h {duracion.Value.Minutes}m"
                        : null,
                    CostoManoObra = trabajo.CostoManoObra,
                    TotalRefacciones = trabajo.RefaccionesTotal,              
                    Refacciones = refacciones,
                    ComentariosTecnico = trabajo.ComentariosTecnico
                });
            }

            // Obtener checklist si existe
            var checkList = await _db.Set<CheckListServicio>()
                .Where(c => c.OrdenGeneralId == ordenId)
                .Select(c => new CheckListPdfDto
                {
                    Bieletas = c.Bieletas,
                    Terminales = c.Terminales,
                    CajaDireccion = c.CajaDireccion,
                    Volante = c.Volante,
                    AmortiguadoresDelanteros = c.AmortiguadoresDelanteros,
                    AmortiguadoresTraseros = c.AmortiguadoresTraseros,
                    BarraEstabilizadora = c.BarraEstabilizadora,
                    Horquillas = c.Horquillas,
                    NeumaticosDelanteros = c.NeumaticosDelanteros,
                    NeumaticosTraseros = c.NeumaticosTraseros,
                    Balanceo = c.Balanceo,
                    Alineacion = c.Alineacion,
                    LucesAltas = c.LucesAltas,
                    LucesBajas = c.LucesBajas,
                    LucesAntiniebla = c.LucesAntiniebla,
                    LucesReversa = c.LucesReversa,
                    LucesDireccionales = c.LucesDireccionales,
                    LucesIntermitentes = c.LucesIntermitentes,
                    DiscosTamboresDelanteros = c.DiscosTamboresDelanteros,
                    DiscosTamboresTraseros = c.DiscosTamboresTraseros,
                    BalatasDelanteras = c.BalatasDelanteras,
                    BalatasTraseras = c.BalatasTraseras,
                    ReemplazoAceiteMotor = c.ReemplazoAceiteMotor,
                    ReemplazoFiltroAceite = c.ReemplazoFiltroAceite,
                    ReemplazoFiltroAireMotor = c.ReemplazoFiltroAireMotor,
                    ReemplazoFiltroAirePolen = c.ReemplazoFiltroAirePolen,
                    DescristalizacionTamboresDiscos = c.DescristalizacionTamboresDiscos,
                    AjusteFrenos = c.AjusteFrenos,
                    CalibracionPresionNeumaticos = c.CalibracionPresionNeumaticos,
                    TorqueNeumaticos = c.TorqueNeumaticos,
                    RotacionNeumaticos = c.RotacionNeumaticos
                })
                .FirstOrDefaultAsync();

            return new OrdenPdfDto
            {
                NumeroOrden = orden.NumeroOrden,
                TipoOrden = orden.TipoOrden.NombreTipo ?? "",
                FechaCreacion = orden.FechaCreacion,
                FechaPromesaEntrega = orden.FechaHoraPromesaEntrega,
                FechaFinalizacion = orden.FechaFinalizacion,
                EstadoOrden = orden.EstadoOrden.NombreEstado ?? "",

                Cliente = new ClientePdfDto
                {
                    NombreCompleto = orden.Cliente.NombreCompleto,
                    RFC = orden.Cliente.RFC,
                    TelefonoMovil = orden.Cliente.TelefonoMovil,
                    CorreoElectronico = orden.Cliente.CorreoElectronico,
                    DireccionCompleta = $"{orden.Cliente.Calle} {orden.Cliente.NumeroExterior}, {orden.Cliente.Colonia}, {orden.Cliente.Municipio}, {orden.Cliente.Estado}"
                },

                Vehiculo = new VehiculoPdfDto
                {
                    VehiculoCompleto = $"{orden.Vehiculo.Marca} {orden.Vehiculo.Modelo} {orden.Vehiculo.Anio}",
                    VIN = orden.Vehiculo.VIN,
                    Placas = orden.Vehiculo.Placas,
                    Color = orden.Vehiculo.Color ?? "",
                    KilometrajeActual = orden.KilometrajeActual
                },

                AsesorNombre = orden.Asesor.NombreCompleto,
                Trabajos = trabajosConRefacciones,
                CheckList = checkList,

                TotalRefacciones = trabajosConRefacciones.Sum(t => t.TotalRefacciones),
                TotalManoObra = trabajosConRefacciones.Sum(t => t.CostoManoObra),
                CostoTotal = orden.CostoTotal ?? 0,
                CostoTotal_IVA = orden.CostoTotal_IVA,

                ObservacionesAsesor = orden.ObservacionesAsesor,
                ObservacionesJefeTaller = orden.ObservacionesJefe,

                TotalTrabajos = orden.TotalTrabajos,
                TrabajosCompletados = orden.TrabajosCompletados,
                ProgresoGeneral = orden.ProgresoGeneral
            };
        }
    }

}