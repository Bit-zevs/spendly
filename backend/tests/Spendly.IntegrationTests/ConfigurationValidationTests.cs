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
    [InlineData("HealthChecks:LivePath", "", "HealthChecks:LivePath is required.")]
    [InlineData("HealthChecks:LivePath", "health/live", "HealthChecks:LivePath must start with '/'.")]
    [InlineData("HealthChecks:ReadyPath", "", "HealthChecks:ReadyPath is required.")]
    [InlineData("HealthChecks:ReadyPath", "health/ready", "HealthChecks:ReadyPath must start with '/'.")]
    [InlineData("HealthChecks:LivePath", TestApiConstants.ReadinessHealthPath, "HealthChecks:LivePath and HealthChecks:ReadyPath must be different.")]
    [InlineData("OpenApi:DocumentName", "", "OpenApi:DocumentName is required.")]
    [InlineData("OpenApi:Endpoint", "", "OpenApi:Endpoint is required.")]
    [InlineData("OpenApi:Endpoint", "openapi/{documentName}.json", "OpenApi:Endpoint must start with '/'.")]
    [InlineData("OpenApi:Endpoint", "/openapi/document.json", "OpenApi:Endpoint must contain '{documentName}'.")]
    [InlineData("OpenApi:ScalarEndpoint", "", "OpenApi:ScalarEndpoint is required.")]
    [InlineData("OpenApi:ScalarEndpoint", "docs", "OpenApi:ScalarEndpoint must start with '/'.")]
    [InlineData("OpenApi:ScalarEndpoint", "/openapi/{documentName}.json", "OpenApi:Endpoint and OpenApi:ScalarEndpoint must be different.")]
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
