using SupplierContracts;

namespace IntegrationService.Adapters;

public sealed class SupplierOperationException(SupplierError error, Exception? innerException = null)
    : Exception(error.Message, innerException)
{
    public SupplierError Error { get; } = error;
}
