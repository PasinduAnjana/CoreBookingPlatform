using AbcMockApi.Services;
using CdeMockApi.Services;
using IntegrationService.Adapters;
using IntegrationService.Adapters.Interfaces;
using IntegrationService.Services;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using OrderService.Repositories;
using OrderService.Repositories.Interfaces;
using OrderService.Services;
using OrderService.Services.Interfaces;
using ProductService.Data;
using ProductService.Repositories;
using ProductService.Repositories.Interfaces;
using ProductService.Services;
using ProductService.Services.Interfaces;

var connection = Environment.GetEnvironmentVariable("SUPPLIER_CHECK_SQL_CONNECTION");
if (string.IsNullOrWhiteSpace(connection))
    throw new InvalidOperationException("Set SUPPLIER_CHECK_SQL_CONNECTION to a SQL Server connection with permission to create disposable test databases.");

// Names are generated here, never accepted from the environment, so cleanup cannot
// target the application's ProductDB or OrderDB.
var runId = Guid.NewGuid().ToString("N");
var productConnection = new SqlConnectionStringBuilder(connection) { InitialCatalog = "SupplierProductChecks_" + runId };
var orderConnection = new SqlConnectionStringBuilder(connection) { InitialCatalog = "SupplierOrderChecks_" + runId };
const string url = "http://localhost:15173";
var builder = WebApplication.CreateBuilder(new WebApplicationOptions { Args = args, ContentRootPath = AppContext.BaseDirectory, EnvironmentName = "Development" });
builder.WebHost.UseUrls(url);
builder.Logging.SetMinimumLevel(LogLevel.Warning);
builder.Logging.AddFilter("Microsoft", LogLevel.Critical);
builder.Logging.AddFilter("System.Net.Http", LogLevel.Warning);
builder.Services.AddControllers()
    .AddApplicationPart(typeof(ProductService.Controllers.ProductController).Assembly)
    .AddApplicationPart(typeof(OrderService.Controllers.CartController).Assembly)
    .AddApplicationPart(typeof(IntegrationService.Controllers.IntegrationController).Assembly)
    .AddApplicationPart(typeof(AbcMockApi.Controllers.HotelsController).Assembly)
    .AddApplicationPart(typeof(CdeMockApi.Controllers.ActivitiesController).Assembly);
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.WithOrigins("http://localhost:5175").AllowAnyHeader().AllowAnyMethod()));
builder.Services.AddDbContext<ProductDbContext>(o => o.UseSqlServer(productConnection.ConnectionString));
builder.Services.AddDbContext<OrderDbContext>(o => o.UseSqlServer(orderConnection.ConnectionString));
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<ISupplierRepository, SupplierRepository>();
builder.Services.AddScoped<ISupplierService, SupplierService>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IProductService, ProductService.Services.ProductService>();
builder.Services.AddScoped<IProductContentRepository, ProductContentRepository>();
builder.Services.AddScoped<IProductContentService, ProductContentService>();
builder.Services.AddScoped<IProductImportRepository, ProductImportRepository>();
builder.Services.AddScoped<IProductImportService, ProductImportService>();
builder.Services.AddScoped<ICartRepository, CartRepository>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<ICheckoutRepository, CheckoutRepository>();
builder.Services.AddScoped<ICheckoutService, CheckoutService>();
builder.Services.AddScoped<IOrderCancellationService, OrderCancellationService>();
builder.Services.AddHttpClient<OrderService.Clients.ProductServiceClient>("OrderProducts", c => c.BaseAddress = new(url));
builder.Services.AddHttpClient<OrderService.Clients.IntegrationServiceClient>(c => c.BaseAddress = new(url));
builder.Services.AddHttpClient<IntegrationService.Services.ProductServiceClient>("ImportProducts", c => c.BaseAddress = new(url));
builder.Services.AddSingleton<HotelMockData>();
builder.Services.AddSingleton<ActivityMockData>();
builder.Services.AddHttpClient<AbcSupplierAdapter>(c => c.BaseAddress = new(url));
builder.Services.AddHttpClient<CdeSupplierAdapter>(c => c.BaseAddress = new(url));
builder.Services.AddTransient<ISupplierAdapter>(s => s.GetRequiredService<AbcSupplierAdapter>());
builder.Services.AddTransient<ISupplierAdapter>(s => s.GetRequiredService<CdeSupplierAdapter>());
builder.Services.AddSingleton<XyzSupplierAdapter>();
builder.Services.AddSingleton<ISupplierAdapter>(s => s.GetRequiredService<XyzSupplierAdapter>());
builder.Services.AddScoped<SupplierAdapterRegistry>();
builder.Services.AddScoped<SupplierIntegrationService>();
builder.Services.AddScoped<ProductImportOrchestrator>();
await using var app = builder.Build();
app.UseExceptionHandler();
app.UseCors();
app.MapControllers();
app.MapOpenApi();

try
{
    await MigrationChecks.Run(app.Services);
    await app.StartAsync();
    await SupplierChecks.Run(app.Services, url);
    await OrderManagementChecks.Run(app.Services, url);
    await ErrorHandlingChecks.Run(app.Services);
    if (args.Contains("--serve"))
    {
        Console.WriteLine("Disposable supplier fixture ready at " + url + ". Stop it to delete its test databases.");
        await app.WaitForShutdownAsync();
    }
}
finally
{
    await app.StopAsync();
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<OrderDbContext>().Database.EnsureDeletedAsync();
    await scope.ServiceProvider.GetRequiredService<ProductDbContext>().Database.EnsureDeletedAsync();
    Console.WriteLine("Disposable test databases removed. Application databases were not modified.");
}
