using System.Net;
using OrderService.Clients;
using OrderService.DTOs;
using OrderService.Models;
using OrderService.Repositories.Interfaces;
using OrderService.Services;
using SupplierContracts;

static class CheckoutChecks
{
    public static async Task Run()
    {
        using (var fixture = new Fixture(_ => Success()))
        {
            var order = await fixture.Checkout();
            Check.That(order.Status == OrderStatus.Confirmed && order.Bookings.All(b => b.Status == BookingStatus.Confirmed),
                "normalized confirmations complete a mixed checkout");
        }

        foreach (var (name, response, orderStatus, bookingStatus) in new (string, Func<HttpResponseMessage>, string, string)[]
        {
            ("explicit rejection", () => Responses.Json(SupplierError.Create(SupplierErrorCode.Conflict, SupplierErrorOutcome.Rejected), HttpStatusCode.Conflict), OrderStatus.Failed, BookingStatus.Failed),
            ("bare HTTP 409", () => new(HttpStatusCode.Conflict) { Content = new StringContent("upstream conflict") }, OrderStatus.RequiresAttention, BookingStatus.Unknown),
            ("uncertain conflict", () => Responses.Json(SupplierError.Create(SupplierErrorCode.Conflict), HttpStatusCode.Conflict), OrderStatus.RequiresAttention, BookingStatus.Unknown),
            ("timeout", () => Responses.Json(SupplierError.Create(SupplierErrorCode.Timeout), HttpStatusCode.GatewayTimeout), OrderStatus.RequiresAttention, BookingStatus.Unknown),
            ("malformed success", () => Responses.Json("bad booking response"), OrderStatus.RequiresAttention, BookingStatus.Unknown),
            ("numeric status", () => Responses.Json(new { status = 2, supplierBookingId = "reference" }), OrderStatus.RequiresAttention, BookingStatus.Unknown),
            ("unknown status", () => Responses.Json(new { status = "Unknown", supplierBookingId = "possibly-created" }), OrderStatus.RequiresAttention, BookingStatus.Unknown),
            ("pending status", () => Responses.Json(new { status = "Pending", supplierBookingId = "pending-reference" }), OrderStatus.RequiresAttention, BookingStatus.Unknown),
            ("failed status without reference", () => Responses.Json(new { status = "Failed" }), OrderStatus.Failed, BookingStatus.Failed),
            ("cancelled status", () => Responses.Json(new { status = "Cancelled", supplierBookingId = "cancelled-reference" }), OrderStatus.Failed, BookingStatus.Cancelled),
            ("confirmation without reference", () => Responses.Json(new { status = "Confirmed" }), OrderStatus.RequiresAttention, BookingStatus.Unknown)
        })
        {
            using var fixture = new Fixture(call => call == 1 ? Success() : response());
            var order = await fixture.Checkout();
            Check.That(order.Status == orderStatus && order.Bookings[1].Status == bookingStatus &&
                order.Bookings[0].Status == BookingStatus.Cancelled && fixture.CancellationCalls == 1,
                $"{name} preserves the correct outcome and compensates the first booking");
            var retry = await fixture.Checkout();
            Check.That(retry.Id == order.Id && fixture.BookingCalls == 2, $"{name} checkout retry does not book again");
            if (name == "unknown status")
                Check.That(fixture.Store.Order!.Bookings.Single(b => b.CartItemId == 2).SupplierBookingId == "possibly-created",
                    "uncertain bookings retain supplier references for reconciliation");
        }

        using (var fixture = new Fixture(call => call == 1 ? Success() : Responses.Json(
            SupplierError.Create(SupplierErrorCode.Conflict, SupplierErrorOutcome.Rejected), HttpStatusCode.Conflict),
            () => Responses.Json(new { status = "Unknown", supplierBookingId = "reference" })))
        {
            var order = await fixture.Checkout();
            Check.That(order.Status == OrderStatus.RequiresAttention && order.Bookings[0].Status == BookingStatus.Confirmed,
                "unconfirmed cancellation never claims successful compensation");
        }
    }

    private static HttpResponseMessage Success() => Responses.Json(new
    { status = "Confirmed", supplierBookingId = "reference", currency = "LKR", totalPrice = 100m });

    private sealed class Fixture : IDisposable
    {
        private readonly HttpClient products;
        private readonly HttpClient integration;
        private readonly CheckoutService checkout;
        public MemoryStore Store { get; } = new();
        public int BookingCalls { get; private set; }
        public int CancellationCalls { get; private set; }

        public Fixture(Func<int, HttpResponseMessage> book, Func<HttpResponseMessage>? cancel = null)
        {
            products = Responses.Client(request =>
            {
                var id = int.Parse(request.RequestUri!.Segments.Last());
                return Responses.Json(new { id, name = "Test", isActive = true, supplierCode = id == 1 ? "ABC" : "CDE", supplierProductId = "product" });
            });
            integration = Responses.Client(request =>
            {
                if (request.RequestUri!.AbsolutePath.EndsWith("/availability"))
                    return Responses.Json(new { available = true, availableQuantity = 10, currency = "LKR", totalPrice = 100m });
                if (request.RequestUri.AbsolutePath.EndsWith("/cancel"))
                {
                    CancellationCalls++;
                    return cancel?.Invoke() ?? Responses.Json(new { status = "Cancelled", supplierBookingId = "reference" });
                }
                return book(++BookingCalls);
            });
            var productClient = new ProductServiceClient(products);
            var integrationClient = new IntegrationServiceClient(integration);
            var cartService = new CartService(Store, productClient, integrationClient);
            checkout = new CheckoutService(Store, cartService, productClient, integrationClient);
        }

        public async Task<OrderDto> Checkout() => await checkout.CheckoutAsync(Store.Cart.Id, new() { CustomerName = "Test" })
            ?? throw new Exception("Missing order");

        public void Dispose() { products.Dispose(); integration.Dispose(); }
    }

    public sealed class MemoryStore : ICartRepository, ICheckoutRepository
    {
        public Cart Cart { get; } = new()
        {
            Items = new List<CartItem>
            {
                new() { Id = 1, ProductId = 1, SupplierOptionId = "room", Adults = 1, Rooms = 1 },
                new() { Id = 2, ProductId = 2, SupplierOptionId = "package", Adults = 1, Rooms = 1 }
            }
        };
        public Order? Order { get; private set; }
        public Task<Cart?> GetByIdAsync(Guid id) => Task.FromResult<Cart?>(id == Cart.Id ? Cart : null);
        public Task<Cart?> GetCartAsync(Guid id) => GetByIdAsync(id);
        public Task<Order?> GetOrderByCartIdAsync(Guid id) => Task.FromResult(Order?.CartId == id ? Order : null);
        public Task<Order?> GetOrderByIdAsync(Guid id) => Task.FromResult(Order?.Id == id ? Order : null);
        public Task<bool> UpdateCustomerNameAsync(Guid id, string name, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (Order?.Id != id) return Task.FromResult(false);
            Order.CustomerName = name;
            return Task.FromResult(true);
        }
        public Task<bool> HasOrderAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(Order?.CartId == id);
        public Task AddOrderAsync(Order order) { Order = order; return Task.CompletedTask; }
        public Task<int> SaveChangesAsync() => Task.FromResult(1);
        public Task<(List<Order>, int)> GetOrdersAsync(int page, int pageSize, CancellationToken cancellationToken)
            => Task.FromResult((Order is null ? new List<Order>() : new List<Order> { Order }, Order is null ? 0 : 1));
        public Task AddAsync(Cart cart) => throw new NotSupportedException();
        public void RemoveItem(CartItem item) => throw new NotSupportedException();
    }
}
