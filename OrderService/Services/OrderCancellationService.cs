
using Microsoft.EntityFrameworkCore;
using OrderService.Clients;
using OrderService.Data;
using OrderService.DTOs;
using OrderService.Models;
using OrderService.Services.Interfaces;
using SupplierContracts;

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
            return OrderDto.FromOrder(order);

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

                if (result.Status != SupplierBookingStatus.Cancelled ||
                    result.SupplierBookingId != booking.SupplierBookingId)
                {
                    throw new InvalidOperationException(
                        "Supplier did not confirm cancellation.");
                }

                booking.Status = BookingStatus.Cancelled;
                booking.ErrorMessage = null;
            }
            catch (Exception ex) when (
                ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
            {
                // Keep Confirmed so cancellation can be retried.
                // The supplier may already have cancelled it.
                allCancelled = false;
                booking.ErrorMessage = ex is OperationCanceledException
                    ? "Cancellation timed out and needs retry."
                    : $"Cancellation needs retry: {ex.Message}";
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

        return OrderDto.FromOrder(order);
    }
}
