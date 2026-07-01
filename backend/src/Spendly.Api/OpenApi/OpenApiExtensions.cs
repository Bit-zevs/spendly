using Microsoft.Extensions.Options;
using Spendly.Api.Configuration;

namespace Spendly.Api.OpenApi;

public static class OpenApiExtensions
{
    public static IServiceCollection AddApiOpenApi(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var documentName = configuration
            .GetRequiredSection(OpenApiOptions.SectionName)[nameof(OpenApiOptions.DocumentName)];

        services.AddOpenApi(string.IsNullOrWhiteSpace(documentName) ? "v1" : documentName);

        return services;
    }

    public static WebApplication MapApiOpenApi(this WebApplication app)
    {
        var options = app.Services
            .GetRequiredService<IOptions<OpenApiOptions>>()
            .Value;

        if (!options.Enabled)
        {
            return app;
        }

        app.MapOpenApi(options.Endpoint);

        return app;
    }
}
