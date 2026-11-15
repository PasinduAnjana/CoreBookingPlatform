using System.Text.Json.Serialization;

namespace SupplierContracts;

[JsonConverter(typeof(StringEnumConverter<SupplierErrorCode>))]
public enum SupplierErrorCode
{
    Unknown,
    InvalidRequest,
    NotFound,
    Conflict,
    AuthenticationFailed,
    RateLimited,
    Timeout,
    Unreachable,
    InvalidResponse,
    SupplierFailure
}

[JsonConverter(typeof(StringEnumConverter<SupplierErrorOutcome>))]
public enum SupplierErrorOutcome
{
    Unknown,
    Rejected
}

public sealed record SupplierError
{
    public SupplierErrorCode Code { get; init; }
    public SupplierErrorOutcome Outcome { get; init; }
    public string Message { get; init; } = "Supplier operation could not be completed.";

    // Messages crossing service boundaries never contain supplier bodies or credentials.
    public static SupplierError Create(SupplierErrorCode code,
        SupplierErrorOutcome outcome = SupplierErrorOutcome.Unknown) => new()
    {
        Code = code,
        Outcome = outcome,
        Message = code switch
        {
            SupplierErrorCode.InvalidRequest => "The supplier rejected the request details.",
            SupplierErrorCode.NotFound => "The requested supplier resource was not found.",
            SupplierErrorCode.Conflict => "The supplier rejected the operation because of a conflict.",
            SupplierErrorCode.AuthenticationFailed => "Supplier authentication failed.",
            SupplierErrorCode.RateLimited => "The supplier request limit was reached.",
            SupplierErrorCode.Timeout => "The supplier request timed out; its outcome may be unknown.",
            SupplierErrorCode.Unreachable => "The supplier could not be reached; its outcome may be unknown.",
            SupplierErrorCode.InvalidResponse => "The supplier returned an invalid response; its outcome may be unknown.",
            _ => "Supplier operation could not be completed; its outcome may be unknown."
        }
    };
}
