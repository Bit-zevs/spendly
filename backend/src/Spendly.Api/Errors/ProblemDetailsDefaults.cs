using Microsoft.AspNetCore.WebUtilities;

namespace Spendly.Api.Errors;

internal static class ProblemDetailsDefaults
{
    public const string CodeExtensionName = "code";
    public const string TraceIdExtensionName = "traceId";

    public static string GetTypeUri()
    {
        return "about:blank";
    }

    public static string GetTitle(int statusCode)
    {
        var reasonPhrase = ReasonPhrases.GetReasonPhrase(statusCode);

        return string.IsNullOrWhiteSpace(reasonPhrase)
            ? "Error"
            : reasonPhrase;
    }

    public static string GetCode(int statusCode)
    {
        return statusCode switch
        {
            StatusCodes.Status400BadRequest => "bad_request",
            StatusCodes.Status401Unauthorized => "unauthorized",
            StatusCodes.Status403Forbidden => "forbidden",
            StatusCodes.Status404NotFound => "not_found",
            StatusCodes.Status405MethodNotAllowed => "method_not_allowed",
            StatusCodes.Status409Conflict => "conflict",
            StatusCodes.Status422UnprocessableEntity => "unprocessable_entity",
            StatusCodes.Status500InternalServerError => "internal_server_error",
            _ => "http_error"
        };
    }

    public static string GetDetail(int statusCode)
    {
        return statusCode switch
        {
            StatusCodes.Status400BadRequest => "The request is invalid.",
            StatusCodes.Status401Unauthorized => "Authentication is required to access this resource.",
            StatusCodes.Status403Forbidden => "You do not have permission to access this resource.",
            StatusCodes.Status404NotFound => "The requested resource was not found.",
            StatusCodes.Status405MethodNotAllowed => "The HTTP method is not supported for this resource.",
            StatusCodes.Status409Conflict => "The request conflicts with the current state of the resource.",
            StatusCodes.Status422UnprocessableEntity => "The request could not be processed.",
            StatusCodes.Status500InternalServerError => "An unexpected error occurred.",
            _ => "An error occurred while processing the request."
        };
    }
}
