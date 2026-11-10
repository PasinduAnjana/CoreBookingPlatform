namespace OrderService.Dtos
{
    public class ExternalServiceDtos
    {
        public class ProductLookupDto
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public string SupplierCode { get; set; } = string.Empty;
            public string SupplierProductId { get; set; }
                = string.Empty;
            public string? AttributesJson { get; set; }
            public bool IsActive { get; set; }
        }

        public class AvailabilityRequestDto
        {
            public string SupplierCode { get; set; } = string.Empty;
            public string SupplierProductId { get; set; } = string.Empty;
            public string SupplierOptionId { get; set; } = string.Empty;

            public DateOnly? CheckIn { get; set; }
            public DateOnly? CheckOut { get; set; }

            public DateOnly? Date { get; set; }
            public string? TimeSlot { get; set; }

            public int Adults { get; set; }
            public int Children { get; set; }
            public int Rooms { get; set; } = 1;
        }

        public class AvailabilityResponseDto
        {
            public string SupplierCode { get; set; } = string.Empty;
            public string SupplierProductId { get; set; } = string.Empty;
            public string SupplierOptionId { get; set; } = string.Empty;

            public bool Available { get; set; }
            public int AvailableQuantity { get; set; }
            public string Currency { get; set; } = string.Empty;
            public decimal TotalPrice { get; set; }
        }
    }
}
