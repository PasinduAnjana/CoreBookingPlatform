
using OrderService.Models;

namespace OrderService.Repositories.Interfaces
{
    public interface ICheckoutRepository
    {
        Task<Cart?> GetCartAsync(Guid cartId);

        Task<Order?> GetOrderByCartIdAsync(Guid cartId);

        Task<Order?> GetOrderByIdAsync(Guid orderId);
        Task<bool> UpdateCustomerNameAsync(Guid orderId, string customerName, CancellationToken cancellationToken);
        Task<(List<Order> Items, int TotalCount)> GetOrdersAsync(int page, int pageSize, CancellationToken cancellationToken);

        Task AddOrderAsync(Order order);

        Task<int> SaveChangesAsync();
    }
}
