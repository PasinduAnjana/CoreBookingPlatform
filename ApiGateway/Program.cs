using Ocelot.DependencyInjection;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Load gateway routes
builder.Configuration.AddJsonFile(
    "ocelot.json",
    optional: false,
    reloadOnChange: true);

// Register Ocelot services
builder.Services.AddOcelot(builder.Configuration);

var app = builder.Build();

// Ocelot handles the incoming requests
await app.UseOcelot();

await app.RunAsync();