using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using OrderService.Models;

static class OrderManagementChecks
{
    public static async Task Run(IServiceProvider services, string url)
    {
        using var http = new HttpClient { BaseAddress = new Uri(url) };
        var day = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(30);
        Guid id;
        using (var scope = services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
            var cart = new Cart();
            db.Carts.Add(cart);
            var order = new Order
            {
                CartId = cart.Id, CustomerName = "Original customer", Status = OrderStatus.Confirmed,
                Currency = "LKR", TotalPrice = 34000
            };
            order.Bookings.Add(new Booking
            {
                CartItemId = 1, ProductId = 1, SupplierCode = "ABC", SupplierProductId = "ABC-H001",
                SupplierOptionId = "STD", SupplierBookingId = "test-hotel-reference",
                IdempotencyKey = Guid.NewGuid().ToString("N"), Status = BookingStatus.Confirmed,
                CheckIn = day, CheckOut = day.AddDays(2), Adults = 2, Children = 1, Rooms = 1,
                Currency = "LKR", TotalPrice = 24000
            });
            order.Bookings.Add(new Booking
            {
                CartItemId = 2, ProductId = 2, SupplierCode = "CDE", SupplierProductId = "CDE-A101",
                SupplierOptionId = "MORNING", SupplierBookingId = "test-activity-reference",
                IdempotencyKey = Guid.NewGuid().ToString("N"), Status = BookingStatus.Confirmed,
                ActivityDate = day, TimeSlot = "09:00", Adults = 2, Children = 0, Rooms = 1,
                Currency = "LKR", TotalPrice = 10000
            });
            db.Orders.Add(order);
            await db.SaveChangesAsync();
            id = order.Id;
        }

        void Check(bool condition, string description)
        {
            if (!condition) throw new Exception(description);
            Console.WriteLine("PASS " + description);
        }
        async Task<JsonNode> Get() => (await http.GetFromJsonAsync<JsonNode>($"api/orders/{id}"))!;
        var original = await Get();
        var hotel = original["bookings"]![0]!;
        var activity = original["bookings"]![1]!;
        Check((string?)hotel["supplierProductId"] == "ABC-H001" && (string?)hotel["supplierOptionId"] == "STD" &&
            (string?)hotel["checkIn"] == day.ToString("yyyy-MM-dd") &&
            (string?)hotel["checkOut"] == day.AddDays(2).ToString("yyyy-MM-dd") &&
            (int)hotel["adults"]! == 2 && (int)hotel["children"]! == 1 && (int)hotel["rooms"]! == 1 &&
            (string?)hotel["currency"] == "LKR", "order details return the saved hotel selection and currency");
        Check((string?)activity["supplierProductId"] == "CDE-A101" && (string?)activity["supplierOptionId"] == "MORNING" &&
            (string?)activity["activityDate"] == day.ToString("yyyy-MM-dd") && (string?)activity["timeSlot"] == "09:00" &&
            (int)activity["adults"]! == 2 && (int)activity["children"]! == 0 && activity["checkIn"] is null,
            "order details return the saved activity selection and time slot");

        using (var response = await http.PutAsJsonAsync($"api/orders/{id}", new { customerName = "  Updated customer  " }))
        {
            response.EnsureSuccessStatusCode();
            var updated = await response.Content.ReadFromJsonAsync<JsonNode>();
            original["customerName"] = "Updated customer";
            Check(JsonNode.DeepEquals(original, updated), "customer update trims the name and preserves every other order/booking field");
        }
        using (var scope = services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
            Check((await db.Orders.AsNoTracking().SingleAsync(o => o.Id == id)).CustomerName == "Updated customer",
                "customer update persists in SQL Server");
        }
        foreach (var name in new string?[] { null, "", "   ", new('x', 201) })
        {
            using var response = await http.PutAsJsonAsync($"api/orders/{id}", new { customerName = name });
            Check(response.StatusCode == HttpStatusCode.BadRequest && JsonNode.DeepEquals(original, await Get()),
                $"invalid customer name ({(name is null ? "null" : name.Length + " characters")}) returns 400 without changing the order");
        }
        using (var response = await http.PutAsJsonAsync($"api/orders/{Guid.NewGuid()}", new { customerName = "Missing order" }))
            Check(response.StatusCode == HttpStatusCode.NotFound, "updating an unknown order returns 404");

        using (var response = await http.PutAsJsonAsync($"api/orders/{id}", new
        {
            customerName = "Updated customer", status = "Cancelled", totalPrice = 1, bookings = Array.Empty<object>()
        }))
        {
            response.EnsureSuccessStatusCode();
            Check(JsonNode.DeepEquals(original, await Get()), "customer update cannot overwrite statuses, prices or supplier bookings");
        }
    }
}
