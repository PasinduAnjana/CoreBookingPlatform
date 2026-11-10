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

            var result = await _httpClient.GetFromJsonAsync<
                CdeActivityAvailabilityResponse>(
                    url,
                    cancellationToken);

            if (result == null)
            {
                throw new InvalidOperationException(
                    "CDE returned an empty availability response.");
            }

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

    }
}
