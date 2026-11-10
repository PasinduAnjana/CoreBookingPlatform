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
        public async Task<List<NormalizedProduct>> GetProductsAsync(CancellationToken cancellationToken = default)
        {
            var hotels = await _httpClient.GetFromJsonAsync<List<AbcHotel>>("api/hotels", cancellationToken);
            if(hotels == null)
            {
                return [];
            }
            return hotels.Select(MapToNormalizedProduct).ToList();
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

            using var response = await _httpClient.GetAsync(url, cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                var message = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new ArgumentException(message);
            }

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<AbcHotelAvailabilityResponse>(
                cancellationToken: cancellationToken);

            if (result == null)
            {
                throw new InvalidOperationException(
                    "ABC returned an empty availability response.");
            }

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

    }
}
