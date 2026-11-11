
using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using OrderService.Models;
using OrderService.Repositories.Interfaces;

namespace OrderService.Repositories
{
    public class CheckoutRepository : ICheckoutRepository
    {
        private readonly OrderDbContext _context;

        public CheckoutRepository(OrderDbContext context)
        {
            _context = context;
        }

        public async Task<Cart?> GetCartAsync(Guid cartId)
        {
            return await _context.Carts
                .AsNoTracking()
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.Id == cartId);
        }

        public async Task<Order?> GetOrderByCartIdAsync(
            Guid cartId)
        {
            return await _context.Orders
                .AsNoTracking()
                .Include(o => o.Bookings)
                .FirstOrDefaultAsync(o => o.CartId == cartId);
        }

        public async Task<Order?> GetOrderByIdAsync(
            Guid orderId)
        {
            return await _context.Orders
                .AsNoTracking()
                .Include(o => o.Bookings)
                .FirstOrDefaultAsync(o => o.Id == orderId);
        }

        public async Task AddOrderAsync(Order order)
        {
            await _context.Orders.AddAsync(order);
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }
    }
}
