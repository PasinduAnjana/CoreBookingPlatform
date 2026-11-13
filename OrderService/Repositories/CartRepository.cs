using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using OrderService.Models;
using OrderService.Repositories.Interfaces;

namespace OrderService.Repositories
{
    public class CartRepository : ICartRepository
    {
        private readonly OrderDbContext _context;

        public CartRepository(OrderDbContext context)
        {
            _context = context;
        }

        public async Task<Cart?> GetByIdAsync(Guid cartId)
        {
            return await _context.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.Id == cartId);
        }

        public async Task AddAsync(Cart cart)
        {
            await _context.Carts.AddAsync(cart);
        }

        public Task<bool> HasOrderAsync(Guid cartId, CancellationToken cancellationToken = default)
        {
            return _context.Orders.AnyAsync(o => o.CartId == cartId, cancellationToken);
        }

        public void RemoveItem(CartItem item)
        {
            _context.CartItems.Remove(item);
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }
    }
}
