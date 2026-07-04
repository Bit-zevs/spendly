using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace Spendly.IntegrationTests;

public sealed class ApiFeatureToggleTests(SpendlyApiFactory factory)
    : IClassFixture<SpendlyApiFactory>
{
    [Theory]
    [InlineData(TestApiConstants.LivenessHealthPath)]
    [InlineData(TestApiConstants.ReadinessHealthPath)]
    public async Task HealthEndpoints_ShouldReturnNotFound_WhenHealthChecksAreDisabled(string path)
    {
        var client = factory
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_, configuration) =>
                {
                    configuration.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        [TestApiConstants.HealthChecksEnabledConfigurationKey] = "false"
                    });
                });
            })
            .CreateApiClient();

        using var response = await client.GetAsync(
            path,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Theory]
    [InlineData(TestApiConstants.DocsPath)]
    [InlineData(TestApiConstants.DocsPathWithTrailingSlash)]
    [InlineData(TestApiConstants.OpenApiDocumentPath)]
    public async Task OpenApiAndScalar_ShouldReturnNotFound_WhenDisabledInDevelopment(string path)
    {
        var client = factory
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment(TestApiConstants.DevelopmentEnvironment);

                builder.ConfigureAppConfiguration((_, configuration) =>
                {
                    configuration.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        [TestApiConstants.OpenApiEnabledConfigurationKey] = "false"
                    });
                });
            })
            .CreateApiClient();

        using var response = await client.GetAsync(
            path,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
