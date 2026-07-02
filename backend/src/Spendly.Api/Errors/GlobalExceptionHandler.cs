using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Spendly.Api.Errors;

public sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (httpContext.Response.HasStarted)
        {
            logger.LogWarning(
                exception,
                "Unhandled exception occurred, but the response has already started. TraceId: {TraceId}",
                httpContext.TraceIdentifier);

            return false;
        }

        logger.LogError(
            exception,
            "Unhandled exception occurred while processing request. TraceId: {TraceId}",
            httpContext.TraceIdentifier);

        const int statusCode = StatusCodes.Status500InternalServerError;

        httpContext.Response.StatusCode = statusCode;

        var problemDetails = new ProblemDetails
        {
            Type = ProblemDetailsDefaults.GetType(statusCode),
            Title = ProblemDetailsDefaults.GetTitle(statusCode),
            Status = statusCode,
            Detail = ProblemDetailsDefaults.GetDetail(statusCode)
        };

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = problemDetails
        });
    }
}
