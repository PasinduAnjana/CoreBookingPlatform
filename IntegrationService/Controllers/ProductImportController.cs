using IntegrationService.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SupplierContracts;

namespace IntegrationService.Controllers
{
    [Route("api/integration/import")]
    [ApiController]
    [TypeFilter(typeof(SupplierExceptionFilter))]
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

            try
            {
                var result = await _orchestrator.ImportAsync(supplierCode, cancellationToken);
                return Ok(result);
            }
            catch (KeyNotFoundException)
            {
                return BadRequest(SupplierError.Create(SupplierErrorCode.InvalidRequest, SupplierErrorOutcome.Rejected));
            }
            catch (InvalidOperationException)
            {
                return Conflict(SupplierError.Create(SupplierErrorCode.Conflict, SupplierErrorOutcome.Rejected));
            }
        }
    }
}
