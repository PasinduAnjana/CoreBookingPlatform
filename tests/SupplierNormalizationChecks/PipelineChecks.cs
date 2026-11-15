using System.Net;
using System.Net.Http.Json;
using IntegrationService.Adapters.Interfaces;
using IntegrationService.Controllers;
using IntegrationService.Services;
using Microsoft.AspNetCore.Mvc;
using SupplierContracts;
using IntegrationClient = OrderService.Clients.IntegrationServiceClient;
using ClientException = OrderService.Clients.SupplierOperationException;

static class PipelineChecks
{
    public static async Task Run()
    {
        Func<HttpResponseMessage> supplierReply = () => Responses.Json(AdapterChecks.Booking("Confirmed"));
        using var supplierClient = Responses.Client(_ => supplierReply());
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Development" });
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Logging.ClearProviders();
        builder.Services.AddProblemDetails();
        builder.Services.AddControllers().AddApplicationPart(typeof(BookingController).Assembly)
            .ConfigureApiBehaviorOptions(options => options.InvalidModelStateResponseFactory = _ =>
                new BadRequestObjectResult(SupplierError.Create(SupplierErrorCode.InvalidRequest, SupplierErrorOutcome.Rejected)));
        builder.Services.AddSingleton<ISupplierAdapter>(AdapterChecks.Adapter("ABC", supplierClient));
        builder.Services.AddScoped<SupplierAdapterRegistry>();
        builder.Services.AddScoped<SupplierIntegrationService>();
        await using var app = builder.Build();
        app.UseExceptionHandler();
        app.MapControllers();
        app.MapGet("/unexpected-error", (Func<IResult>)(() =>
            throw new Microsoft.EntityFrameworkCore.DbUpdateException("SECRET database details")));
        await app.StartAsync();
        try
        {
            using var http = new HttpClient { BaseAddress = new Uri(app.Urls.Single()) };
            var client = new IntegrationClient(http);
            var booking = await client.CreateBookingAsync(Request());
            Check.That(booking.Status == SupplierBookingStatus.Confirmed, "adapter status survives real HTTP serialization to OrderService");

            using (var response = await http.GetAsync("unexpected-error"))
            {
                var body = await response.Content.ReadAsStringAsync();
                var problem = System.Text.Json.JsonDocument.Parse(body);
                using (problem)
                {
                    Check.That(response.StatusCode == HttpStatusCode.InternalServerError &&
                        response.Content.Headers.ContentType?.MediaType == "application/problem+json" &&
                        problem.RootElement.GetProperty("status").GetInt32() == 500 && !body.Contains("SECRET"),
                        "unexpected database errors return safe HTTP 500 Problem Details even in Development");
                }
            }

            foreach (var (supplierStatus, apiStatus, expected) in new[]
            {
                (HttpStatusCode.BadRequest, HttpStatusCode.BadRequest, SupplierErrorCode.InvalidRequest),
                (HttpStatusCode.NotFound, HttpStatusCode.NotFound, SupplierErrorCode.NotFound),
                (HttpStatusCode.Conflict, HttpStatusCode.Conflict, SupplierErrorCode.Conflict),
                (HttpStatusCode.Unauthorized, HttpStatusCode.BadGateway, SupplierErrorCode.AuthenticationFailed),
                (HttpStatusCode.TooManyRequests, HttpStatusCode.ServiceUnavailable, SupplierErrorCode.RateLimited),
                (HttpStatusCode.GatewayTimeout, HttpStatusCode.GatewayTimeout, SupplierErrorCode.Timeout),
                (HttpStatusCode.InternalServerError, HttpStatusCode.BadGateway, SupplierErrorCode.SupplierFailure)
            })
            {
                supplierReply = () => new(supplierStatus) { Content = new StringContent("SECRET upstream response") };
                foreach (var operation in new[] { "bookings", "bookings/cancel", "availability", "products/ABC" })
                {
                    using var response = operation.StartsWith("products")
                        ? await http.GetAsync("api/integration/" + operation)
                        : await http.PostAsJsonAsync("api/integration/" + operation, operation.EndsWith("cancel")
                            ? (object)new { supplierCode = "ABC", supplierBookingId = "reference" } : AdapterChecks.Request("ABC"));
                    var error = await response.Content.ReadFromJsonAsync<SupplierError>();
                    Check.That(response.StatusCode == apiStatus && error?.Code == expected && !error.Message.Contains("SECRET"),
                        $"{operation} returns the normalized {expected} error envelope");
                }
                var failure = await Check.Throws<ClientException>(() => client.CreateBookingAsync(Request()));
                Check.That(failure.Error.Code == expected, $"OrderService reads normalized {expected}");
            }

            using (var response = await http.PostAsJsonAsync("api/integration/bookings", new { supplierCode = "ABC" }))
            {
                var error = await response.Content.ReadFromJsonAsync<SupplierError>();
                Check.That(error?.Code == SupplierErrorCode.InvalidRequest && error.Outcome == SupplierErrorOutcome.Rejected,
                    "adapter validation returns a normalized rejection");
            }
            using (var body = new StringContent("{", System.Text.Encoding.UTF8, "application/json"))
            using (var response = await http.PostAsync("api/integration/bookings", body))
            {
                var error = await response.Content.ReadFromJsonAsync<SupplierError>();
                Check.That(error?.Code == SupplierErrorCode.InvalidRequest, "MVC request validation uses the normalized error envelope");
            }
        }
        finally { await app.StopAsync(); }

        foreach (var body in new object[]
        {
            "SECRET legacy conflict", new { code = "Conflict" }, new { code = "Conflict", outcome = "Unknown" },
            new { code = "Timeout", outcome = "Rejected" }, new { code = 999, outcome = "Rejected" }
        })
        {
            using var http = Responses.Client(_ => Responses.Json(body, HttpStatusCode.Conflict));
            var ex = await Check.Throws<ClientException>(() => new IntegrationClient(http).CreateBookingAsync(Request()));
            Check.That(ex.Error.Outcome == SupplierErrorOutcome.Unknown && !ex.Message.Contains("SECRET"),
                "HTTP conflict without a valid explicit rejection remains uncertain");
        }
    }

    private static OrderService.DTOs.SupplierBookingRequestDto Request() => new()
    {
        SupplierCode = "ABC", SupplierProductId = "product", SupplierOptionId = "option",
        CustomerName = "Test", IdempotencyKey = "key", Adults = 1, Rooms = 1,
        CheckIn = new DateOnly(2030, 1, 1), CheckOut = new DateOnly(2030, 1, 2)
    };
}
