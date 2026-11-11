
using System.Net;

namespace IntegrationService.Adapters
{
    public class SupplierApiException : Exception
    {
        public HttpStatusCode StatusCode { get; }

        public SupplierApiException(
            HttpStatusCode statusCode,
            string message) : base(message)
        {
            StatusCode = statusCode;
        }
    }
}
