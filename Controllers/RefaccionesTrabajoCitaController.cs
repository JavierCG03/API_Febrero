using CarSlineAPI.Data;
using CarSlineAPI.Models.DTOs;
using CarSlineAPI.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarSlineAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RefaccionesTrabajoCitaController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<RefaccionesTrabajoCitaController> _logger;

        public RefaccionesTrabajoCitaController(ApplicationDbContext db, ILogger<RefaccionesTrabajoCitaController> logger)
        {
            _db = db;
            _logger = logger;
        }

    }
}
