using Ocelot.DependencyInjection;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Load gateway routes
var configFile = builder.Configuration["OcelotConfigFile"]
                 ?? "ocelot.json";

builder.Configuration.AddJsonFile(
    configFile,
    optional: false,
    reloadOnChange: true);


// Register Ocelot services
builder.Services.AddOcelot(builder.Configuration);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerForOcelot(builder.Configuration);
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    policy.WithOrigins("http://localhost:5173", "http://127.0.0.1:5173")
        .AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();
app.UseCors();

if (app.Environment.IsDevelopment())
{
    // Register documentation before Ocelot's terminal proxy middleware.
    app.UseSwaggerForOcelotUI(options =>
    {
        options.PathToSwaggerGenerator = "/swagger/docs";
    }, ui =>
    {
        ui.DocumentTitle = "Core Booking API Gateway";
    });
}

// Ocelot handles the incoming requests
await app.UseOcelot();

await app.RunAsync();
