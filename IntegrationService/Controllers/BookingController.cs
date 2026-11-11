
using System.Net;
using IntegrationService.Adapters;
using IntegrationService.Models.Common;
using IntegrationService.Services;
using Microsoft.AspNetCore.Mvc;

namespace IntegrationService.Controllers
{
    [ApiController]
    [Route("api/integration/bookings")]
    public class BookingController : ControllerBase
    {
        private readonly SupplierIntegrationService _service;

        public BookingController(
            SupplierIntegrationService service)
        {
            _service = service;
        }

        [HttpPost]
        public async Task<IActionResult> CreateBooking(
            [FromBody] NormalizedBookingRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                var result = await _service.CreateBookingAsync(
                    request, cancellationToken);

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
            catch (SupplierApiException ex)
            {
                return HandleSupplierError(ex);
            }
            catch (HttpRequestException)
            {
                return StatusCode(502,
                    "Unable to communicate with supplier.");
            }
            catch (OperationCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                return StatusCode(504,
                    "Supplier request timed out.");
            }
        }

        [HttpPost("cancel")]
        public async Task<IActionResult> CancelBooking(
            [FromBody] NormalizedCancellationRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                var result = await _service.CancelBookingAsync(
                    request, cancellationToken);

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
            catch (SupplierApiException ex)
            {
                return HandleSupplierError(ex);
            }
            catch (HttpRequestException)
            {
                return StatusCode(502,
                    "Unable to communicate with supplier.");
            }
            catch (OperationCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                return StatusCode(504,
                    "Supplier request timed out.");
            }
        }

        private IActionResult HandleSupplierError(
            SupplierApiException ex)
        {
            return ex.StatusCode switch
            {
                HttpStatusCode.BadRequest =>
                    BadRequest(ex.Message),

                HttpStatusCode.NotFound =>
                    NotFound(ex.Message),

                HttpStatusCode.Conflict =>
                    Conflict(ex.Message),

                _ => StatusCode(
                    502, "Supplier returned an error.")
            };
        }
    }
}
