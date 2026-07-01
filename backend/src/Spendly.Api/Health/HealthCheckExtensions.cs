using Microsoft.Extensions.Options;
using Spendly.Api.Configuration;

namespace Spendly.Api.Health;

public static class HealthCheckExtensions
{
    public static IServiceCollection AddApiHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks();

        return services;
    }

    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var options = endpoints.ServiceProvider
            .GetRequiredService<IOptions<HealthChecksOptions>>()
            .Value;

        if (!options.Enabled)
        {
            return endpoints;
        }

        endpoints.MapHealthChecks(options.Path);

        return endpoints;
    }
}
