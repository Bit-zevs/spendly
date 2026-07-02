using Microsoft.AspNetCore.WebUtilities;

namespace Spendly.Api.Errors;

internal static class ProblemDetailsDefaults
{
    public const string TraceIdExtensionName = "traceId";

    public static string GetType(int statusCode)
    {
        return $"https://httpstatuses.com/{statusCode}";
    }

    public static string GetTitle(int statusCode)
    {
        var reasonPhrase = ReasonPhrases.GetReasonPhrase(statusCode);

        return string.IsNullOrWhiteSpace(reasonPhrase)
            ? "Error"
            : reasonPhrase;
    }

    public static string GetDetail(int statusCode)
    {
        return statusCode switch
        {
            StatusCodes.Status400BadRequest => "The request is invalid.",
            StatusCodes.Status401Unauthorized => "Authentication is required to access this resource.",
            StatusCodes.Status403Forbidden => "You do not have permission to access this resource.",
            StatusCodes.Status404NotFound => "The requested resource was not found.",
            StatusCodes.Status409Conflict => "The request conflicts with the current state of the resource.",
            StatusCodes.Status500InternalServerError => "An unexpected error occurred.",
            _ => "An error occurred while processing the request."
        };
    }
}
