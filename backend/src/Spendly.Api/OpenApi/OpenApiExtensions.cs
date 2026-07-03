using Microsoft.Extensions.Options;
using Scalar.AspNetCore;
using Spendly.Api.Configuration;

namespace Spendly.Api.OpenApi;

public static class OpenApiExtensions
{
    private const string DefaultDocumentName = "v0.2";
    private const string DefaultOpenApiEndpoint = "/openapi/{documentName}.json";
    private const string DefaultScalarEndpoint = "/docs";

    public static IServiceCollection AddApiOpenApi(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var documentName = configuration
            .GetRequiredSection(OpenApiOptions.SectionName)[nameof(OpenApiOptions.DocumentName)];

        documentName = string.IsNullOrWhiteSpace(documentName)
            ? DefaultDocumentName
            : documentName;

        services.AddOpenApi(documentName, options =>
        {
            options.AddDocumentTransformer((document, context, _) =>
            {
                var applicationOptions = context.ApplicationServices
                    .GetRequiredService<IOptions<ApplicationOptions>>()
                    .Value;

                document.Info = new()
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

        var documentName = string.IsNullOrWhiteSpace(openApiOptions.DocumentName)
            ? DefaultDocumentName
            : openApiOptions.DocumentName;

        var openApiEndpoint = string.IsNullOrWhiteSpace(openApiOptions.Endpoint)
            ? DefaultOpenApiEndpoint
            : openApiOptions.Endpoint;

        var scalarEndpoint = string.IsNullOrWhiteSpace(openApiOptions.ScalarEndpoint)
            ? DefaultScalarEndpoint
            : openApiOptions.ScalarEndpoint;

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
}
