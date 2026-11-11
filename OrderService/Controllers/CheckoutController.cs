
using Microsoft.AspNetCore.Mvc;
using OrderService.DTOs;
using OrderService.Services.Interfaces;

namespace OrderService.Controllers
{
    [ApiController]
    [Route("api")]
    public class CheckoutController : ControllerBase
    {
        private readonly ICheckoutService _service;

        public CheckoutController(ICheckoutService service)
        {
            _service = service;
        }

        [HttpPost("carts/{cartId:guid}/checkout")]
        public async Task<IActionResult> Checkout(
            Guid cartId,
            [FromBody] CheckoutRequestDto request,
            CancellationToken cancellationToken)
        {
            try
            {
                var order = await _service.CheckoutAsync(
                    cartId, request, cancellationToken);

                if (order == null)
                    return NotFound("Cart not found.");

                return order.Status switch
                {
                    "Confirmed" => Ok(order),
                    "Failed" => Conflict(order),
                    "RequiresAttention" => StatusCode(202, order),
                    _ => StatusCode(202, order)
                };
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (HttpRequestException)
            {
                return StatusCode(502,
                    "Unable to validate cart with downstream services.");
            }
            catch (OperationCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                return StatusCode(504,
                    "Downstream service timed out.");
            }
        }

        [HttpGet("orders/{orderId:guid}")]
        public async Task<IActionResult> GetOrder(Guid orderId)
        {
            var order = await _service.GetOrderByIdAsync(orderId);

            if (order == null)
                return NotFound();

            return Ok(order);
        }
    }
}

