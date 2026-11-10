using IntegrationService.Adapters.Interfaces;
using IntegrationService.Models.Common;
using IntegrationService.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace IntegrationService.Controllers
{
    [Route("api/integration/availability")]
    [ApiController]
    public class AvailabilityController : ControllerBase
    {
        private readonly SupplierIntegrationService _service;

        public AvailabilityController(SupplierIntegrationService service)
        {
            _service = service;
        }

        [HttpPost]
        public async Task<IActionResult> CheckAvailability(
           [FromBody] NormalizedAvailabilityRequest request,
           CancellationToken cancellationToken)
        {
            try
            {
                var result = await _service.CheckAvailabilityAsync(
                    request,
                    cancellationToken);

                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (HttpRequestException)
            {
                return StatusCode(
                    StatusCodes.Status502BadGateway,
                    "Unable to obtain availability from supplier.");
            }
            catch (OperationCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                return StatusCode(
                    StatusCodes.Status504GatewayTimeout,
                    "Supplier availability request timed out.");
            }
        }
    }
}
