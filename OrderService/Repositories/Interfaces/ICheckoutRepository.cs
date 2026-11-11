
using OrderService.Models;

namespace OrderService.Repositories.Interfaces
{
    public interface ICheckoutRepository
    {
        Task<Cart?> GetCartAsync(Guid cartId);

        Task<Order?> GetOrderByCartIdAsync(Guid cartId);

        Task<Order?> GetOrderByIdAsync(Guid orderId);

        Task AddOrderAsync(Order order);

        Task<int> SaveChangesAsync();
    }
}
