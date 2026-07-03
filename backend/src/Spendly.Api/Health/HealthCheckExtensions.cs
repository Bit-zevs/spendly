using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Spendly.Api.Configuration;

namespace Spendly.Api.Health;

public static class HealthCheckExtensions
{
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

        endpoints.MapHealthChecks(options.LivePath, new HealthCheckOptions
            {
                Predicate = _ => false,
                ResponseWriter = WriteHealthCheckResponseAsync
            })
            .AllowAnonymous()
            .WithName("GetLivenessHealth")
            .WithTags("Health Checks");

        endpoints.MapHealthChecks(options.ReadyPath, new HealthCheckOptions
            {
                Predicate = check => check.Tags.Contains(ReadyTag, StringComparer.OrdinalIgnoreCase),
                ResponseWriter = WriteHealthCheckResponseAsync
            })
            .AllowAnonymous()
            .WithName("GetReadinessHealth")
            .WithTags("Health Checks");

        return endpoints;
    }

    private static Task WriteHealthCheckResponseAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";

        var response = new HealthCheckResponse(
            Status: report.Status.ToString(),
            TotalDuration: report.TotalDuration,
            Entries: report.Entries.ToDictionary(
                entry => entry.Key,
                entry => new HealthCheckEntryResponse(
                    Status: entry.Value.Status.ToString(),
                    Description: entry.Value.Description,
                    Duration: entry.Value.Duration)));

        return context.Response.WriteAsJsonAsync(response);
    }

    private sealed record HealthCheckResponse(
        string Status,
        TimeSpan TotalDuration,
        IReadOnlyDictionary<string, HealthCheckEntryResponse> Entries);

    private sealed record HealthCheckEntryResponse(
        string Status,
        string? Description,
        TimeSpan Duration);
}
