using System.Net;
using System.Net.Http.Json;
using SupplierContracts;
using IntegrationService.Adapters.Interfaces;
using IntegrationService.Models.Cde;
using IntegrationService.Models.Common;
using System.Text.Json;

namespace IntegrationService.Adapters
{
    public class CdeSupplierAdapter : ISupplierAdapter
    {
        private readonly HttpClient _httpClient;
        public CdeSupplierAdapter(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public string SupplierCode => "CDE";
        public string SupplierName => "CDE Activities";
        public async Task<List<NormalizedProduct>> GetProductsAsync(CancellationToken cancellationToken = default)
        {
            using var message = new HttpRequestMessage(HttpMethod.Get, "api/activities");
            var products = await SupplierResponseReader.SendAsync<List<CdeActivity>>(
                _httpClient, message, MapError, cancellationToken);
            return products.Select(MapToNormalizedProduct).ToList();
        }

        private static NormalizedProduct MapToNormalizedProduct(CdeActivity activity)
        {
            var attributes = new
            {
                durationMinutes = activity.DurationMinutes,
                timeSlots = activity.TimeSlots,
                options = activity.Packages.Select(package => new
                {
                    supplierOptionId = package.PackageId,
                    label = package.Label,
                    maxParticipants = package.MaxParticipants,
                    passengerTypes = package.PassengerTypes
                }).ToList()
            };
            return new NormalizedProduct
            {
                SupplierCode = "CDE",
                ProductType = "Activity",
                SupplierProductId = activity.ActivityId,
                Name = activity.Title,
                Location = activity.Destination,
                Attributes = JsonSerializer.SerializeToElement(attributes),
                Content = new NormalizedProductContent
                {
                    Description = activity.Overview,
                    Images = activity.Gallery,
                    Inclusions = activity.Includes,
                    Exclusions = activity.Excludes
                }
            };
        }

        public async Task<NormalizedAvailabilityResponse> CheckAvailabilityAsync(
            NormalizedAvailabilityRequest request,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.SupplierProductId) ||
                string.IsNullOrWhiteSpace(request.SupplierOptionId))
            {
                throw new ArgumentException(
                    "Product ID and option ID are required.");
            }

            if (!request.Date.HasValue ||
                string.IsNullOrWhiteSpace(request.TimeSlot))
            {
                throw new ArgumentException(
                    "Activity date and time slot are required.");
            }

            if (request.Adults < 0 ||
                request.Children < 0 ||
                request.Adults + request.Children < 1)
            {
                throw new ArgumentException(
                    "At least one participant is required.");
            }

            var activityId = Uri.EscapeDataString(
                request.SupplierProductId);

            var packageId = Uri.EscapeDataString(
                request.SupplierOptionId);

            var timeSlot = Uri.EscapeDataString(
                request.TimeSlot);

            var date = request.Date.Value;

            var url =
                $"api/activities/{activityId}/availability" +
                $"?packageId={packageId}" +
                $"&date={date:yyyy-MM-dd}" +
                $"&timeSlot={timeSlot}" +
                $"&adults={request.Adults}" +
                $"&children={request.Children}";

            using var message = new HttpRequestMessage(HttpMethod.Get, url);
            var result = await SupplierResponseReader.SendAsync<CdeActivityAvailabilityResponse>(
                _httpClient, message, MapError, cancellationToken);

            return new NormalizedAvailabilityResponse
            {
                SupplierCode = SupplierCode,
                SupplierProductId = result.ActivityId,
                SupplierOptionId = result.PackageId,

                Available = result.Available,
                AvailableQuantity = result.RemainingSeats,

                Currency = result.CurrencyCode,
                TotalPrice = result.PayableAmount
            };
        }


        public async Task<NormalizedBookingResponse> CreateBookingAsync(
            NormalizedBookingRequest request,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.SupplierProductId) ||
                string.IsNullOrWhiteSpace(request.SupplierOptionId) ||
                string.IsNullOrWhiteSpace(request.IdempotencyKey) ||
                string.IsNullOrWhiteSpace(request.CustomerName))
            {
                throw new ArgumentException(
                    "Product, option, idempotency key and customer are required.");
            }

            if (!request.Date.HasValue ||
                string.IsNullOrWhiteSpace(request.TimeSlot))
            {
                throw new ArgumentException(
                    "Activity date and time slot are required.");
            }

            if (request.Adults < 0 ||
                request.Children < 0 ||
                request.Adults + request.Children < 1)
            {
                throw new ArgumentException(
                    "Invalid participant count.");
            }

            // Translate the normalized request to CDE's contract.
            var cdeRequest = new
            {
                idempotencyKey = request.IdempotencyKey,
                activityId = request.SupplierProductId,
                packageId = request.SupplierOptionId,
                date = request.Date.Value,
                timeSlot = request.TimeSlot,
                adults = request.Adults,
                children = request.Children,
                customerName = request.CustomerName
            };

            using var message = new HttpRequestMessage(HttpMethod.Post, "api/activities/bookings")
            {
                Content = JsonContent.Create(cdeRequest)
            };
            var booking = await SupplierResponseReader.SendAsync<CdeActivityBookingResponse>(
                _httpClient, message, MapError, cancellationToken);

            return new NormalizedBookingResponse
            {
                SupplierCode = SupplierCode,
                SupplierBookingId = booking.BookingId,
                SupplierProductId = booking.ActivityId,
                SupplierOptionId = booking.PackageId,
                Status = MapStatus(booking.Status, booking.BookingId),
                Currency = booking.CurrencyCode,
                TotalPrice = booking.PayableAmount
            };
        }


        public async Task<NormalizedCancellationResponse> CancelBookingAsync(
            NormalizedCancellationRequest request,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.SupplierBookingId))
                throw new ArgumentException("Booking ID is required.");

            var bookingId = Uri.EscapeDataString(
                request.SupplierBookingId);

            using var message = new HttpRequestMessage(HttpMethod.Post,
                $"api/activities/bookings/{bookingId}/cancel");
            var booking = await SupplierResponseReader.SendAsync<CdeActivityBookingResponse>(
                _httpClient, message, MapError, cancellationToken);

            return new NormalizedCancellationResponse
            {
                SupplierCode = SupplierCode,
                SupplierBookingId = booking.BookingId,
                Status = MapStatus(booking.Status, booking.BookingId)
            };
        }

        // Only statuses defined by this supplier's contract are recognized.
        private static SupplierBookingStatus MapStatus(string? status, string? reference) =>
            string.IsNullOrWhiteSpace(reference) ? SupplierBookingStatus.Unknown : status?.Trim().ToUpperInvariant() switch
        {
            "CONFIRMED" => SupplierBookingStatus.Confirmed,
            "CANCELLED" => SupplierBookingStatus.Cancelled,
            _ => SupplierBookingStatus.Unknown
        };

        // This mock contract rejects invalid/missing/conflicting requests before booking.
        // Other HTTP failures do not establish whether a booking was created.
        private static SupplierError MapError(HttpStatusCode statusCode) => statusCode switch
        {
            HttpStatusCode.BadRequest => SupplierError.Create(SupplierErrorCode.InvalidRequest, SupplierErrorOutcome.Rejected),
            HttpStatusCode.NotFound => SupplierError.Create(SupplierErrorCode.NotFound, SupplierErrorOutcome.Rejected),
            HttpStatusCode.Conflict => SupplierError.Create(SupplierErrorCode.Conflict, SupplierErrorOutcome.Rejected),
            HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => SupplierError.Create(SupplierErrorCode.AuthenticationFailed),
            HttpStatusCode.TooManyRequests => SupplierError.Create(SupplierErrorCode.RateLimited),
            HttpStatusCode.RequestTimeout or HttpStatusCode.GatewayTimeout => SupplierError.Create(SupplierErrorCode.Timeout),
            _ => SupplierError.Create(SupplierErrorCode.SupplierFailure)
        };
    }
}
