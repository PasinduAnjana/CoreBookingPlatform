using IntegrationService.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace IntegrationService.Controllers
{
    [Route("api/integration/import")]
    [ApiController]
    public class ProductImportController : ControllerBase
    {
        private readonly ProductImportOrchestrator _orchestrator;
        private readonly IWebHostEnvironment _environment;

        public ProductImportController(
            ProductImportOrchestrator orchestrator,
            IWebHostEnvironment environment)
        {
            _orchestrator = orchestrator;
            _environment = environment;
        }

        [HttpPost("{supplierCode}")]
        public async Task<IActionResult> Import(string supplierCode,CancellationToken cancellationToken)
        {
            if (!_environment.IsDevelopment())
                return NotFound();

            if (!supplierCode.Equals("ABC",StringComparison.OrdinalIgnoreCase) &&
                !supplierCode.Equals("CDE",StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest("Unsupported supplier.");
            }

            var result = await _orchestrator.ImportAsync(
                supplierCode,
                cancellationToken);

            return Ok(result);
        }
    }
}
