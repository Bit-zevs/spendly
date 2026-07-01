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
            .WithName("GetApiStatus")
            .WithTags("Status");

        return endpoints;
    }
}
