
using OrderService.Clients;
using OrderService.DTOs;
using OrderService.Models;
using OrderService.Repositories.Interfaces;
using OrderService.Services.Interfaces;
using System.Text.Json;
using static OrderService.Dtos.ExternalServiceDtos;

namespace OrderService.Services
{
    public class CartService : ICartService
    {
        private readonly ICartRepository _repository;
        private readonly ProductServiceClient _productClient;
        private readonly IntegrationServiceClient _integrationClient;

        public CartService(
            ICartRepository repository,
            ProductServiceClient productClient,
            IntegrationServiceClient integrationClient)
        {
            _repository = repository;
            _productClient = productClient;
            _integrationClient = integrationClient;
        }

        private static CartDto ToDto(Cart cart)
        {
            return new CartDto
            {
                Id = cart.Id,
                CreatedAt = cart.CreatedAt,

                Items = cart.Items
                    .OrderBy(i => i.Id)
                    .Select(i => new CartItemDto
                    {
                        Id = i.Id,
                        ProductId = i.ProductId,
                        SupplierOptionId = i.SupplierOptionId,
                        CheckIn = i.CheckIn,
                        CheckOut = i.CheckOut,
                        ActivityDate = i.ActivityDate,
                        TimeSlot = i.TimeSlot,
                        Adults = i.Adults,
                        Children = i.Children,
                        Rooms = i.Rooms
                    })
                    .ToList()
            };
        }

        public async Task<CartDto> CreateCartAsync()
        {
            var cart = new Cart();

            await _repository.AddAsync(cart);
            await _repository.SaveChangesAsync();

            return ToDto(cart);
        }

        public async Task<CartDto?> GetCartAsync(Guid cartId)
        {
            var cart = await _repository.GetByIdAsync(cartId);

            return cart == null ? null : ToDto(cart);
        }

        public async Task<CartDto?> AddItemAsync(Guid cartId,AddCartItemDto dto)
        {
            var cart = await _repository.GetByIdAsync(cartId);

            if (cart == null)
                return null;

            ValidateBookingSelection(dto);
            await ValidateProductAsync(dto);


            var item = new CartItem
            {
                ProductId = dto.ProductId,
                SupplierOptionId = dto.SupplierOptionId,
                CheckIn = dto.CheckIn,
                CheckOut = dto.CheckOut,
                ActivityDate = dto.ActivityDate,
                TimeSlot = dto.TimeSlot,
                Adults = dto.Adults,
                Children = dto.Children,
                Rooms = dto.Rooms
            };

            cart.Items.Add(item);

            await _repository.SaveChangesAsync();

            return ToDto(cart);
        }

        public async Task<bool> RemoveItemAsync(
            Guid cartId,
            int itemId)
        {
            var cart = await _repository.GetByIdAsync(cartId);

            if (cart == null)
                return false;

            var item = cart.Items.FirstOrDefault(
                i => i.Id == itemId);

            if (item == null)
                return false;

            _repository.RemoveItem(item);

            await _repository.SaveChangesAsync();

            return true;
        }

        private static void ValidateBookingSelection(AddCartItemDto dto)
        {
            if (dto.ProductId <= 0 || string.IsNullOrWhiteSpace(dto.SupplierOptionId))
            {
                throw new ArgumentException(
                    "Product and supplier option are required.");
            }

            if (dto.Adults < 0 || dto.Children < 0 ||
                dto.Adults + dto.Children < 1 ||
                dto.Rooms < 1)
            {
                throw new ArgumentException(
                    "Invalid guest or room count.");
            }

            bool hasHotelDates = dto.CheckIn.HasValue || dto.CheckOut.HasValue;

            bool hasActivityDetails =dto.ActivityDate.HasValue || !string.IsNullOrWhiteSpace(dto.TimeSlot);

            if (hasHotelDates == hasActivityDetails)
            {
                throw new ArgumentException("Specify either hotel dates or activity details.");
            }

            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            if (hasHotelDates)
            {
                if (!dto.CheckIn.HasValue ||
                    !dto.CheckOut.HasValue ||
                    dto.CheckIn.Value < today ||
                    dto.CheckOut.Value <= dto.CheckIn.Value ||
                    dto.Adults < dto.Rooms)
                {
                    throw new ArgumentException("Invalid hotel booking selection.");
                }
            }
            else
            {
                if (!dto.ActivityDate.HasValue ||
                    dto.ActivityDate.Value < today ||
                    string.IsNullOrWhiteSpace(dto.TimeSlot) ||
                    dto.Rooms != 1)
                {
                    throw new ArgumentException(
                        "Invalid activity booking selection.");
                }
            }
        }


        private async Task<ProductLookupDto> ValidateProductAsync(AddCartItemDto dto)
        {
            var product = await _productClient.GetProductAsync(dto.ProductId);

            if (product == null)
                throw new ArgumentException("Product does not exist.");

            if (!product.IsActive)
                throw new ArgumentException("Product is inactive.");

            var supplier = product.SupplierCode.ToUpperInvariant();

            if (supplier == "ABC" && (!dto.CheckIn.HasValue || !dto.CheckOut.HasValue))
            {
                throw new ArgumentException(
                    "ABC hotel requires check-in and check-out.");
            }

            if (supplier == "CDE" &&
                (!dto.ActivityDate.HasValue ||
                 string.IsNullOrWhiteSpace(dto.TimeSlot)))
            {
                throw new ArgumentException(
                    "CDE activity requires a date and time slot.");
            }

            if (supplier != "ABC" && supplier != "CDE")
                throw new ArgumentException("Unsupported supplier.");

            if (string.IsNullOrWhiteSpace(product.AttributesJson))
                throw new ArgumentException("Product has no options.");

            using var document = JsonDocument.Parse(product.AttributesJson);

            var root = document.RootElement;

            if (!root.TryGetProperty("options", out var options) ||
                options.ValueKind != JsonValueKind.Array)
            {
                throw new ArgumentException(
                    "Product options are missing.");
            }

            bool optionExists = options.EnumerateArray().Any(option =>
                option.ValueKind == JsonValueKind.Object &&
                (option.TryGetProperty("supplierOptionId", out var id) ||
                 (supplier == "CDE" && option.TryGetProperty("supplierPackageId", out id))) &&
                id.ValueKind == JsonValueKind.String &&
                id.GetString() == dto.SupplierOptionId);

            if (!optionExists)
                throw new ArgumentException(
                    "Selected option does not exist.");

            return product;
        }

        public async Task<CartQuoteDto?> GetCartQuoteAsync(Guid cartId,CancellationToken cancellationToken = default)
        {
            var cart = await _repository.GetByIdAsync(cartId);

            if (cart == null)
                return null;

            var quote = new CartQuoteDto
            {
                CartId = cartId,
                QuotedAt = DateTimeOffset.UtcNow
            };

            foreach (var item in cart.Items.OrderBy(i => i.Id))
            {
                var product = await _productClient.GetProductAsync(
                    item.ProductId,
                    cancellationToken);

                if (product == null || !product.IsActive)
                {
                    throw new InvalidOperationException($"Product {item.ProductId} is unavailable.");
                }

                var request = new AvailabilityRequestDto
                {
                    SupplierCode = product.SupplierCode,
                    SupplierProductId = product.SupplierProductId,
                    SupplierOptionId = item.SupplierOptionId,

                    CheckIn = item.CheckIn,
                    CheckOut = item.CheckOut,

                    Date = item.ActivityDate,
                    TimeSlot = item.TimeSlot,

                    Adults = item.Adults,
                    Children = item.Children,
                    Rooms = item.Rooms
                };

                var availability = await _integrationClient.CheckAvailabilityAsync(request, cancellationToken);

                quote.Items.Add(new CartItemQuoteDto
                {
                    CartItemId = item.Id,
                    ProductId = item.ProductId,
                    ProductName = product.Name,

                    SupplierCode = product.SupplierCode,
                    SupplierOptionId = item.SupplierOptionId,

                    Available = availability.Available,
                    AvailableQuantity = availability.AvailableQuantity,

                    Currency = availability.Currency,
                    TotalPrice = availability.TotalPrice
                });
            }

            quote.AllAvailable =
                quote.Items.Count > 0 &&
                quote.Items.All(i => i.Available);

            var currencies = quote.Items
                .Select(i => i.Currency)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            // Only sum prices when every item is available
            // and all prices use the same currency.
            if (quote.AllAvailable &&
                currencies.Count == 1 &&
                !string.IsNullOrWhiteSpace(currencies[0]))
            {
                quote.Currency = currencies[0];
                quote.TotalPrice = quote.Items.Sum(i => i.TotalPrice);
            }

            return quote;
        }


    }
}
