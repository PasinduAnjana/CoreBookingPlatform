using IntegrationService.Adapters;
using IntegrationService.Adapters.Interfaces;
using IntegrationService.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddHttpClient<AbcSupplierAdapter>(client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["SupplierApis:ABC"]
        ?? throw new InvalidOperationException(
            "ABC API URL is missing."));
});
builder.Services.AddHttpClient<CdeSupplierAdapter>(client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["SupplierApis:CDE"]
        ?? throw new InvalidOperationException(
            "CDE API URL is missing."));
});

builder.Services.AddHttpClient<ProductServiceClient>(client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["ProductService:BaseUrl"]
        ?? throw new InvalidOperationException(
            "ProductService URL is missing."));
});


builder.Services.AddTransient<ISupplierAdapter>(sp=>
    sp.GetRequiredService<AbcSupplierAdapter>());

builder.Services.AddTransient<ISupplierAdapter>(sp =>
    sp.GetRequiredService<CdeSupplierAdapter>());

builder.Services.AddScoped<SupplierIntegrationService>();
builder.Services.AddScoped<ProductImportOrchestrator>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint(
            "/openapi/v1.json",
            "Integration Service API");
    });
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
