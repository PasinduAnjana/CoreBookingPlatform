using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProductService.DTOs;
using ProductService.Services.Interfaces;

namespace ProductService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly IProductService _service;

        public ProductController(IProductService service)
        {
            _service = service;
        }
        [HttpGet]
        public async Task<IActionResult> GetAllProducts()
        {
            var products = await _service.GetAllProductsAsync();
            return Ok(products);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetProductById(int id)
        {
            var product = await _service.GetProductByIdAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            return Ok(product);
        }

        [HttpGet("supplier/{supplierId}")]
        public async Task<IActionResult> GetProductsBySupplierId(int supplierId)
        {
            var products = await _service.GetProductsBySupplierIdAsync(supplierId);
            return Ok(products);
        }
        
        [HttpGet("supplier/{supplierId}/product/{supplierProductId}")]
        public async Task<IActionResult> GetSupplierReference(int supplierId, string supplierProductId)
        {
            var product = await _service.GetSupplierReferenceAsync(supplierId, supplierProductId);
            if (product == null)
            {
                return NotFound();
            }
            return Ok(product);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateProduct(int id, [FromBody] UpdateProductDto dto)
        {
            var result = await _service.UpdateProductAsync(id, dto);
            if (!result)
            {
                return NotFound();
            }
            return NoContent();
        }
    }
}
