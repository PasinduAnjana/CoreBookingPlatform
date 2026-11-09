using IntegrationService.Models.Common;
using IntegrationService.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace IntegrationService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class IntegrationController : ControllerBase
    {
        private readonly SupplierIntegrationService _service;
        public IntegrationController(SupplierIntegrationService service)
        {
            _service = service;
        }

        [HttpGet("products")]
        public async Task<ActionResult<IEnumerable<NormalizedProduct>>> GetAllProducts(CancellationToken cancellationToken)
        {
            var products = await _service.GetAllProductsAsync(cancellationToken);
            return Ok(products);
        }

        [HttpGet("products/{supplierCode}")]
        public async Task<IActionResult> GetSupplierProducts(
            string supplierCode,
            CancellationToken cancellationToken)
        {
            try
            {
                var products = await _service.GetProductsAsync(
                    supplierCode,
                    cancellationToken);

                return Ok(products);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }
    }
}
