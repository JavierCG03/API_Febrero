using CarSlineAPI.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarSlineAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CatalogosController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<CatalogosController> _logger;

        public CatalogosController(ApplicationDbContext db, ILogger<CatalogosController> logger)
        {
            _db = db;
            _logger = logger;
        }

        [HttpGet("tipos-servicio")]
        public async Task<IActionResult> ObtenerTiposServicio()
        {
            var tipos = await _db.TiposServicio
                .Where(t => t.Activo)
                .Select(t => new { t.Id, Nombre = t.NombreServicio, t.Descripcion, Precio = t.PrecioBase })
                .ToListAsync();

            return Ok(tipos);
        }

        [HttpGet("servicios-frecuentes")]
        public async Task<IActionResult> ObtenerServiciosFrecuentes()
        {
            var servicios = await _db.ServiciosFrecuentes
                .Where(s => s.Activo)
                .OrderBy(s => s.Categoria).ThenBy(s => s.NombreServicio)
                .Select(s => new { s.Id, Nombre = s.NombreServicio, Descripcion = s.Descripcion, Precio = s.Precio, Categoria = s.Categoria })
                .ToListAsync();
             
            return Ok(servicios);
        }
    }
}
