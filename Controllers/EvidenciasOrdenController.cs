
using CarSlineAPI.Data;
using CarSlineAPI.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


namespace CarSlineAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EvidenciasOrdenController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<EvidenciasOrdenController> _logger;
        private readonly string _rutaBaseEvidencias = @"C:\Users\LENOVO\Downloads\Evidencias_Ordenes";
        public EvidenciasOrdenController(ApplicationDbContext db, ILogger<EvidenciasOrdenController> logger)
        {
            _db = db;
            _logger = logger;
            if (!Directory.Exists(_rutaBaseEvidencias))
            {
                Directory.CreateDirectory(_rutaBaseEvidencias);
            }
        }

        // POST: api/EvidenciasTrabajoOrden
        [HttpPost("Trabajo")]
        public async Task<ActionResult<List<Evidenciaorden>>> PostEvidenciasTrabajo([FromForm] EvidenciasUploadModel model)
        {
            try
            {
                if (model.Imagenes == null || !model.Imagenes.Any())
                {
                    return BadRequest("No se recibieron imágenes");
                }

                if (model.Descripciones == null || model.Descripciones.Count != model.Imagenes.Count)
                {
                    return BadRequest("El número de descripciones debe coincidir con el número de imágenes");
                }

                // Buscar el trabajo
                var Orden = await _db.Set<OrdenGeneral>()
                    .FirstOrDefaultAsync(t => t.Id == model.OrdenGeneralId);

                // Verificar que la orden existe
                var ordenExiste = await _db.Set<OrdenGeneral>()
                    .AnyAsync(o => o.Id == model.OrdenGeneralId);


                if (!ordenExiste)
                {
                    return NotFound($"La orden {model.OrdenGeneralId} no existe");
                }

                // Crear carpeta específica para esta orden
                string carpetaOrden = Path.Combine(_rutaBaseEvidencias, $"{Orden.NumeroOrden}/Trabajo");
                if (!Directory.Exists(carpetaOrden))
                {
                    Directory.CreateDirectory(carpetaOrden);
                }

                var evidenciasGuardadas = new List<Evidenciaorden>();

                // Procesar cada imagen
                for (int i = 0; i < model.Imagenes.Count; i++)
                {
                    var imagen = model.Imagenes[i];
                    var descripcion = model.Descripciones[i];

                    if (imagen.Length > 0)
                    {
                        // Generar nombre único para el archivo
                        string descripcionLimpia = LimpiarNombreArchivo(descripcion);
                        string extension = Path.GetExtension(imagen.FileName);
                        string nombreArchivo = $"{descripcionLimpia}_{DateTime.Now:dd_HH_mm_ss}{extension}";
                        string rutaCompleta = Path.Combine(carpetaOrden, nombreArchivo);

                        // Guardar archivo físico
                        using (var stream = new FileStream(rutaCompleta, FileMode.Create))
                        {
                            await imagen.CopyToAsync(stream);
                        }

                        // Crear registro en base de datos
                        var evidencia = new Evidenciaorden
                        {
                            OrdenGeneralId = model.OrdenGeneralId,
                            RutaImagen = rutaCompleta,
                            Descripcion = descripcion,
                            FechaRegistro = DateTime.Now,
                            EvidenciaTrabajo=true,
                            Activo = true
                        };

                        _db.Set<Evidenciaorden>().Add(evidencia);
                        evidenciasGuardadas.Add(evidencia);
                    }
                }
                Orden.TieneEvidencia = true;
                await _db.SaveChangesAsync();
                return Ok(new
                {
                    mensaje = "Evidencias guardadas correctamente",
                    cantidad = evidenciasGuardadas.Count,
                    evidencias = evidenciasGuardadas.Select(e => new
                    {
                        e.Id,
                        e.Descripcion,
                        e.RutaImagen,
                        e.FechaRegistro
                    })
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno del servidor: {ex.Message}");
            }
        }

        // GET: api/EvidenciasOrden/5
        [HttpGet("Trabajo/{ordenGeneralId}")]
        public async Task<ActionResult<IEnumerable<Evidenciaorden>>> GetEvidenciasdeTrabajoPorOrden(int ordenGeneralId)
        {
            var evidencias = await _db.Set<Evidenciaorden>()
                .Where(e => e.OrdenGeneralId == ordenGeneralId)
                .Where(e => e.EvidenciaTrabajo)
                .Where(e => e.Activo)
                .OrderBy(e => e.FechaRegistro)
                .ToListAsync();

            if (!evidencias.Any())
            {
                return NotFound($"No se encontraron evidencias para la orden {ordenGeneralId}");
            }

            return Ok(evidencias);
        }

        [HttpPost("Recepcion")]
        public async Task<ActionResult<List<Evidenciaorden>>> PostEvidencias([FromForm] EvidenciasUploadModel model)
        {
            try
            {
                if (model.Imagenes == null || !model.Imagenes.Any())
                {
                    return BadRequest("No se recibieron imágenes");
                }

                if (model.Descripciones == null || model.Descripciones.Count != model.Imagenes.Count)
                {
                    return BadRequest("El número de descripciones debe coincidir con el número de imágenes");
                }

                // Buscar el trabajo
                var Orden = await _db.Set<OrdenGeneral>()
                    .FirstOrDefaultAsync(t => t.Id == model.OrdenGeneralId);

                // Verificar que la orden existe
                var ordenExiste = await _db.Set<OrdenGeneral>()
                    .AnyAsync(o => o.Id == model.OrdenGeneralId);


                if (!ordenExiste)
                {
                    return NotFound($"La orden {model.OrdenGeneralId} no existe");
                }

                // Crear carpeta específica para esta orden
                string carpetaOrden = Path.Combine(_rutaBaseEvidencias, $"{Orden.NumeroOrden}/Recepcion");
                if (!Directory.Exists(carpetaOrden))
                {
                    Directory.CreateDirectory(carpetaOrden);

                }

                var evidenciasGuardadas = new List<Evidenciaorden>();

                // Procesar cada imagen
                for (int i = 0; i < model.Imagenes.Count; i++)
                {
                    var imagen = model.Imagenes[i];
                    var descripcion = model.Descripciones[i];

                    if (imagen.Length > 0)
                    {
                        // Generar nombre único para el archivo
                        string descripcionLimpia = LimpiarNombreArchivo(descripcion);
                        string extension = Path.GetExtension(imagen.FileName);
                        string nombreArchivo = $"{descripcionLimpia}_{DateTime.Now:dd_HH_mm_ss}{extension}";
                        string rutaCompleta = Path.Combine(carpetaOrden, nombreArchivo);

                        // Guardar archivo físico
                        using (var stream = new FileStream(rutaCompleta, FileMode.Create))
                        {
                            await imagen.CopyToAsync(stream);
                        }

                        // Crear registro en base de datos
                        var evidencia = new Evidenciaorden
                        {
                            OrdenGeneralId = model.OrdenGeneralId,
                            RutaImagen = rutaCompleta,
                            Descripcion = descripcion,
                            FechaRegistro = DateTime.Now,
                            EvidenciaTrabajo = false,
                            Activo = true
                        };

                        _db.Set<Evidenciaorden>().Add(evidencia);
                        evidenciasGuardadas.Add(evidencia);
                    }
                }
                Orden.TieneEvidencia = true;

                await _db.SaveChangesAsync();

                return Ok(new
                {
                    mensaje = "Evidencias guardadas correctamente",
                    cantidad = evidenciasGuardadas.Count,
                    evidencias = evidenciasGuardadas.Select(e => new
                    {
                        e.Id,
                        e.Descripcion,
                        e.RutaImagen,
                        e.FechaRegistro
                    })
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno del servidor: {ex.Message}");
            }
        }

        // GET: api/EvidenciasOrden/5
        [HttpGet("Recepcion/{ordenGeneralId}")]
        public async Task<ActionResult<IEnumerable<Evidenciaorden>>> GetEvidenciasPorOrden(int ordenGeneralId)
        {
            var evidencias = await _db.Set<Evidenciaorden>()
                .Where(e => e.OrdenGeneralId == ordenGeneralId)
                .Where(e => e.Activo)
                .Where(e => e.EvidenciaTrabajo== false)
                .OrderBy(e => e.FechaRegistro)
                .ToListAsync();

            if (!evidencias.Any())
            {
                return NotFound($"No se encontraron evidencias para la orden {ordenGeneralId}");
            }

            return Ok(evidencias);
        }


        // GET: api/EvidenciasOrden/imagen/5
        [HttpGet("imagen/{id}")]
        public async Task<IActionResult> GetImagen(int id)
        {
            var evidencia = await _db.Set<Evidenciaorden>()
                .FirstOrDefaultAsync(e => e.Id == id);

            if (evidencia == null)
            {
                return NotFound("Evidencia no encontrada");
            }

            if (!System.IO.File.Exists(evidencia.RutaImagen))
            {
                return NotFound("Archivo de imagen no encontrado");
            }

            var imagen = System.IO.File.OpenRead(evidencia.RutaImagen);
            var extension = Path.GetExtension(evidencia.RutaImagen).ToLower();
            var mimeType = extension switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".bmp" => "image/bmp",
                _ => "application/octet-stream"
            };

            return File(imagen, mimeType);
        }

        // DELETE: api/EvidenciasOrden/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEvidencia(int id)
        {
            var evidencia = await _db.Set<Evidenciaorden>()
                .FirstOrDefaultAsync(e => e.Id == id);

            if (evidencia == null)
            {
                return NotFound();
            }

            // Eliminar archivo físico
            if (System.IO.File.Exists(evidencia.RutaImagen))
            {
                System.IO.File.Delete(evidencia.RutaImagen);
            }

            evidencia.Activo = false;
            await _db.SaveChangesAsync();

            return Ok(new { mensaje = "Evidencia eliminada correctamente" });
        }

        // Método auxiliar para limpiar nombres de archivo
        private string LimpiarNombreArchivo(string nombre)
        {
            if (string.IsNullOrEmpty(nombre))
                return "evidencia";

            // Reemplazar espacios y caracteres especiales
            var nombreLimpio = nombre.Replace(" ", "_")
                                    .Replace("á", "a")
                                    .Replace("é", "e")
                                    .Replace("í", "i")
                                    .Replace("ó", "o")
                                    .Replace("ú", "u")
                                    .Replace("ñ", "n");

            // Remover caracteres no válidos para nombres de archivo
            var caracteresInvalidos = Path.GetInvalidFileNameChars();
            nombreLimpio = string.Join("_", nombreLimpio.Split(caracteresInvalidos));

            return nombreLimpio;
        }
    }

    // Modelo para recibir los datos del upload
    public class EvidenciasUploadModel
    {
        public int OrdenGeneralId { get; set; }
        public List<IFormFile> Imagenes { get; set; }
        public List<string> Descripciones { get; set; }
    }
}