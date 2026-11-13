using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using OrderService.Clients;
using OrderService.Controllers;
using OrderService.DTOs;
using OrderService.Models;
using OrderService.Repositories.Interfaces;
using OrderService.Services;
using OrderService.Services.Interfaces;
using static OrderService.Dtos.ExternalServiceDtos;

if (args.Contains("--serve"))
{
    // Isolated browser fixture: real cart controller/service, in-memory persistence,
    // deterministic product and supplier HTTP responses. No SQL or live bookings.
    var builder = WebApplication.CreateBuilder(args);
    builder.WebHost.UseUrls("http://localhost:15173");
    builder.Services.AddControllers().AddApplicationPart(typeof(CartController).Assembly);
    builder.Services.AddSingleton<ICartRepository, MemoryCarts>();
    builder.Services.AddScoped<ICartService, CartService>();
    builder.Services.AddTransient(_ => Fixture.ProductClient());
    builder.Services.AddTransient(_ => Fixture.IntegrationClient());
    builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.WithOrigins("http://localhost:5175").AllowAnyHeader().AllowAnyMethod()));
    var app = builder.Build();
    app.UseCors();
    app.MapControllers();
    app.MapGet("/api/product", () => new[] { Fixture.Product(1), Fixture.Product(2) });
    app.MapGet("/api/product/{id:int}", (int id) => Results.Ok(Fixture.Product(id)));
    app.MapGet("/api/products/{id:int}/content", () => Results.NotFound());
    await app.RunAsync();
    return;
}

var checks = 0;
async Task Check(string name, Func<Cart, AddCartItemDto, MemoryCarts, Task> run)
{
    var cart = Fixture.Cart();
    var edit = Fixture.Selection();
    var repository = new MemoryCarts();
    await repository.AddAsync(cart);
    await run(cart, edit, repository);
    Console.WriteLine($"PASS {name}");
    checks++;
}

void Assert(bool condition, string message)
{
    if (!condition) throw new Exception(message);
}

async Task Reject(Cart cart, AddCartItemDto edit, MemoryCarts repository, int status = 400, ProductLookupDto? product = null)
{
    var before = JsonSerializer.Serialize(cart.Items.Select(i => new { i.Id, i.ProductId, i.SupplierOptionId, i.CheckIn, i.CheckOut, i.ActivityDate, i.TimeSlot, i.Adults, i.Children, i.Rooms }));
    var response = await Fixture.Controller(repository, product).UpdateItem(cart.Id, 7, edit, default);
    Assert(response is ObjectResult result && result.StatusCode == status, $"Expected HTTP {status}");
    var after = JsonSerializer.Serialize(cart.Items.Select(i => new { i.Id, i.ProductId, i.SupplierOptionId, i.CheckIn, i.CheckOut, i.ActivityDate, i.TimeSlot, i.Adults, i.Children, i.Rooms }));
    Assert(before == after && repository.Saves == 0, "Rejected edit changed persisted selection");
}

await Check("hotel update keeps item ID and cart size", async (cart, edit, repo) =>
{
    edit.SupplierOptionId = "DLX";
    edit.Adults = 3;
    edit.Children = 2;
    edit.Rooms = 2;
    edit.CheckOut = edit.CheckOut!.Value.AddDays(1);
    var response = await Fixture.Controller(repo).UpdateItem(cart.Id, 7, edit, default);
    Assert(response is OkObjectResult { Value: CartDto dto } && dto.Items.Single().Id == 7, "Expected updated cart with original item ID");
    Assert(cart.Items.Single().Rooms == 2 && cart.Items.Single().SupplierOptionId == "DLX" && repo.Saves == 1, "Hotel change was not saved");
});

await Check("activity option, date, slot and guests update", async (cart, edit, repo) =>
{
    cart.Items.Single().ProductId = 2;
    edit.ProductId = 2;
    edit.CheckIn = edit.CheckOut = null;
    edit.ActivityDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(40);
    edit.TimeSlot = "14:00";
    edit.SupplierOptionId = "MORNING";
    var response = await Fixture.Controller(repo).UpdateItem(cart.Id, 7, edit, default);
    Assert(response is OkObjectResult && cart.Items.Single().TimeSlot == "14:00", "Activity change failed");
    Assert(cart.Items.Single().CheckIn == null && cart.Items.Single().ActivityDate == edit.ActivityDate, "Dates were not replaced");
});

await Check("missing cart", async (cart, edit, repo) =>
{
    var response = await Fixture.Controller(repo).UpdateItem(Guid.NewGuid(), 7, edit, default);
    Assert(response is NotFoundObjectResult && repo.Saves == 0, "Missing cart must return 404");
});
await Check("item must belong to requested cart", async (cart, edit, repo) =>
{
    var response = await Fixture.Controller(repo).UpdateItem(cart.Id, 99, edit, default);
    Assert(response is NotFoundObjectResult && repo.Saves == 0, "Unknown item must return 404");
});
await Check("checked-out carts cannot be edited", async (cart, edit, repo) => { repo.OrderExists = true; await Reject(cart, edit, repo, 409); });
await Check("product identity cannot change", async (cart, edit, repo) => { edit.ProductId = 2; await Reject(cart, edit, repo); });
await Check("past hotel dates", async (cart, edit, repo) => { edit.CheckIn = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1); await Reject(cart, edit, repo); });
await Check("checkout must follow check-in", async (cart, edit, repo) => { edit.CheckOut = edit.CheckIn; await Reject(cart, edit, repo); });
await Check("hotel and activity fields cannot mix", async (cart, edit, repo) => { edit.ActivityDate = edit.CheckIn; await Reject(cart, edit, repo); });
await Check("one adult per room", async (cart, edit, repo) => { edit.Rooms = 3; await Reject(cart, edit, repo); });
await Check("hotel adult occupancy", async (cart, edit, repo) => { edit.Adults = 3; await Reject(cart, edit, repo); });
await Check("hotel child occupancy", async (cart, edit, repo) => { edit.Children = 2; await Reject(cart, edit, repo); });
await Check("unknown supplier option", async (cart, edit, repo) => { edit.SupplierOptionId = "missing"; await Reject(cart, edit, repo); });
await Check("inactive product", async (cart, edit, repo) => { var p = Fixture.Product(1); p.IsActive = false; await Reject(cart, edit, repo, product: p); });
await Check("invalid product metadata", async (cart, edit, repo) => { var p = Fixture.Product(1); p.AttributesJson = "{"; await Reject(cart, edit, repo, product: p); });
await Check("unsupported activity slot", async (cart, edit, repo) => { Fixture.Activity(cart, edit); edit.TimeSlot = "23:00"; await Reject(cart, edit, repo); });
await Check("past activity date", async (cart, edit, repo) => { Fixture.Activity(cart, edit); edit.ActivityDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1); await Reject(cart, edit, repo); });
await Check("activity capacity", async (cart, edit, repo) => { Fixture.Activity(cart, edit); edit.Adults = 16; await Reject(cart, edit, repo); });
await Check("unsupported passenger type", async (cart, edit, repo) =>
{
    Fixture.Activity(cart, edit);
    edit.Children = 1;
    var p = Fixture.Product(2);
    p.AttributesJson = p.AttributesJson!.Replace("[\"Adult\",\"Child\"]", "[\"Adult\"]");
    await Reject(cart, edit, repo, product: p);
});
await Check("large participant counts cannot overflow", async (cart, edit, repo) => { Fixture.Activity(cart, edit); edit.Adults = edit.Children = int.MaxValue; await Reject(cart, edit, repo); });
await Check("add uses the same capacity validation", async (cart, edit, repo) =>
{
    edit.Adults = 3;
    try { await Fixture.Service(repo).AddItemAsync(cart.Id, edit); throw new Exception("Expected rejection"); }
    catch (ArgumentException) { Assert(repo.Saves == 0 && cart.Items.Count == 1, "Invalid add changed cart"); }
});
Console.WriteLine($"{checks} cart item checks passed. No databases or live supplier APIs were used.");

static class Fixture
{
    public static ProductLookupDto Product(int id) => new()
    {
        Id = id, Name = id == 1 ? "Test hotel" : "Test activity", IsActive = true,
        SupplierCode = id == 1 ? "ABC" : "CDE", SupplierProductId = id == 1 ? "ABC-H001" : "CDE-A101",
        AttributesJson = id == 1
            ? """{"options":[{"supplierOptionId":"STD","name":"Standard Room","maxAdults":2,"maxChildren":1},{"supplierOptionId":"DLX","name":"Deluxe Room","maxAdults":2,"maxChildren":2}]}"""
            : """{"timeSlots":["09:00","14:00"],"options":[{"supplierOptionId":"MORNING","label":"Morning Group Tour","maxParticipants":15,"passengerTypes":["Adult","Child"]}]}"""
    };
    public static AddCartItemDto Selection() => new()
    {
        ProductId = 1, SupplierOptionId = "STD", Adults = 2, Children = 0, Rooms = 1,
        CheckIn = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(30), CheckOut = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(32)
    };
    public static Cart Cart()
    {
        var selection = Selection();
        var cart = new Cart();
        cart.Items.Add(new CartItem { Id = 7, ProductId = 1, SupplierOptionId = "STD", Adults = 2, Rooms = 1, CheckIn = selection.CheckIn, CheckOut = selection.CheckOut });
        return cart;
    }
    public static void Activity(Cart cart, AddCartItemDto edit)
    {
        cart.Items.Single().ProductId = edit.ProductId = 2;
        edit.SupplierOptionId = "MORNING";
        edit.ActivityDate = edit.CheckIn;
        edit.CheckIn = edit.CheckOut = null;
        edit.TimeSlot = "09:00";
    }
    public static ProductServiceClient ProductClient(ProductLookupDto? product = null) => new(new HttpClient(new ProductResponses(product)) { BaseAddress = new Uri("http://fixture/") });
    public static IntegrationServiceClient IntegrationClient() => new(new HttpClient(new QuoteResponses()) { BaseAddress = new Uri("http://fixture/") });
    public static CartService Service(MemoryCarts repo, ProductLookupDto? product = null) => new(repo, ProductClient(product), IntegrationClient());
    public static CartController Controller(MemoryCarts repo, ProductLookupDto? product = null) => new(Service(repo, product));
}

sealed class MemoryCarts : ICartRepository
{
    private readonly Dictionary<Guid, Cart> carts = [];
    public int Saves { get; private set; }
    public bool OrderExists { get; set; }
    public Task<Cart?> GetByIdAsync(Guid id) => Task.FromResult(carts.GetValueOrDefault(id));
    public Task<bool> HasOrderAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(OrderExists);
    public Task AddAsync(Cart cart) { carts[cart.Id] = cart; return Task.CompletedTask; }
    public void RemoveItem(CartItem item) { foreach (var cart in carts.Values) cart.Items.Remove(item); }
    public Task<int> SaveChangesAsync()
    {
        foreach (var cart in carts.Values)
        {
            var next = cart.Items.Select(i => i.Id).DefaultIfEmpty().Max() + 1;
            foreach (var item in cart.Items.Where(i => i.Id == 0)) item.Id = next++;
        }
        Saves++;
        return Task.FromResult(1);
    }
}

sealed class ProductResponses(ProductLookupDto? product) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
    {
        var id = int.Parse(request.RequestUri!.Segments.Last());
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(product ?? Fixture.Product(id)) });
    }
}

sealed class QuoteResponses : HttpMessageHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
    {
        var input = (await request.Content!.ReadFromJsonAsync<AvailabilityRequestDto>(token))!;
        var price = input.SupplierCode == "ABC"
            ? (input.SupplierOptionId == "STD" ? 12000 : 18000) * input.Rooms * (input.CheckOut!.Value.DayNumber - input.CheckIn!.Value.DayNumber)
            : input.Adults * 5000 + input.Children * 2500;
        return new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new AvailabilityResponseDto { Available = true, AvailableQuantity = 20, Currency = "LKR", TotalPrice = price }) };
    }
}
