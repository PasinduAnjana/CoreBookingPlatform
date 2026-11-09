using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProductService.DTOs;
using ProductService.Services.Interfaces;

namespace ProductService.Controllers
{
    [Route("api/internal/products")]
    [ApiController]
    public class ProductImportController : ControllerBase
    {
        private readonly IProductImportService _service;
        private readonly IWebHostEnvironment _environment;

        public ProductImportController(
            IProductImportService service,
            IWebHostEnvironment webHostEnvironment)
        {
            _service = service;
            _environment = webHostEnvironment;
        }

        [HttpPost("import")]
        public async Task<IActionResult> ImportProduct([FromBody] ImportProductDto dto)
        {
            if (!_environment.IsDevelopment())
            {
                return NotFound();
            }

            try
            {
                var result = await _service.ImportAsync(dto);
                return Ok(result);

            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }

}