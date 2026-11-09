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
        public async Task<List<NormalizedProduct>> GetProductsAsync(CancellationToken cancellationToken = default)
        {
            var hotels = await _httpClient.GetFromJsonAsync<List<CdeActivity>>("api/activities", cancellationToken);
            if (hotels == null)
            {
                return [];
            }
            return hotels.Select(MapToNormalizedProduct).ToList();
        }

        private static NormalizedProduct MapToNormalizedProduct(CdeActivity activity)
        {
            var attributes = new
            {
                durationMinutes = activity.DurationMinutes,
                options = activity.Packages.Select(package => new
                {
                    supplierPackageId = package.PackageId,
                    label = package.Label,
                    maxParticipants = package.MaxParticipants,
                    passengerTypes = package.PassengerTypes
                }).ToList()
            };
            return new NormalizedProduct
            {
                SupplierCode = "CDE",
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
    }
}
