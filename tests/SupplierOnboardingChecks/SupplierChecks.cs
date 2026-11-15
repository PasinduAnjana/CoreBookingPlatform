using System.Net.Http.Json;
using System.Text.Json.Nodes;
using IntegrationService.Services;
using Microsoft.EntityFrameworkCore;
using ProductService.Data;

static class SupplierChecks
{
    public static async Task Run(IServiceProvider services, string url)
    {
        using var http = new HttpClient { BaseAddress = new Uri(url) };
        var checks = 0;
        void Check(bool condition, string description)
        {
            if (!condition) throw new Exception(description);
            checks++;
            Console.WriteLine("PASS " + description);
        }
        async Task<JsonNode> Request(string path, HttpMethod? method = null, object? body = null, int status = 200)
        {
            using var request = new HttpRequestMessage(method ?? HttpMethod.Get, path);
            if (body != null) request.Content = JsonContent.Create(body);
            using var response = await http.SendAsync(request);
            var text = await response.Content.ReadAsStringAsync();
            if ((int)response.StatusCode != status)
                throw new Exception($"{request.Method} {path}: expected {status}, got {(int)response.StatusCode}: {text}");
            if (string.IsNullOrEmpty(text)) return new JsonObject();
            return response.Content.Headers.ContentType?.MediaType == "application/json"
                ? JsonNode.Parse(text) ?? new JsonObject()
                : JsonValue.Create(text)!;
        }
        var suppliers = (await Request("api/integration/suppliers")).AsArray();
        Check(suppliers.Select(s => (string?)s!["code"]).SequenceEqual(new[] { "ABC", "CDE", "XYZ" }), "registry discovers the third supplier before its first import");
        using (var scope = services.CreateScope())
        {
            var adapters = scope.ServiceProvider.GetRequiredService<SupplierAdapterRegistry>().Adapters.ToList();
            try { _ = new SupplierAdapterRegistry(adapters.Concat([adapters[0]])); throw new Exception("Duplicate accepted"); }
            catch (InvalidOperationException) { Check(true, "duplicate adapter codes are rejected"); }
        }
        await Request("api/integration/import/UNKNOWN", HttpMethod.Post, status: 400);
        foreach (var code in new[] { "ABC", "CDE", "XYZ" })
        {
            var imported = await Request("api/integration/import/" + code, HttpMethod.Post);
            Check((int)imported["failed"]! == 0 && (int)imported["created"]! == 2, code + " catalog imports without supplier-specific core changes");
        }
        var xyzSupplier = await Request("api/supplier/code/XYZ");
        var xyzId = (int)xyzSupplier["id"]!;
        var catalog = (await Request("api/product")).AsArray();
        var xyzHotel = catalog.Single(p => (string?)p!["supplierCode"] == "XYZ" && (string?)p["supplierProductId"] == "ABC-H001")!;
        var abcHotel = catalog.Single(p => (string?)p!["supplierCode"] == "ABC" && (string?)p["supplierProductId"] == "ABC-H001")!;
        var cdeActivity = catalog.Single(p => (string?)p!["supplierCode"] == "CDE" && (string?)p["supplierProductId"] == "CDE-A101")!;
        var hotelId = (int)xyzHotel["id"]!;
        Check((string?)xyzHotel["productType"] == "Hotel" && (string?)cdeActivity["productType"] == "Activity", "product types are independent of supplier codes");
        await Request("api/internal/products/import", HttpMethod.Post, new {
            supplierCode = "XYZ", supplierProductId = "bad-type", name = "Invalid type", productType = "Flight", attributes = new { }
        }, 400);
        await Request("api/internal/products/import", HttpMethod.Post, new {
            supplierCode = "XYZ", supplierProductId = "ABC-H001", name = "Wrong type", productType = "Activity", attributes = new { }
        }, 400);
        Check(true, "unsupported types and changes to existing product types are rejected");

        var category = await Request("api/category", HttpMethod.Post, new { name = "XYZ category", description = "Keep assignment" }, 201);
        await Request("api/product/" + hotelId, HttpMethod.Put, new { categoryId = (int)category["id"]!, isActive = false }, 204);
        var repeated = await Request("api/integration/import/xyz", HttpMethod.Post);
        var after = await Request("api/product/" + hotelId);
        Check((int)repeated["updated"]! == 2 && (int)repeated["created"]! == 0 &&
            (int)after["supplierId"]! == xyzId && !(bool)after["isActive"]! && (int)after["categoryId"]! == (int)category["id"]!,
            "repeat sync preserves supplier/product IDs, category and inactive product state");
        await Request("api/product/" + hotelId, HttpMethod.Put, new { categoryId = (int)category["id"]!, isActive = true }, 204);

        using (var scope = services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ProductDbContext>();
            var supplier = await db.Suppliers.FindAsync(xyzId);
            supplier!.Name = "Locally named XYZ";
            supplier.IsActive = false;
            await db.SaveChangesAsync();
        }
        await Request("api/integration/import/XYZ", HttpMethod.Post, status: 409);
        var inactive = await Request("api/supplier/code/XYZ");
        Check(!(bool)inactive["isActive"]! && (string?)inactive["name"] == "Locally named XYZ", "sync never reactivates or renames an existing supplier");
        using (var scope = services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ProductDbContext>();
            (await db.Suppliers.FindAsync(xyzId))!.IsActive = true;
            await db.SaveChangesAsync();
        }
        var registrations = await Task.WhenAll(Enumerable.Range(0, 6).Select(_ => Request("api/internal/suppliers/register", HttpMethod.Post, new { code = " concurrent ", name = "Concurrent supplier" })));
        Check(registrations.Select(s => (int)s["id"]!).Distinct().Count() == 1, "concurrent supplier registration is idempotent");

        var day = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(30);
        object Hotel(int productId) => new { productId, supplierOptionId = "STD", checkIn = day.ToString("yyyy-MM-dd"), checkOut = day.AddDays(2).ToString("yyyy-MM-dd"), adults = 2, children = 0, rooms = 1 };
        object Activity() => new { productId = (int)cdeActivity["id"]!, supplierOptionId = "MORNING", activityDate = day.ToString("yyyy-MM-dd"), timeSlot = "09:00", adults = 2, children = 0, rooms = 1 };
        async Task<string> Cart(params object[] selections)
        {
            var created = await Request("api/carts", HttpMethod.Post, status: 201);
            var id = (string)created["id"]!;
            foreach (var selection in selections) await Request($"api/carts/{id}/items", HttpMethod.Post, selection);
            return id;
        }
        var cart = await Cart(Hotel(hotelId), Activity(), Hotel((int)abcHotel["id"]!));
        var quote = await Request($"api/carts/{cart}/quote");
        Check((bool)quote["allAvailable"]! && quote["items"]!.AsArray().Count == 3, "XYZ hotel quotes alongside ABC and CDE selections");
        var items = (await Request("api/carts/" + cart))["items"]!.AsArray();
        var editId = (int)items.First()!["id"]!;
        await Request($"api/carts/{cart}/items/{editId}", HttpMethod.Put, Hotel(hotelId));
        Check((await Request("api/carts/" + cart))["items"]!.AsArray().Count == 3, "third-supplier cart editing preserves item identity");
        var order = await Request($"api/carts/{cart}/checkout", HttpMethod.Post, new { customerName = "Extensibility check" });
        Check((string?)order["status"] == "Confirmed" && order["bookings"]!.AsArray().All(b => (string?)b!["status"] == "Confirmed"), "mixed checkout confirms all three suppliers");
        var xyz = services.GetRequiredService<XyzSupplierAdapter>();
        var bookingCalls = xyz.BookingCalls;
        var retry = await Request($"api/carts/{cart}/checkout", HttpMethod.Post, new { customerName = "Extensibility check" });
        Check((string?)retry["id"] == (string?)order["id"] && xyz.BookingCalls == bookingCalls, "checkout retry does not create duplicate supplier bookings");
        xyz.FailNextCancellation = true;
        var cancellation = await Request($"api/orders/{order["id"]}/cancel", HttpMethod.Post, status: 202);
        Check((string?)cancellation["status"] == "CancellationFailed", "failed cancellation retains retryable state");
        cancellation = await Request($"api/orders/{order["id"]}/cancel", HttpMethod.Post);
        Check((string?)cancellation["status"] == "Cancelled" && cancellation["bookings"]!.AsArray().All(b => (string?)b!["status"] == "Cancelled"), "cancellation retry cancels all suppliers");

        var failedCart = await Cart(Hotel((int)abcHotel["id"]!), Hotel(hotelId));
        xyz.RejectNextBooking = true;
        var failed = await Request($"api/carts/{failedCart}/checkout", HttpMethod.Post, new { customerName = "Compensation check" }, 409);
        Check((string?)failed["status"] == "Failed" && (string?)failed["bookings"]![0]!["status"] == "Cancelled", "definitive third-supplier failure compensates the earlier booking");
        var uncertainCart = await Cart(Hotel((int)abcHotel["id"]!), Hotel(hotelId));
        xyz.LoseNextBookingResponse = true;
        var uncertain = await Request($"api/carts/{uncertainCart}/checkout", HttpMethod.Post, new { customerName = "Uncertain check" }, 202);
        Check((string?)uncertain["status"] == "RequiresAttention" && (string?)uncertain["bookings"]![1]!["status"] == "Unknown", "uncertain outcomes preserve RequiresAttention and Unknown statuses");

        var environment = services.GetRequiredService<IWebHostEnvironment>();
        environment.EnvironmentName = "Production";
        try
        {
            await Request("api/integration/import/XYZ", HttpMethod.Post, status: 404);
            await Request("api/internal/suppliers/register", HttpMethod.Post, new { code = "BLOCKED", name = "Blocked" }, 404);
            await Request("api/internal/products/import", HttpMethod.Post, new { }, 404);
            Check(true, "import and registration endpoints remain unavailable outside Development");
        }
        finally { environment.EnvironmentName = "Development"; }
        Console.WriteLine($"{checks} supplier onboarding checks passed using disposable SQL databases and isolated mock APIs.");
    }
}
