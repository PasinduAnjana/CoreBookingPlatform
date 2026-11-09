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
    }
}
