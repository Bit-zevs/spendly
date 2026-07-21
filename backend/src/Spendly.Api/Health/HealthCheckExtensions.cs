using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Spendly.Api.Configuration;
using Spendly.Api.Contracts.Health;

namespace Spendly.Api.Health;

public static class HealthCheckExtensions
{
    private const string JsonContentType = "application/json";
    private const string ReadyTag = "ready";
    private const string SelfCheckName = "self";

    public static IServiceCollection AddApiHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck(
                name: SelfCheckName,
                check: () => HealthCheckResult.Healthy("Application is running."),
                tags: [ReadyTag]);

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

        endpoints.MapGet(
                options.LivePath,
                async (HealthCheckService healthCheckService, CancellationToken cancellationToken) =>
                    await ExecuteHealthCheckAsync(
                        healthCheckService,
                        predicate: _ => false,
                        cancellationToken))
            .AllowAnonymous()
            .WithName("GetLivenessHealth")
            .WithTags("Health Checks")
            .WithSummary("Get liveness health")
            .WithDescription("Checks whether the API process is alive and able to respond to HTTP requests.")
            .Produces<HealthCheckResponse>(StatusCodes.Status200OK, JsonContentType)
            .Produces<HealthCheckResponse>(StatusCodes.Status503ServiceUnavailable, JsonContentType);

        endpoints.MapGet(
                options.ReadyPath,
                async (HealthCheckService healthCheckService, CancellationToken cancellationToken) =>
                    await ExecuteHealthCheckAsync(
                        healthCheckService,
                        predicate: check => check.Tags.Contains(ReadyTag, StringComparer.OrdinalIgnoreCase),
                        cancellationToken))
            .AllowAnonymous()
            .WithName("GetReadinessHealth")
            .WithTags("Health Checks")
            .WithSummary("Get readiness health")
            .WithDescription("Checks whether the API and its required dependencies are ready to serve traffic.")
            .Produces<HealthCheckResponse>(StatusCodes.Status200OK, JsonContentType)
            .Produces<HealthCheckResponse>(StatusCodes.Status503ServiceUnavailable, JsonContentType);

        return endpoints;
    }

    private static async Task<IResult> ExecuteHealthCheckAsync(
        HealthCheckService healthCheckService,
        Func<HealthCheckRegistration, bool> predicate,
        CancellationToken cancellationToken)
    {
        var report = await healthCheckService.CheckHealthAsync(predicate, cancellationToken);

        var response = new HealthCheckResponse(
            Status: report.Status.ToString(),
            TotalDuration: report.TotalDuration,
            Entries: report.Entries.ToDictionary(
                entry => entry.Key,
                entry => new HealthCheckEntryResponse(
                    Status: entry.Value.Status.ToString(),
                    Description: entry.Value.Description,
                    Duration: entry.Value.Duration)));

        var statusCode = GetStatusCode(report.Status);

        return Results.Json(
            response,
            contentType: JsonContentType,
            statusCode: statusCode);
    }

    private static int GetStatusCode(HealthStatus status)
    {
        return status == HealthStatus.Unhealthy
            ? StatusCodes.Status503ServiceUnavailable
            : StatusCodes.Status200OK;
    }
}
