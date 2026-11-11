
using OrderService.DTOs;

namespace OrderService.Services.Interfaces;

public interface IOrderCancellationService
{
    Task<OrderDto?> CancelOrderAsync(
        Guid orderId,
        CancellationToken cancellationToken = default);
}
