using System.Net;
using System.Net.Http.Json;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using OrderService.Clients;
using OrderService.Data;
using OrderService.Models;
using OrderService.Services;
using ProductService.Controllers;
using ProductService.Data;
using ProductService.DTOs;
using ProductService.Services.Interfaces;

static class ErrorHandlingChecks
{
    public static async Task Run(IServiceProvider services)
    {
        using (var scope = services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
            var order = await CreateOrder(db);
            using var cancellation = new CancellationTokenSource();
            using var http = Client(token =>
            {
                cancellation.Cancel();
                if (!token.IsCancellationRequested) throw new Exception("Cancellation was not propagated");
                throw new OperationCanceledException(token);
            });
            try
            {
                await new OrderCancellationService(db, new IntegrationServiceClient(http))
                    .CancelOrderAsync(order.Id, cancellation.Token);
                throw new Exception("Caller cancellation was swallowed");
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
            if (order.Status != OrderStatus.Confirmed || order.Bookings.Single().ErrorMessage is not null)
                throw new Exception("Caller cancellation was treated as a supplier failure");
            Console.WriteLine("PASS caller cancellation propagates without changing booking status or errors");
        }

        using (var scope = services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
            var order = await CreateOrder(db);
            using var timeout = Client(_ => throw new TaskCanceledException("Downstream timeout"));
            var result = await new OrderCancellationService(db, new IntegrationServiceClient(timeout)).CancelOrderAsync(order.Id);
            db.ChangeTracker.Clear();
            var saved = await db.Orders.Include(o => o.Bookings).SingleAsync(o => o.Id == order.Id);
            if (result?.Status != OrderStatus.CancellationFailed || saved.Status != OrderStatus.CancellationFailed ||
                saved.Bookings.Single().Status != BookingStatus.Confirmed ||
                saved.Bookings.Single().ErrorMessage?.Contains("timed out") != true)
                throw new Exception("Cancellation timeout did not persist a retryable state");
            Console.WriteLine("PASS downstream cancellation timeout persists CancellationFailed with a retryable booking");

            using var success = Client(_ => new(HttpStatusCode.OK)
            { Content = JsonContent.Create(new { status = "Cancelled", supplierBookingId = "error-check-reference" }) });
            result = await new OrderCancellationService(db, new IntegrationServiceClient(success)).CancelOrderAsync(order.Id);
            if (result?.Status != OrderStatus.Cancelled || result.Bookings.Single().ErrorMessage is not null)
                throw new Exception("Cancellation could not be retried after a timeout");
            Console.WriteLine("PASS cancellation retry after a timeout succeeds and clears the booking error");
        }

        DbUpdateException? foreignKeyFailure = null;
        using (var scope = services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ProductDbContext>();
            var categoryId = await db.Products.Where(p => p.CategoryId != null).Select(p => p.CategoryId!.Value).FirstAsync();
            var category = await db.Categories.FindAsync(categoryId) ?? throw new Exception("Missing fixture category");
            db.Categories.Remove(category);
            try { await db.SaveChangesAsync(); }
            catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 547 }) { foreignKeyFailure = ex; }
        }
        if (foreignKeyFailure is null) throw new Exception("Expected SQL Server to reject deletion of a referenced category");
        var controller = new CategoryController(new FailedCategoryDeletion(foreignKeyFailure));
        if (await controller.DeleteCategory(1) is not Microsoft.AspNetCore.Mvc.ConflictObjectResult)
            throw new Exception("SQL foreign-key conflict did not produce HTTP 409");
        Console.WriteLine("PASS category deletion maps a real SQL foreign-key violation to HTTP 409");

        var databaseFailure = new DbUpdateException("Unrelated database failure");
        try
        {
            await new CategoryController(new FailedCategoryDeletion(databaseFailure)).DeleteCategory(1);
            throw new Exception("Unrelated database failure was misclassified as an assigned category");
        }
        catch (DbUpdateException ex) when (ReferenceEquals(ex, databaseFailure)) { }
        Console.WriteLine("PASS unrelated category database failures propagate to the centralized handler");
    }

    private static async Task<Order> CreateOrder(OrderDbContext db)
    {
        var cart = new Cart();
        db.Carts.Add(cart);
        var order = new Order { CartId = cart.Id, CustomerName = "Exception check", Status = OrderStatus.Confirmed };
        order.Bookings.Add(new Booking
        {
            ProductId = 1, SupplierCode = "ABC", SupplierProductId = "error-check-product", SupplierOptionId = "STD",
            SupplierBookingId = "error-check-reference", IdempotencyKey = Guid.NewGuid().ToString("N"), Status = BookingStatus.Confirmed
        });
        db.Orders.Add(order);
        await db.SaveChangesAsync();
        return order;
    }

    private static HttpClient Client(Func<CancellationToken, HttpResponseMessage> respond)
        => new(new Replies(respond)) { BaseAddress = new Uri("http://fixture/") };

    private sealed class Replies(Func<CancellationToken, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
            => Task.FromResult(respond(token));
    }

    private sealed class FailedCategoryDeletion(Exception failure) : ICategoryService
    {
        public Task<bool> DeleteCategoryAsync(int id) => Task.FromException<bool>(failure);
        public Task<List<CategoryDto>> GetAllCategoriesAsync() => throw new NotSupportedException();
        public Task<CategoryDto?> GetCategoryByIdAsync(int id) => throw new NotSupportedException();
        public Task<CategoryDto> CreateCategoryAsync(CreateCategoryDto category) => throw new NotSupportedException();
        public Task<CategoryDto?> UpdateCategoryAsync(int id, UpdateCategoryDto category) => throw new NotSupportedException();
    }
}
