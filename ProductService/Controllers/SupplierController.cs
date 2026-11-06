using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProductService.Services.Interfaces;

namespace ProductService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SupplierController : ControllerBase
    {
        private readonly ISupplierService _supplierService;

        public SupplierController(ISupplierService supplierService)
        {
            _supplierService = supplierService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllSuppliers()
        {
            var suppliers = await _supplierService.GetAllSuppliersAsync();
            return Ok(suppliers);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetSupplierById([FromRoute] int id)
        {
            var supplier = await _supplierService.GetSupplierByIdAsync(id);
            if (supplier == null)
            {
                return NotFound();
            }
            return Ok(supplier);
        }

        [HttpGet("code/{code}")]
        public async Task<IActionResult> GetSupplierByCode([FromRoute] string code)
        {
            var supplier = await _supplierService.GetSupplierByCodeAsync(code);
            if (supplier == null)
            {
                return NotFound();
            }
            return Ok(supplier);
        }
    }
}
