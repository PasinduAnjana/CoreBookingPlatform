
using OrderService.Models;

namespace OrderService.Repositories.Interfaces
{
    public interface ICartRepository
    {
        Task<Cart?> GetByIdAsync(Guid cartId);

        Task<bool> HasOrderAsync(Guid cartId, CancellationToken cancellationToken = default);

        Task AddAsync(Cart cart);

        void RemoveItem(CartItem item);

        Task<int> SaveChangesAsync();
    }
}
