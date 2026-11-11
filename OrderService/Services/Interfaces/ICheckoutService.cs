
using OrderService.DTOs;

namespace OrderService.Services.Interfaces
{
    public interface ICheckoutService
    {
        Task<OrderDto?> CheckoutAsync(
            Guid cartId,
            CheckoutRequestDto request,
            CancellationToken cancellationToken = default);

        Task<OrderDto?> GetOrderByIdAsync(Guid orderId);
    }
}
