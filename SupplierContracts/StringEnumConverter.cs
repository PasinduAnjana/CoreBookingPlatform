using System.Text.Json.Serialization;

namespace SupplierContracts;

// Numeric enum values are not part of the HTTP contract.
public sealed class StringEnumConverter<TEnum>()
    : JsonStringEnumConverter<TEnum>(namingPolicy: null, allowIntegerValues: false)
    where TEnum : struct, Enum;
