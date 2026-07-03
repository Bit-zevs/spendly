using Microsoft.Extensions.Options;
using Spendly.Api.Configuration;
using Spendly.Api.Contracts;

namespace Spendly.Api.Endpoints;

public static class RootEndpointExtensions
{
    public static IEndpointRouteBuilder MapRootEndpoint(this IEndpointRouteBuilder endpoints)
    {
        var applicationOptions = endpoints.ServiceProvider
            .GetRequiredService<IOptions<ApplicationOptions>>()
            .Value;

        endpoints.MapGet("/", () => Results.Ok(new ApiStatusResponse(
                Application: applicationOptions.DisplayName,
                Status: "Running")))
            .AllowAnonymous()
            .WithName("GetApiStatus")
            .WithTags("Status")
            .WithSummary("Get API status")
            .WithDescription("Returns the current Spendly API status.")
            .Produces<ApiStatusResponse>();

        return endpoints;
    }
}
