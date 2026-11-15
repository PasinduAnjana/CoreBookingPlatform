using IntegrationService.Models.Common;
using IntegrationService.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace IntegrationService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [TypeFilter(typeof(SupplierExceptionFilter))]
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

        [HttpGet("suppliers")]
        public ActionResult<List<SupplierInfo>> GetSuppliers([FromServices] SupplierAdapterRegistry registry)
        {
            return Ok(registry.GetSuppliers());
        }

        [HttpGet("products/{supplierCode}")]
        public async Task<IActionResult> GetSupplierProducts(
            string supplierCode,
            CancellationToken cancellationToken)
        {
            var products = await _service.GetProductsAsync(supplierCode, cancellationToken);
            return Ok(products);
        }
    }
}
