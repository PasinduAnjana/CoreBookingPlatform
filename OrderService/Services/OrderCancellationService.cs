
using Microsoft.EntityFrameworkCore;
using OrderService.Clients;
using OrderService.Data;
using OrderService.DTOs;
using OrderService.Models;
using OrderService.Services.Interfaces;

namespace OrderService.Services;

public class OrderCancellationService : IOrderCancellationService
{
    private readonly OrderDbContext _context;
    private readonly IntegrationServiceClient _integrationClient;

    public OrderCancellationService(
        OrderDbContext context,
        IntegrationServiceClient integrationClient)
    {
        _context = context;
        _integrationClient = integrationClient;
    }

    public async Task<OrderDto?> CancelOrderAsync(
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        var order = await _context.Orders
            .Include(o => o.Bookings)
            .FirstOrDefaultAsync(
                o => o.Id == orderId,
                cancellationToken);

        if (order == null)
            return null;

        // Already cancelled
        if (order.Status == OrderStatus.Cancelled)
            return ToDto(order);

        // Only confirmed orders or previously failed
        // cancellations can be processed.
        if (order.Status != OrderStatus.Confirmed &&
            order.Status != OrderStatus.CancellationFailed)
        {
            throw new InvalidOperationException(
                "This order cannot be cancelled.");
        }

        bool allCancelled = true;

        foreach (var booking in order.Bookings.OrderBy(b => b.Id))
        {
            // Skip bookings that were already cancelled.
            if (booking.Status == BookingStatus.Cancelled)
                continue;

            if (booking.Status != BookingStatus.Confirmed ||
                string.IsNullOrWhiteSpace(booking.SupplierBookingId))
            {
                allCancelled = false;
                booking.ErrorMessage =
                    "Booking has no confirmed supplier reference.";
                continue;
            }

            try
            {
                var result = await _integrationClient
                    .CancelBookingAsync(
                        new SupplierCancellationRequestDto
                        {
                            SupplierCode = booking.SupplierCode,
                            SupplierBookingId =
                                booking.SupplierBookingId
                        },
                        cancellationToken);

                if (!string.Equals(
                    result.Status,
                    "Cancelled",
                    StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        "Supplier did not confirm cancellation.");
                }

                booking.Status = BookingStatus.Cancelled;
                booking.ErrorMessage = null;
            }
            catch (Exception ex) when (
                ex is not OperationCanceledException)
            {
                // Keep Confirmed so cancellation can be retried.
                // The supplier may already have cancelled it.
                allCancelled = false;
                booking.ErrorMessage =
                    $"Cancellation needs retry: {ex.Message}";
            }

            await _context.SaveChangesAsync(cancellationToken);
        }

        order.Status = allCancelled
            ? OrderStatus.Cancelled
            : OrderStatus.CancellationFailed;

        order.ErrorMessage = allCancelled
            ? null
            : "Some supplier bookings could not be cancelled.";

        await _context.SaveChangesAsync(cancellationToken);

        return ToDto(order);
    }

    private static OrderDto ToDto(Order order)
    {
        return new OrderDto
        {
            Id = order.Id,
            CartId = order.CartId,
            CustomerName = order.CustomerName,
            Status = order.Status,
            Currency = order.Currency,
            TotalPrice = order.TotalPrice,
            ErrorMessage = order.ErrorMessage,
            CreatedAt = order.CreatedAt,

            Bookings = order.Bookings
                .OrderBy(b => b.CartItemId)
                .Select(b => new BookingResultDto
                {
                    Id = b.Id,
                    ProductId = b.ProductId,
                    SupplierCode = b.SupplierCode,
                    SupplierBookingId = b.SupplierBookingId,
                    Status = b.Status,
                    TotalPrice = b.TotalPrice,
                    ErrorMessage = b.ErrorMessage
                })
                .ToList()
        };
    }
}
