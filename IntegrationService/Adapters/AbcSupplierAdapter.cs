using System.Net;
using System.Net.Http.Json;
using SupplierContracts;
using IntegrationService.Adapters.Interfaces;
using IntegrationService.Models.Abc;
using IntegrationService.Models.Common;
using System.Text.Json;

namespace IntegrationService.Adapters
{
    public class AbcSupplierAdapter:ISupplierAdapter
    {
        private readonly HttpClient _httpClient;
        public AbcSupplierAdapter(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public string SupplierCode => "ABC";
        public string SupplierName => "ABC Hotels";
        public async Task<List<NormalizedProduct>> GetProductsAsync(CancellationToken cancellationToken = default)
        {
            using var message = new HttpRequestMessage(HttpMethod.Get, "api/hotels");
            var products = await SupplierResponseReader.SendAsync<List<AbcHotel>>(
                _httpClient, message, MapError, cancellationToken);
            return products.Select(MapToNormalizedProduct).ToList();
        }

        private static NormalizedProduct MapToNormalizedProduct(AbcHotel hotel)
        {
            var attributes = new
            {
                starRating = hotel.StarRating,
                options = hotel.RoomTypes.Select(room => new
                {
                    supplierOptionId = room.RoomCode,
                    name = room.RoomName,
                    maxAdults = room.MaxAdults,
                    maxChildren = room.MaxChildren,
                    bedType = room.BedType
                }).ToList()
            };
            return new NormalizedProduct
            {
                SupplierCode = "ABC",
                ProductType = "Hotel",
                SupplierProductId = hotel.HotelCode,
                Name = hotel.HotelName,
                Location = hotel.City,

                Attributes = JsonSerializer.SerializeToElement(attributes),

                Content = new NormalizedProductContent
                {
                    Description = hotel.Description,
                    Images = hotel.Images,
                    Facilities = hotel.Facilities
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

            if (!request.CheckIn.HasValue ||
                !request.CheckOut.HasValue)
            {
                throw new ArgumentException(
                    "Check-in and check-out dates are required.");
            }

            var checkIn = request.CheckIn.Value;
            var checkOut = request.CheckOut.Value;

            if (checkOut <= checkIn)
                throw new ArgumentException("Invalid hotel dates.");

            if (request.Adults < 1 ||
                request.Children < 0 ||
                request.Rooms < 1)
            {
                throw new ArgumentException("Invalid guest or room count.");
            }

            var hotelCode = Uri.EscapeDataString(
                request.SupplierProductId);

            var roomCode = Uri.EscapeDataString(
                request.SupplierOptionId);

            var url =
                $"api/hotels/{hotelCode}/availability" +
                $"?roomCode={roomCode}" +
                $"&checkIn={checkIn:yyyy-MM-dd}" +
                $"&checkOut={checkOut:yyyy-MM-dd}" +
                $"&adults={request.Adults}" +
                $"&children={request.Children}" +
                $"&rooms={request.Rooms}";

            using var message = new HttpRequestMessage(HttpMethod.Get, url);
            var result = await SupplierResponseReader.SendAsync<AbcHotelAvailabilityResponse>(
                _httpClient, message, MapError, cancellationToken);

            return new NormalizedAvailabilityResponse
            {
                SupplierCode = SupplierCode,
                SupplierProductId = result.HotelCode,
                SupplierOptionId = result.RoomCode,

                Available = result.IsAvailable,
                AvailableQuantity = result.RoomsLeft,

                Currency = result.Currency,
                TotalPrice = result.TotalAmount
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

            if (!request.CheckIn.HasValue ||
                !request.CheckOut.HasValue ||
                request.CheckOut <= request.CheckIn)
            {
                throw new ArgumentException("Invalid hotel dates.");
            }

            if (request.Adults < 1 ||
                request.Children < 0 ||
                request.Rooms < 1)
            {
                throw new ArgumentException(
                    "Invalid guest or room count.");
            }

            // Translate the normalized request to ABC's contract.
            var abcRequest = new
            {
                idempotencyKey = request.IdempotencyKey,
                hotelCode = request.SupplierProductId,
                roomCode = request.SupplierOptionId,
                checkIn = request.CheckIn.Value,
                checkOut = request.CheckOut.Value,
                adults = request.Adults,
                children = request.Children,
                rooms = request.Rooms,
                guestName = request.CustomerName
            };

            using var message = new HttpRequestMessage(HttpMethod.Post, "api/hotels/bookings")
            {
                Content = JsonContent.Create(abcRequest)
            };
            var booking = await SupplierResponseReader.SendAsync<AbcHotelBookingResponse>(
                _httpClient, message, MapError, cancellationToken);

            return new NormalizedBookingResponse
            {
                SupplierCode = SupplierCode,
                SupplierBookingId = booking.BookingId,
                SupplierProductId = booking.HotelCode,
                SupplierOptionId = booking.RoomCode,
                Status = MapStatus(booking.Status, booking.BookingId),
                Currency = booking.Currency,
                TotalPrice = booking.TotalAmount
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
                $"api/hotels/bookings/{bookingId}/cancel");
            var booking = await SupplierResponseReader.SendAsync<AbcHotelBookingResponse>(
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
