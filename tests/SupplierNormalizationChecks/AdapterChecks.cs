using System.Net;
using System.Text;
using System.Text.Json;
using IntegrationService.Adapters;
using IntegrationService.Adapters.Interfaces;
using IntegrationService.Models.Common;
using SupplierContracts;

static class AdapterChecks
{
    public static ISupplierAdapter Adapter(string code, HttpClient client)
        => code == "ABC" ? new AbcSupplierAdapter(client) : new CdeSupplierAdapter(client);

    public static NormalizedBookingRequest Request(string code) => new()
    {
        SupplierCode = code, SupplierProductId = "product", SupplierOptionId = "option",
        CustomerName = "Test", IdempotencyKey = "test-key", Adults = 1, Rooms = 1,
        CheckIn = new DateOnly(2030, 1, 1), CheckOut = new DateOnly(2030, 1, 2),
        Date = new DateOnly(2030, 1, 1), TimeSlot = "09:00"
    };

    public static object Booking(string? status, string reference = "reference") => new
    {
        bookingId = reference, hotelCode = "product", roomCode = "option",
        activityId = "product", packageId = "option", status,
        currency = "LKR", currencyCode = "LKR", totalAmount = 100m, payableAmount = 100m
    };

    public static async Task Run()
    {
        foreach (var code in new[] { "ABC", "CDE" })
        {
            foreach (var (native, expected) in new (string?, SupplierBookingStatus)[]
            {
                ("Confirmed", SupplierBookingStatus.Confirmed), (" confirmed ", SupplierBookingStatus.Confirmed),
                ("CANCELLED", SupplierBookingStatus.Cancelled), ("BOOKED", SupplierBookingStatus.Unknown),
                ("unexpected", SupplierBookingStatus.Unknown), (null, SupplierBookingStatus.Unknown),
                ("", SupplierBookingStatus.Unknown)
            })
            {
                using var client = Responses.Client(_ => Responses.Json(Booking(native)));
                var adapter = Adapter(code, client);
                var booking = await adapter.CreateBookingAsync(Request(code));
                var cancellation = await adapter.CancelBookingAsync(new() { SupplierBookingId = "reference" });
                Check.That(booking.Status == expected && cancellation.Status == expected,
                    $"{code} maps '{native ?? "null"}' explicitly for booking and cancellation");
                Check.That(booking.SupplierBookingId == "reference", $"{code} preserves references for '{native ?? "null"}'");
            }

            foreach (var (http, expected, outcome) in new[]
            {
                (HttpStatusCode.BadRequest, SupplierErrorCode.InvalidRequest, SupplierErrorOutcome.Rejected),
                (HttpStatusCode.NotFound, SupplierErrorCode.NotFound, SupplierErrorOutcome.Rejected),
                (HttpStatusCode.Conflict, SupplierErrorCode.Conflict, SupplierErrorOutcome.Rejected),
                (HttpStatusCode.Unauthorized, SupplierErrorCode.AuthenticationFailed, SupplierErrorOutcome.Unknown),
                (HttpStatusCode.Forbidden, SupplierErrorCode.AuthenticationFailed, SupplierErrorOutcome.Unknown),
                (HttpStatusCode.TooManyRequests, SupplierErrorCode.RateLimited, SupplierErrorOutcome.Unknown),
                (HttpStatusCode.GatewayTimeout, SupplierErrorCode.Timeout, SupplierErrorOutcome.Unknown),
                (HttpStatusCode.InternalServerError, SupplierErrorCode.SupplierFailure, SupplierErrorOutcome.Unknown)
            })
            {
                using var client = Responses.Client(_ => new(http) { Content = new StringContent("SECRET supplier body") });
                var adapter = Adapter(code, client);
                foreach (var operation in new[] { "catalog", "availability", "booking", "cancellation" })
                {
                    var ex = await Check.Throws<SupplierOperationException>(() => Call(adapter, operation));
                    Check.That(ex.Error.Code == expected && ex.Error.Outcome == outcome && !ex.Message.Contains("SECRET"),
                        $"{code} {operation} normalizes {(int)http} without leaking supplier bodies");
                }
            }

            using (var client = Responses.Client(_ => Responses.Json(Booking("Cancelled", ""))))
            {
                var response = await Adapter(code, client).CancelBookingAsync(new() { SupplierBookingId = "reference" });
                Check.That(response.Status == SupplierBookingStatus.Unknown,
                    $"{code} cancellation without a supplier reference is not confirmed");
            }

            foreach (var body in new[] { "null", "broken JSON", "{\"status\":" })
            {
                using var client = Responses.Client(_ => new(HttpStatusCode.OK)
                { Content = new StringContent(body, Encoding.UTF8, "application/json") });
                var ex = await Check.Throws<SupplierOperationException>(() => Adapter(code, client).CreateBookingAsync(Request(code)));
                Check.That(ex.Error.Code == SupplierErrorCode.InvalidResponse && ex.Error.Outcome == SupplierErrorOutcome.Unknown,
                    $"{code} invalid/null response remains uncertain");
            }

            using (var client = Responses.Client(_ => throw new HttpRequestException("SECRET transport details")))
            {
                var ex = await Check.Throws<SupplierOperationException>(() => Adapter(code, client).CreateBookingAsync(Request(code)));
                Check.That(ex.Error.Code == SupplierErrorCode.Unreachable && ex.Error.Outcome == SupplierErrorOutcome.Unknown,
                    $"{code} transport failure remains uncertain");
            }
            using (var client = Responses.Client(_ => throw new TaskCanceledException("timeout")))
            {
                var ex = await Check.Throws<SupplierOperationException>(() => Adapter(code, client).CreateBookingAsync(Request(code)));
                Check.That(ex.Error.Code == SupplierErrorCode.Timeout && ex.Error.Outcome == SupplierErrorOutcome.Unknown,
                    $"{code} timeout remains uncertain");
            }
            using (var client = Responses.Client(_ => throw new TaskCanceledException()))
            using (var cancellation = new CancellationTokenSource())
            {
                cancellation.Cancel();
                await Check.Throws<OperationCanceledException>(() => Adapter(code, client).CreateBookingAsync(Request(code), cancellation.Token));
                Check.That(true, $"{code} propagates caller cancellation");
            }
        }

        Check.That(JsonSerializer.Serialize(SupplierBookingStatus.Confirmed) == "\"Confirmed\"",
            "normalized status keeps the existing JSON spelling");
    }

    private static async Task Call(ISupplierAdapter adapter, string operation)
    {
        var request = Request(adapter.SupplierCode);
        switch (operation)
        {
            case "catalog": await adapter.GetProductsAsync(); break;
            case "availability":
                await adapter.CheckAvailabilityAsync(new()
                {
                    SupplierProductId = request.SupplierProductId, SupplierOptionId = request.SupplierOptionId,
                    CheckIn = request.CheckIn, CheckOut = request.CheckOut, Date = request.Date,
                    TimeSlot = request.TimeSlot, Adults = 1, Rooms = 1
                });
                break;
            case "booking": await adapter.CreateBookingAsync(request); break;
            case "cancellation": await adapter.CancelBookingAsync(new() { SupplierBookingId = "reference" }); break;
        }
    }
}
