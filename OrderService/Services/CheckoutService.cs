
using SupplierContracts;
using OrderService.Clients;
using OrderService.DTOs;
using OrderService.Models;
using OrderService.Repositories.Interfaces;
using OrderService.Services.Interfaces;

namespace OrderService.Services
{
    public class CheckoutService : ICheckoutService
    {
        private readonly ICheckoutRepository _repository;
        private readonly ICartService _cartService;
        private readonly ProductServiceClient _productClient;
        private readonly IntegrationServiceClient _integrationClient;

        public CheckoutService(
            ICheckoutRepository repository,
            ICartService cartService,
            ProductServiceClient productClient,
            IntegrationServiceClient integrationClient)
        {
            _repository = repository;
            _cartService = cartService;
            _productClient = productClient;
            _integrationClient = integrationClient;
        }

        public async Task<OrderDto?> GetOrderByIdAsync(
            Guid orderId)
        {
            var order = await _repository
                .GetOrderByIdAsync(orderId);

            return order == null ? null : OrderDto.FromOrder(order);
        }

        public async Task<OrderDto?> UpdateOrderAsync(
            Guid orderId, UpdateOrderRequestDto request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.CustomerName) || request.CustomerName.Length > 200)
                throw new ArgumentException("Customer name must contain between 1 and 200 characters.");

            var updated = await _repository.UpdateCustomerNameAsync(
                orderId, request.CustomerName.Trim(), cancellationToken);
            return updated ? await GetOrderByIdAsync(orderId) : null;
        }

        public async Task<OrderPageDto> GetOrdersAsync(int page, int pageSize, CancellationToken cancellationToken)
        {
            var result = await _repository.GetOrdersAsync(page, pageSize, cancellationToken);
            return new OrderPageDto
            {
                Items = result.Items.Select(OrderDto.FromOrder).ToList(),
                TotalCount = result.TotalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<OrderDto?> CheckoutAsync(
            Guid cartId,
            CheckoutRequestDto request,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.CustomerName))
                throw new ArgumentException("Customer name is required.");

            // Don't create a second order for the same cart.
            var existing = await _repository
                .GetOrderByCartIdAsync(cartId);

            if (existing != null)
                return OrderDto.FromOrder(existing);

            var cart = await _repository.GetCartAsync(cartId);

            if (cart == null)
                return null;

            if (cart.Items.Count == 0)
                throw new ArgumentException("Cart is empty.");

            // Recheck every item before attempting checkout.
            var quote = await _cartService.GetCartQuoteAsync(
                cartId, cancellationToken);

            if (quote == null ||
                !quote.AllAvailable ||
                !quote.TotalPrice.HasValue ||
                string.IsNullOrWhiteSpace(quote.Currency))
            {
                throw new ArgumentException(
                    "One or more cart items cannot be booked.");
            }

            var order = new Order
            {
                CartId = cartId,
                CustomerName = request.CustomerName,
                Currency = quote.Currency,
                TotalPrice = 0m,
                Status = OrderStatus.Pending
            };

            // Resolve product references and prepare booking snapshots.
            foreach (var item in cart.Items.OrderBy(i => i.Id))
            {
                var product = await _productClient.GetProductAsync(
                    item.ProductId, cancellationToken);

                if (product == null || !product.IsActive)
                    throw new ArgumentException(
                        $"Product {item.ProductId} is unavailable.");

                var itemQuote = quote.Items.Single(
                    q => q.CartItemId == item.Id);

                if (!string.Equals(
                    product.SupplierCode,
                    itemQuote.SupplierCode,
                    StringComparison.OrdinalIgnoreCase))
                {
                    throw new ArgumentException(
                        "Supplier information changed during checkout.");
                }

                order.Bookings.Add(new Booking
                {
                    CartItemId = item.Id,
                    ProductId = item.ProductId,

                    SupplierCode = product.SupplierCode,
                    SupplierProductId = product.SupplierProductId,
                    SupplierOptionId = item.SupplierOptionId,

                    // Stable for this order and cart item.
                    IdempotencyKey =
                        $"order-{order.Id:N}-item-{item.Id}",

                    CheckIn = item.CheckIn,
                    CheckOut = item.CheckOut,
                    ActivityDate = item.ActivityDate,
                    TimeSlot = item.TimeSlot,

                    Adults = item.Adults,
                    Children = item.Children,
                    Rooms = item.Rooms,

                    Currency = itemQuote.Currency,
                    Status = BookingStatus.Pending
                });
            }

            // Save the order BEFORE contacting suppliers.
            await _repository.AddOrderAsync(order);
            await _repository.SaveChangesAsync();

            bool checkoutFailed = false;
            bool outcomeUncertain = false;

            foreach (var booking in order.Bookings.OrderBy(
                b => b.CartItemId))
            {
                booking.Status = BookingStatus.Attempting;

                // Persist this state before the external HTTP request.
                await _repository.SaveChangesAsync();

                try
                {
                    var supplierRequest = new SupplierBookingRequestDto
                    {
                        SupplierCode = booking.SupplierCode,
                        SupplierProductId = booking.SupplierProductId,
                        SupplierOptionId = booking.SupplierOptionId,
                        IdempotencyKey = booking.IdempotencyKey,
                        CustomerName = order.CustomerName,

                        CheckIn = booking.CheckIn,
                        CheckOut = booking.CheckOut,
                        Date = booking.ActivityDate,
                        TimeSlot = booking.TimeSlot,

                        Adults = booking.Adults,
                        Children = booking.Children,
                        Rooms = booking.Rooms
                    };

                    var result = await _integrationClient
                        .CreateBookingAsync(
                            supplierRequest, cancellationToken);

                    booking.SupplierBookingId =
                        result.SupplierBookingId;

                    booking.Status = result.Status switch
                    {
                        SupplierBookingStatus.Confirmed when !string.IsNullOrWhiteSpace(result.SupplierBookingId)
                            => BookingStatus.Confirmed,
                        SupplierBookingStatus.Failed => BookingStatus.Failed,
                        SupplierBookingStatus.Cancelled => BookingStatus.Cancelled,
                        _ => BookingStatus.Unknown
                    };

                    booking.TotalPrice = result.TotalPrice;
                    booking.Currency = result.Currency;

                    if (booking.Status != BookingStatus.Confirmed)
                    {
                        checkoutFailed = true;
                        outcomeUncertain = booking.Status == BookingStatus.Unknown;
                        booking.ErrorMessage = "The supplier did not confirm the booking.";
                    }
                }
                catch (SupplierOperationException ex)
                    when (ex.Error.Outcome == SupplierErrorOutcome.Rejected)
                {
                    // Only a normalized, explicit rejection establishes failure.
                    booking.Status = BookingStatus.Failed;
                    booking.ErrorMessage = ex.Message;
                    checkoutFailed = true;
                }
                catch (Exception ex)
                {
                    // Timeout, lost response or other uncertain outcome.
                    // The supplier may still have created a booking.
                    booking.Status = BookingStatus.Unknown;
                    booking.ErrorMessage = ex.Message;
                    checkoutFailed = true;
                    outcomeUncertain = true;
                }

                await _repository.SaveChangesAsync();

                if (checkoutFailed)
                    break;
            }

            if (checkoutFailed)
            {
                order.Status = OrderStatus.Compensating;
                await _repository.SaveChangesAsync();

                bool compensationIncomplete = false;

                // Cancel bookings whose confirmations are known.
                foreach (var booking in order.Bookings.Where(
                    b => b.Status == BookingStatus.Confirmed))
                {
                    try
                    {
                        var cancelled = await _integrationClient
                            .CancelBookingAsync(
                                new SupplierCancellationRequestDto
                                {
                                    SupplierCode = booking.SupplierCode,
                                    SupplierBookingId =
                                        booking.SupplierBookingId!
                                },
                                CancellationToken.None);

                        if (cancelled.Status != SupplierBookingStatus.Cancelled ||
                            cancelled.SupplierBookingId != booking.SupplierBookingId)
                        {
                            throw new InvalidOperationException(
                                "Supplier did not confirm cancellation.");
                        }

                        booking.Status = BookingStatus.Cancelled;
                    }
                    catch (Exception ex)
                    {
                        compensationIncomplete = true;
                        booking.ErrorMessage =
                            $"Cancellation failed: {ex.Message}";
                    }

                    await _repository.SaveChangesAsync();
                }

                order.Status =
                    outcomeUncertain || compensationIncomplete
                    ? OrderStatus.RequiresAttention
                    : OrderStatus.Failed;

                order.ErrorMessage =
                    "Checkout did not complete successfully.";

                await _repository.SaveChangesAsync();

                return OrderDto.FromOrder(order);
            }

            order.Status = OrderStatus.Confirmed;
            order.TotalPrice = order.Bookings.Sum(
                b => b.TotalPrice);

            await _repository.SaveChangesAsync();

            return OrderDto.FromOrder(order);
        }
    }
}
