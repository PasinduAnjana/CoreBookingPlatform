using Microsoft.EntityFrameworkCore;
using OrderService.Clients;
using OrderService.Data;
using OrderService.Repositories;
using OrderService.Repositories.Interfaces;
using OrderService.Services;
using OrderService.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();



builder.Services.AddDbContext<OrderDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));


builder.Services.AddScoped<ICartRepository, CartRepository>();

builder.Services.AddScoped<ICartService, CartService>();

builder.Services.AddHttpClient<ProductServiceClient>(client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["Services:ProductService"]
        ?? throw new InvalidOperationException("ProductService URL is missing."));

    client.Timeout = TimeSpan.FromSeconds(10);
});

builder.Services.AddHttpClient<IntegrationServiceClient>(client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["Services:IntegrationService"]
        ?? throw new InvalidOperationException("IntegrationService URL is missing."));

    client.Timeout = TimeSpan.FromSeconds(15);
});


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint(
        "/openapi/v1.json",
        "Order Service API");
});

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
