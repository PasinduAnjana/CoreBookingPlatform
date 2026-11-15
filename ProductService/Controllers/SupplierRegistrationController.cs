using Microsoft.AspNetCore.Mvc;
using ProductService.DTOs;
using ProductService.Services.Interfaces;

namespace ProductService.Controllers
{
    [ApiController]
    [Route("api/internal/suppliers")]
    public class SupplierRegistrationController : ControllerBase
    {
        private readonly ISupplierService _service;
        private readonly IWebHostEnvironment _environment;

        public SupplierRegistrationController(ISupplierService service, IWebHostEnvironment environment)
        {
            _service = service;
            _environment = environment;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterSupplierDto dto)
        {
            if (!_environment.IsDevelopment()) return NotFound();
            try { return Ok(await _service.RegisterAsync(dto)); }
            catch (ArgumentException ex) { return BadRequest(ex.Message); }
            catch (InvalidOperationException ex) { return Conflict(ex.Message); }
        }
    }
}
