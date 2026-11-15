using OrderService.DTOs;
using System.Text.Json;
using static OrderService.Dtos.ExternalServiceDtos;

namespace OrderService.Services
{
    public static class BookingSelectionValidator
    {
        private static void ValidateBookingSelection(AddCartItemDto dto)
        {
            if (dto.ProductId <= 0 || string.IsNullOrWhiteSpace(dto.SupplierOptionId))
            {
                throw new ArgumentException(
                    "Product and supplier option are required.");
            }

            if (dto.Adults < 0 || dto.Children < 0 ||
                (long)dto.Adults + dto.Children < 1 ||
                dto.Rooms < 1)
            {
                throw new ArgumentException(
                    "Invalid guest or room count.");
            }

            bool hasHotelDates = dto.CheckIn.HasValue || dto.CheckOut.HasValue;

            bool hasActivityDetails =dto.ActivityDate.HasValue || !string.IsNullOrWhiteSpace(dto.TimeSlot);

            if (hasHotelDates == hasActivityDetails)
            {
                throw new ArgumentException("Specify either hotel dates or activity details.");
            }

            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            if (hasHotelDates)
            {
                if (!dto.CheckIn.HasValue ||
                    !dto.CheckOut.HasValue ||
                    dto.CheckIn.Value < today ||
                    dto.CheckOut.Value <= dto.CheckIn.Value ||
                    dto.Adults < dto.Rooms)
                {
                    throw new ArgumentException("Invalid hotel booking selection.");
                }
            }
            else
            {
                if (!dto.ActivityDate.HasValue ||
                    dto.ActivityDate.Value < today ||
                    string.IsNullOrWhiteSpace(dto.TimeSlot) ||
                    dto.Rooms != 1)
                {
                    throw new ArgumentException(
                        "Invalid activity booking selection.");
                }
            }
        }


        public static void Validate(AddCartItemDto dto, ProductLookupDto product)
        {
            ValidateBookingSelection(dto);

            if (!product.IsActive)
                throw new ArgumentException("Product is inactive.");

            var productType = product.ProductType;

            if (productType == "Hotel" && (!dto.CheckIn.HasValue || !dto.CheckOut.HasValue))
            {
                throw new ArgumentException(
                    "Hotel requires check-in and check-out.");
            }

            if (productType == "Activity" &&
                (!dto.ActivityDate.HasValue ||
                 string.IsNullOrWhiteSpace(dto.TimeSlot)))
            {
                throw new ArgumentException(
                    "Activity requires a date and time slot.");
            }

            if (productType != "Hotel" && productType != "Activity")
                throw new ArgumentException("Unsupported product type.");

            if (string.IsNullOrWhiteSpace(product.AttributesJson))
                throw new ArgumentException("Product has no options.");

            JsonDocument document;
            try
            {
                document = JsonDocument.Parse(product.AttributesJson);
            }
            catch (JsonException)
            {
                throw new ArgumentException("Product options are invalid.");
            }

            using var parsedOptions = document;

            var root = document.RootElement;

            if (root.ValueKind != JsonValueKind.Object ||
                !root.TryGetProperty("options", out var options) ||
                options.ValueKind != JsonValueKind.Array)
            {
                throw new ArgumentException(
                    "Product options are missing.");
            }

            var selectedOption = options.EnumerateArray().FirstOrDefault(option =>
                option.ValueKind == JsonValueKind.Object &&
                (option.TryGetProperty("supplierOptionId", out var id) ||
                 (productType == "Activity" && option.TryGetProperty("supplierPackageId", out id))) &&
                id.ValueKind == JsonValueKind.String &&
                id.GetString() == dto.SupplierOptionId);

            if (selectedOption.ValueKind != JsonValueKind.Object)
                throw new ArgumentException(
                    "Selected option does not exist.");

            static int? Limit(JsonElement option, string property)
            {
                if (!option.TryGetProperty(property, out var value)) return null;
                if (value.ValueKind != JsonValueKind.Number || !value.TryGetInt32(out var limit) || limit < 0)
                    throw new ArgumentException("Product capacity limits are invalid.");
                return limit;
            }

            if (productType == "Hotel")
            {
                var maxAdults = Limit(selectedOption, "maxAdults");
                var maxChildren = Limit(selectedOption, "maxChildren");
                if ((maxAdults.HasValue && dto.Adults > (long)maxAdults.Value * dto.Rooms) ||
                    (maxChildren.HasValue && dto.Children > (long)maxChildren.Value * dto.Rooms))
                    throw new ArgumentException("The selected guests exceed this room's occupancy limits.");
            }
            else
            {
                if (!root.TryGetProperty("timeSlots", out var slots) ||
                    slots.ValueKind != JsonValueKind.Array ||
                    !slots.EnumerateArray().Any(s => s.ValueKind == JsonValueKind.String && s.GetString() == dto.TimeSlot))
                    throw new ArgumentException("Select a supported activity time slot.");

                var maxParticipants = Limit(selectedOption, "maxParticipants");
                if (maxParticipants.HasValue && (long)dto.Adults + dto.Children > maxParticipants.Value)
                    throw new ArgumentException("The selected participants exceed this package's capacity.");

                if (!selectedOption.TryGetProperty("passengerTypes", out var passengerTypes) ||
                    passengerTypes.ValueKind != JsonValueKind.Array)
                    throw new ArgumentException("Package passenger types are missing.");

                var supportedTypes = passengerTypes.EnumerateArray()
                    .Where(p => p.ValueKind == JsonValueKind.String)
                    .Select(p => p.GetString())
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
                if ((dto.Adults > 0 && !supportedTypes.Contains("Adult")) ||
                    (dto.Children > 0 && !supportedTypes.Contains("Child")))
                    throw new ArgumentException("The package does not support this participant selection.");
            }

        }

    }
}
