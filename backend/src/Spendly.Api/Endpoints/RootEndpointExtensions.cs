using Spendly.Api.Contracts;

namespace Spendly.Api.Endpoints;

public static class RootEndpointExtensions
{
    public static IEndpointRouteBuilder MapRootEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/", () => Results.Ok(new ApiStatusResponse(
                Application: "Spendly API",
                Status: "Running")))
            .WithName("GetApiStatus")
            .WithTags("Status");

        return endpoints;
    }
}
