
using OrderService.DTOs;

namespace OrderService.Services.Interfaces
{
    public interface ICartService
    {
        Task<CartDto> CreateCartAsync();

        Task<CartDto?> GetCartAsync(Guid cartId);

        Task<CartDto?> AddItemAsync(Guid cartId,AddCartItemDto dto);

        Task<bool> RemoveItemAsync(Guid cartId,int itemId);
        Task<CartQuoteDto?> GetCartQuoteAsync(Guid cartId, CancellationToken cancellationToken = default);
    }
}
