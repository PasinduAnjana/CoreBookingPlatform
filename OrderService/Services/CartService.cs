
using OrderService.Clients;
using OrderService.DTOs;
using OrderService.Models;
using OrderService.Repositories.Interfaces;
using OrderService.Services.Interfaces;
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

        public async Task<CartDto?> AddItemAsync(Guid cartId, AddCartItemDto dto,
            CancellationToken cancellationToken = default)
        {
            var cart = await _repository.GetByIdAsync(cartId);

            if (cart == null)
                return null;

            var product = await ValidateProductAsync(dto, cancellationToken);
            await ValidateAvailabilityAsync(dto, product, cancellationToken);

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

        public async Task<CartDto?> UpdateItemAsync(
            Guid cartId,
            int itemId,
            AddCartItemDto dto,
            CancellationToken cancellationToken = default)
        {
            var cart = await _repository.GetByIdAsync(cartId);
            var item = cart?.Items.FirstOrDefault(i => i.Id == itemId);
            if (cart == null || item == null)
                return null;

            if (await _repository.HasOrderAsync(cartId, cancellationToken))
                throw new InvalidOperationException("This cart has already been checked out and cannot be edited.");

            if (dto.ProductId != item.ProductId)
                throw new ArgumentException("An edit cannot change the product. Add a new selection instead.");

            // Validate before changing the tracked item so rejected edits leave it intact.
            var product = await ValidateProductAsync(dto, cancellationToken);
            await ValidateAvailabilityAsync(dto, product, cancellationToken);

            item.SupplierOptionId = dto.SupplierOptionId;
            item.CheckIn = dto.CheckIn;
            item.CheckOut = dto.CheckOut;
            item.ActivityDate = dto.ActivityDate;
            item.TimeSlot = dto.TimeSlot;
            item.Adults = dto.Adults;
            item.Children = dto.Children;
            item.Rooms = dto.Rooms;

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

        private async Task<ProductLookupDto> ValidateProductAsync(
            AddCartItemDto dto, CancellationToken cancellationToken = default)
        {
            var product = await _productClient.GetProductAsync(dto.ProductId, cancellationToken);
            if (product == null) throw new ArgumentException("Product does not exist.");
            BookingSelectionValidator.Validate(dto, product);
            return product;
        }

        private async Task ValidateAvailabilityAsync(AddCartItemDto dto, ProductLookupDto product,
            CancellationToken cancellationToken)
        {
            var availability = await _integrationClient.CheckAvailabilityAsync(new AvailabilityRequestDto
            {
                SupplierCode = product.SupplierCode,
                SupplierProductId = product.SupplierProductId,
                SupplierOptionId = dto.SupplierOptionId,
                CheckIn = dto.CheckIn,
                CheckOut = dto.CheckOut,
                Date = dto.ActivityDate,
                TimeSlot = dto.TimeSlot,
                Adults = dto.Adults,
                Children = dto.Children,
                Rooms = dto.Rooms
            }, cancellationToken);

            if (product.ProductType == "Hotel" && availability.AvailableQuantity < dto.Rooms)
                throw new InvalidOperationException(
                    $"Only {Math.Max(0, availability.AvailableQuantity)} room(s) are available for the selected dates; you requested {dto.Rooms}.");

            if (!availability.Available)
                throw new InvalidOperationException("This selection is no longer available. Choose different dates or options.");
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
