
using OrderService.Models;

namespace OrderService.Repositories.Interfaces
{
    public interface ICartRepository
    {
        Task<Cart?> GetByIdAsync(Guid cartId);

        Task AddAsync(Cart cart);

        void RemoveItem(CartItem item);

        Task<int> SaveChangesAsync();
    }
}
