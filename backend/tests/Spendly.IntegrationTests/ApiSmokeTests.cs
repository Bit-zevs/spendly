using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Spendly.IntegrationTests;

public sealed class ApiSmokeTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Api_ShouldStartAndReturnRootStatus()
    {
        using var client = CreateSmokeTestClient();

        using var response = await client.GetAsync(
            TestApiConstants.RootPath,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task LivenessHealthEndpoint_ShouldReturnSuccess()
    {
        using var client = CreateSmokeTestClient();

        using var response = await client.GetAsync(
            TestApiConstants.LivenessHealthPath,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task OpenApiEndpoint_ShouldReturnSuccess_WhenEnabledInDevelopment()
    {
        using var client = CreateSmokeTestClient(
            environmentName: TestApiConstants.DevelopmentEnvironment,
            openApiEnabled: true);

        using var response = await client.GetAsync(
            TestApiConstants.OpenApiDocumentPath,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private HttpClient CreateSmokeTestClient(
        string? environmentName = null,
        bool openApiEnabled = false)
    {
        var configuredFactory = factory.WithWebHostBuilder(builder =>
        {
            if (!string.IsNullOrWhiteSpace(environmentName))
            {
                builder.UseEnvironment(environmentName);
            }

            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    [TestApiConstants.HealthChecksEnabledConfigurationKey] = "true",
                    [TestApiConstants.OpenApiEnabledConfigurationKey] = openApiEnabled ? "true" : "false",
                    [TestApiConstants.PostgreSqlConnectionStringConfigurationKey] =
                        TestApiConstants.UnavailablePostgreSqlConnectionString
                });
            });
        });

        return configuredFactory.CreateApiClient();
    }
}
