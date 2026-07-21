using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Spendly.Infrastructure.HealthChecks;

namespace Spendly.IntegrationTests;

public sealed class HealthCheckEndpointTests(SpendlyApiFactory factory)
    : IClassFixture<SpendlyApiFactory>
{
    [Fact]
    public async Task LivenessHealthEndpoint_ShouldReturnHealthyWithoutRunningReadinessChecks()
    {
        using var client = factory.CreateApiClient();

        using var response = await client.GetAsync(
            TestApiConstants.LivenessHealthPath,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

        var json = await response.Content.ReadAsStringAsync(
            TestContext.Current.CancellationToken);

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        var entries = root.GetProperty("entries");

        Assert.Equal("Healthy", root.GetProperty("status").GetString());
        Assert.True(root.TryGetProperty("totalDuration", out _));
        Assert.Equal(JsonValueKind.Object, entries.ValueKind);
        Assert.Empty(entries.EnumerateObject());
    }

    [Fact]
    public async Task ReadinessHealthEndpoint_ShouldReturnUnhealthy_WhenPostgreSqlIsUnavailable()
    {
        const string password = "readiness-password-that-must-remain-secret";
        var connectionString =
            $"Host=127.0.0.1;Port=1;Database=spendly_tests;Username=spendly;Password={password};Timeout=1;Command Timeout=1";

        using var configuredFactory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        [TestApiConstants.PostgreSqlConnectionStringConfigurationKey] =
                            connectionString
                    });
            });
        });

        using var client = configuredFactory.CreateApiClient();

        using var response = await client.GetAsync(
            TestApiConstants.ReadinessHealthPath,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

        var json = await response.Content.ReadAsStringAsync(
            TestContext.Current.CancellationToken);

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        var entries = root.GetProperty("entries");

        Assert.Equal("Unhealthy", root.GetProperty("status").GetString());
        Assert.Equal(
            "Healthy",
            entries.GetProperty("self").GetProperty("status").GetString());

        var postgreSqlEntry = entries.GetProperty(
            PostgreSqlHealthCheck.RegistrationName);

        Assert.Equal(
            "Unhealthy",
            postgreSqlEntry.GetProperty("status").GetString());
        Assert.Equal(
            "PostgreSQL is unavailable.",
            postgreSqlEntry.GetProperty("description").GetString());
        Assert.False(
            json.Contains(connectionString, StringComparison.Ordinal));
        Assert.False(
            json.Contains(password, StringComparison.Ordinal));
    }

    [Fact]
    public async Task PostgreSqlHealthCheck_ShouldReturnSafeFailureWithoutException()
    {
        var healthCheckService = factory.Services
            .GetRequiredService<HealthCheckService>();

        var report = await healthCheckService.CheckHealthAsync(
            registration =>
                registration.Name == PostgreSqlHealthCheck.RegistrationName,
            TestContext.Current.CancellationToken);

        var entry = Assert.Single(report.Entries);

        Assert.Equal(PostgreSqlHealthCheck.RegistrationName, entry.Key);
        Assert.Equal(HealthStatus.Unhealthy, entry.Value.Status);
        Assert.Equal("PostgreSQL is unavailable.", entry.Value.Description);
        Assert.Null(entry.Value.Exception);
    }

    [Fact]
    public void PostgreSqlHealthCheck_ShouldBeRegisteredForReadinessWithTimeout()
    {
        var options = factory.Services
            .GetRequiredService<IOptions<HealthCheckServiceOptions>>()
            .Value;

        var registration = Assert.Single(
            options.Registrations,
            candidate =>
                candidate.Name == PostgreSqlHealthCheck.RegistrationName);

        Assert.Equal(HealthStatus.Unhealthy, registration.FailureStatus);
        Assert.Contains(PostgreSqlHealthCheck.ReadinessTag, registration.Tags, StringComparer.OrdinalIgnoreCase);
        Assert.Equal(PostgreSqlHealthCheck.Timeout, registration.Timeout);
    }
}
