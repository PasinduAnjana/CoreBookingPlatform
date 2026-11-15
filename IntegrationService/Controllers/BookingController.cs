using IntegrationService.Models.Common;
using IntegrationService.Services;
using Microsoft.AspNetCore.Mvc;

namespace IntegrationService.Controllers;

[ApiController]
[Route("api/integration/bookings")]
[TypeFilter(typeof(SupplierExceptionFilter))]
public class BookingController(SupplierIntegrationService service) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateBooking(
        [FromBody] NormalizedBookingRequest request, CancellationToken cancellationToken)
        => Ok(await service.CreateBookingAsync(request, cancellationToken));

    [HttpPost("cancel")]
    public async Task<IActionResult> CancelBooking(
        [FromBody] NormalizedCancellationRequest request, CancellationToken cancellationToken)
        => Ok(await service.CancelBookingAsync(request, cancellationToken));
}
