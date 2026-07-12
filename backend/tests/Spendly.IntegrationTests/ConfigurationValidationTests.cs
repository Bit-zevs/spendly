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
        "Infrastructure:Database:Provider",
        "",
        "Infrastructure:Database:Provider is required.")]
    [InlineData(
        "Infrastructure:Database:Provider",
        "SqlServer",
        "Infrastructure:Database:Provider must be 'NotConfigured' or 'PostgreSQL'.")]
    public async Task ApiHost_ShouldFailFast_WhenConfigurationIsInvalid(
        string configurationKey,
        string configurationValue,
        string expectedFailure)
    {
        var configuredFactory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    [configurationKey] = configurationValue
                });
            });
        });

        var exception = await Assert.ThrowsAnyAsync<Exception>(async () =>
        {
            using var client = configuredFactory.CreateApiClient();

            using var _ = await client.GetAsync(
                TestApiConstants.RootPath,
                TestContext.Current.CancellationToken);
        });

        var optionsValidationException = Assert.IsType<OptionsValidationException>(
            FindException<OptionsValidationException>(exception));

        Assert.Contains(expectedFailure, optionsValidationException.Failures);
    }

    private static TException? FindException<TException>(Exception exception)
        where TException : Exception
    {
        for (var currentException = exception;
             currentException is not null;
             currentException = currentException.InnerException)
        {
            if (currentException is TException expectedException)
            {
                return expectedException;
            }
        }

        return null;
    }
}
