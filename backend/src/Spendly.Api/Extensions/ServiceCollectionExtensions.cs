using Spendly.Api.Configuration;
using Spendly.Api.Errors;
using Spendly.Api.Health;
using Spendly.Api.OpenApi;

namespace Spendly.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApiServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddApiConfigurationOptions(configuration);

        services.AddControllers();
        services.AddApiProblemDetails();
        services.AddApiHealthChecks();
        services.AddApiOpenApi(configuration);

        return services;
    }
}
