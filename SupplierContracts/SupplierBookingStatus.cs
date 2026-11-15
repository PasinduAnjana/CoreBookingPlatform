using System.Text.Json.Serialization;

namespace SupplierContracts;

[JsonConverter(typeof(StringEnumConverter<SupplierBookingStatus>))]
public enum SupplierBookingStatus
{
    Unknown,
    Pending,
    Confirmed,
    Cancelled,
    Failed
}
