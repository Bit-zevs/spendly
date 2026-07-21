using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Spendly.IntegrationTests;

public sealed class ConfigurationValidationTests(SpendlyApiFactory factory)
    : IClassFixture<SpendlyApiFactory>
{
    [Theory]
    [InlineData("Application:Name", "", "Application:Name is required.")]
    [InlineData("Application:DisplayName", "", "Application:DisplayName is required.")]
    [InlineData("Application:Version", "", "Application:Version is required.")]
    [InlineData(
        "Application:Version",
        "v0/2",
        "Application:Version may contain only Latin letters, digits, '.', '_' and '-'; it must start with a letter or digit.")]
    [InlineData("HealthChecks:LivePath", "", "HealthChecks:LivePath is required.")]
    [InlineData(
        "HealthChecks:LivePath",
        "health/live",
        "HealthChecks:LivePath must be an absolute application path without a query string or fragment.")]
    [InlineData(
        "HealthChecks:LivePath",
        "/",
        "HealthChecks:LivePath must not use the root endpoint '/'.")]
    [InlineData("HealthChecks:ReadyPath", "", "HealthChecks:ReadyPath is required.")]
    [InlineData(
        "HealthChecks:ReadyPath",
        "health/ready",
        "HealthChecks:ReadyPath must be an absolute application path without a query string or fragment.")]
    [InlineData(
        "HealthChecks:ReadyPath",
        "/",
        "HealthChecks:ReadyPath must not use the root endpoint '/'.")]
    [InlineData(
        "HealthChecks:LivePath",
        TestApiConstants.ReadinessHealthPath,
        "HealthChecks:LivePath and HealthChecks:ReadyPath must be different.")]
    [InlineData("OpenApi:Endpoint", "", "OpenApi:Endpoint is required.")]
    [InlineData(
        "OpenApi:Endpoint",
        "openapi/{documentName}.json",
        "OpenApi:Endpoint must be an absolute application path without a query string or fragment.")]
    [InlineData(
        "OpenApi:Endpoint",
        "/openapi/document.json",
        "OpenApi:Endpoint must contain exactly one '{documentName}' placeholder.")]
    [InlineData(
        "OpenApi:Endpoint",
        "/openapi/{documentName}/{documentName}.json",
        "OpenApi:Endpoint must contain exactly one '{documentName}' placeholder.")]
    [InlineData("OpenApi:ScalarEndpoint", "", "OpenApi:ScalarEndpoint is required.")]
    [InlineData(
        "OpenApi:ScalarEndpoint",
        "docs",
        "OpenApi:ScalarEndpoint must be an absolute application path without a query string or fragment.")]
    [InlineData(
        "OpenApi:ScalarEndpoint",
        "/",
        "OpenApi:ScalarEndpoint must not use the root endpoint '/'.")]
    [InlineData(
        "OpenApi:ScalarEndpoint",
        "/openapi/{documentName}.json",
        "OpenApi:Endpoint and OpenApi:ScalarEndpoint must be different.")]
    [InlineData(
        "OpenApi:ScalarEndpoint",
        TestApiConstants.LivenessHealthPath,
        "Root, health-check, OpenAPI and Scalar endpoint paths must be unique.")]
    [InlineData(
        TestApiConstants.PostgreSqlConnectionStringConfigurationKey,
        "",
        "Connection string 'SpendlyDatabase' is required.")]
    [InlineData(
        TestApiConstants.PostgreSqlConnectionStringConfigurationKey,
        "Host=localhost;Database",
        "Connection string 'SpendlyDatabase' is not a valid PostgreSQL connection string.")]
    [InlineData(
        TestApiConstants.PostgreSqlConnectionStringConfigurationKey,
        "Database=spendly;Username=spendly",
        "Connection string 'SpendlyDatabase' must define Host.")]
    [InlineData(
        TestApiConstants.PostgreSqlConnectionStringConfigurationKey,
        "Host=localhost;Username=spendly",
        "Connection string 'SpendlyDatabase' must define Database.")]
    [InlineData(
        TestApiConstants.PostgreSqlConnectionStringConfigurationKey,
        "Host=localhost;Database=spendly",
        "Connection string 'SpendlyDatabase' must define Username.")]
    public async Task ApiHost_ShouldFailFast_WhenConfigurationIsInvalid(
        string configurationKey,
        string configurationValue,
        string expectedFailure)
    {
        var optionsValidationException =
            await StartApiAndGetOptionsValidationExceptionAsync(
                new Dictionary<string, string?>
                {
                    [configurationKey] = configurationValue
                });

        Assert.Contains(expectedFailure, optionsValidationException.Failures);
    }

    [Fact]
    public async Task ApiHost_ShouldNotExposeDatabasePassword_WhenValidationFails()
    {
        const string password = "database-password-that-must-remain-secret";

        var optionsValidationException =
            await StartApiAndGetOptionsValidationExceptionAsync(
                new Dictionary<string, string?>
                {
                    [TestApiConstants.PostgreSqlConnectionStringConfigurationKey] =
                        $"Host=;Database=spendly;Username=spendly;Password={password}"
                });

        Assert.False(
            optionsValidationException.ToString().Contains(
                password,
                StringComparison.Ordinal),
            "Database configuration validation must not expose the password.");
    }

    private async Task<OptionsValidationException>
        StartApiAndGetOptionsValidationExceptionAsync(
            IReadOnlyDictionary<string, string?> configurationValues)
    {
        using var configuredFactory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddInMemoryCollection(configurationValues);
            });
        });

        var exception = await Assert.ThrowsAnyAsync<Exception>(async () =>
        {
            using var client = configuredFactory.CreateApiClient();

            using var _ = await client.GetAsync(
                TestApiConstants.RootPath,
                TestContext.Current.CancellationToken);
        });

        return Assert.IsType<OptionsValidationException>(
            FindException<OptionsValidationException>(exception));
    }

    private static TException? FindException<TException>(Exception exception)
        where TException : Exception
    {
        ArgumentNullException.ThrowIfNull(exception);

        var pendingExceptions = new Stack<Exception>();
        pendingExceptions.Push(exception);

        while (pendingExceptions.TryPop(out var currentException))
        {
            if (currentException is TException expectedException)
            {
                return expectedException;
            }

            if (currentException is AggregateException aggregateException)
            {
                foreach (var innerException in aggregateException.InnerExceptions)
                {
                    pendingExceptions.Push(innerException);
                }

                continue;
            }

            if (currentException.InnerException is not null)
            {
                pendingExceptions.Push(currentException.InnerException);
            }
        }

        return null;
    }
}
