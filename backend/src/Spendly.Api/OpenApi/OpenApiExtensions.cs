using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Scalar.AspNetCore;
using Spendly.Api.Configuration;

namespace Spendly.Api.OpenApi;

public static class OpenApiExtensions
{
    private const string InvalidConfigurationDocumentName = "invalid-configuration";

    public static IServiceCollection AddApiOpenApi(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var documentName = GetOpenApiDocumentName(configuration);

        services.AddOpenApi(documentName, options =>
        {
            options.AddDocumentTransformer((document, context, _) =>
            {
                var applicationOptions = context.ApplicationServices
                    .GetRequiredService<IOptions<ApplicationOptions>>()
                    .Value;

                document.Info = new OpenApiInfo
                {
                    Title = applicationOptions.DisplayName,
                    Version = applicationOptions.Version,
                    Description = "HTTP API for the Spendly personal finance application."
                };

                return Task.CompletedTask;
            });
        });

        return services;
    }

    public static WebApplication MapApiOpenApi(this WebApplication app)
    {
        var openApiOptions = app.Services
            .GetRequiredService<IOptions<OpenApiOptions>>()
            .Value;

        if (!openApiOptions.Enabled || !app.Environment.IsDevelopment())
        {
            return app;
        }

        var applicationOptions = app.Services
            .GetRequiredService<IOptions<ApplicationOptions>>()
            .Value;

        var documentName = applicationOptions.Version;
        var openApiEndpoint = openApiOptions.Endpoint;
        var scalarEndpoint = openApiOptions.ScalarEndpoint;

        app.MapOpenApi(openApiEndpoint)
            .AllowAnonymous();

        app.MapScalarApiReference(scalarEndpoint, options =>
            {
                options
                    .WithTitle($"{applicationOptions.DisplayName} {applicationOptions.Version}")
                    .WithOpenApiRoutePattern(openApiEndpoint)
                    .AddDocument(
                        documentName,
                        $"{applicationOptions.DisplayName} {applicationOptions.Version}",
                        openApiEndpoint,
                        isDefault: true)
                    .DisableAgent();
            })
            .AllowAnonymous();

        return app;
    }

    private static string GetOpenApiDocumentName(IConfiguration configuration)
    {
        var documentName = configuration
            .GetRequiredSection(ApplicationOptions.SectionName)[nameof(ApplicationOptions.Version)];

        return string.IsNullOrWhiteSpace(documentName)
            ? InvalidConfigurationDocumentName
            : documentName;
    }
}
