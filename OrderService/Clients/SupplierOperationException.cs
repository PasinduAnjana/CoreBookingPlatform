using System.Net;
using SupplierContracts;

namespace OrderService.Clients;

public sealed class SupplierOperationException(SupplierError error, HttpStatusCode statusCode)
    : HttpRequestException(error.Message, inner: null, statusCode)
{
    public SupplierError Error { get; } = error;
}
