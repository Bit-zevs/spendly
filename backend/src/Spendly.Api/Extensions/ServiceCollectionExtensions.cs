using Spendly.Api.Health;
using Spendly.Api.OpenApi;

namespace Spendly.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApiServices(this IServiceCollection services)
    {
        services.AddControllers();
        services.AddApiHealthChecks();
        services.AddApiOpenApi();

        return services;
    }
}
