using IntegrationService.Models.Common;
using IntegrationService.Services;
using Microsoft.AspNetCore.Mvc;

namespace IntegrationService.Controllers;

[ApiController]
[Route("api/integration/availability")]
[TypeFilter(typeof(SupplierExceptionFilter))]
public class AvailabilityController(SupplierIntegrationService service) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CheckAvailability(
        [FromBody] NormalizedAvailabilityRequest request, CancellationToken cancellationToken)
        => Ok(await service.CheckAvailabilityAsync(request, cancellationToken));
}
