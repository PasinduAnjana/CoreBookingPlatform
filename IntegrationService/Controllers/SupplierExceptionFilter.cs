using IntegrationService.Adapters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SupplierContracts;

namespace IntegrationService.Controllers;

// Apply to supplier operation controllers, not unrelated application exceptions.
public sealed class SupplierExceptionFilter(ILogger<SupplierExceptionFilter> logger) : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        var error = context.Exception switch
        {
            SupplierOperationException ex => ex.Error,
            ArgumentException => SupplierError.Create(SupplierErrorCode.InvalidRequest, SupplierErrorOutcome.Rejected),
            KeyNotFoundException => SupplierError.Create(SupplierErrorCode.NotFound, SupplierErrorOutcome.Rejected),
            HttpRequestException => SupplierError.Create(SupplierErrorCode.Unreachable),
            OperationCanceledException when !context.HttpContext.RequestAborted.IsCancellationRequested
                => SupplierError.Create(SupplierErrorCode.Timeout),
            _ => null
        };

        if (error is null) return;

        logger.Log(error.Outcome == SupplierErrorOutcome.Rejected ? LogLevel.Information : LogLevel.Warning,
            "Supplier operation failed with {ErrorCode}; outcome {Outcome}; trace {TraceId}.",
            error.Code, error.Outcome, context.HttpContext.TraceIdentifier);

        var statusCode = error.Code switch
        {
            SupplierErrorCode.InvalidRequest => StatusCodes.Status400BadRequest,
            SupplierErrorCode.NotFound => StatusCodes.Status404NotFound,
            SupplierErrorCode.Conflict => StatusCodes.Status409Conflict,
            SupplierErrorCode.RateLimited => StatusCodes.Status503ServiceUnavailable,
            SupplierErrorCode.Timeout => StatusCodes.Status504GatewayTimeout,
            _ => StatusCodes.Status502BadGateway
        };

        context.Result = new ObjectResult(error) { StatusCode = statusCode };
        context.ExceptionHandled = true;
    }
}
