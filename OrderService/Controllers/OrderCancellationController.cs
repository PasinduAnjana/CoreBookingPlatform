
using Microsoft.AspNetCore.Mvc;
using OrderService.Models;
using OrderService.Services.Interfaces;

namespace OrderService.Controllers;

[ApiController]
[Route("api/orders")]
public class OrderCancellationController : ControllerBase
{
    private readonly IOrderCancellationService _service;

    public OrderCancellationController(
        IOrderCancellationService service)
    {
        _service = service;
    }

    [HttpPost("{orderId:guid}/cancel")]
    public async Task<IActionResult> Cancel(
        Guid orderId,
        CancellationToken cancellationToken)
    {
        try
        {
            var order = await _service.CancelOrderAsync(
                orderId, cancellationToken);

            if (order == null)
                return NotFound("Order not found.");

            if (order.Status == OrderStatus.CancellationFailed)
                return StatusCode(202, order);

            return Ok(order);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
    }
}
