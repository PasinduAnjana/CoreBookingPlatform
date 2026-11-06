using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProductService.DTOs;
using ProductService.Services;
using ProductService.Services.Interfaces;

namespace ProductService.Controllers
{
    [Route("api/products/{productId:int}/content")]
    [ApiController]
    public class ProductContentController : ControllerBase
    {
        private readonly IProductContentService _service;

        public ProductContentController(IProductContentService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetContent(
            int productId)
        {
            var content = await _service.GetContentAsync(productId);

            if (content == null)
                return NotFound();

            return Ok(content);
        }

        [HttpPut]
        public async Task<IActionResult> UpsertContent(
            int productId,
            [FromBody] UpdateProductContentDto dto)
        {
            var updated = await _service
                .UpsertContentAsync(productId, dto);

            if (!updated)
                return NotFound("Product not found.");

            return NoContent();
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteContent(
            int productId)
        {
            var deleted = await _service.DeleteContentAsync(productId);

            if (!deleted)
                return NotFound();

            return NoContent();
        }
    }
}
