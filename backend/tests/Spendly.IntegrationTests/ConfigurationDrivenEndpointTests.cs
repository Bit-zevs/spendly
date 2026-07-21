using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace Spendly.IntegrationTests;

public sealed class ConfigurationDrivenEndpointTests(SpendlyApiFactory factory)
    : IClassFixture<SpendlyApiFactory>
{
    [Fact]
    public async Task HealthEndpoints_ShouldUseConfiguredPaths()
    {
        const string customLivePath = "/internal/health/live";
        const string customReadyPath = "/internal/health/ready";

        using var client = factory
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_, configuration) =>
                {
                    configuration.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["HealthChecks:LivePath"] = customLivePath,
                        ["HealthChecks:ReadyPath"] = customReadyPath
                    });
                });
            })
            .CreateApiClient();

        using var liveResponse = await client.GetAsync(
            customLivePath,
            TestContext.Current.CancellationToken);

        using var readyResponse = await client.GetAsync(
            customReadyPath,
            TestContext.Current.CancellationToken);

        using var defaultLiveResponse = await client.GetAsync(
            TestApiConstants.LivenessHealthPath,
            TestContext.Current.CancellationToken);

        using var defaultReadyResponse = await client.GetAsync(
            TestApiConstants.ReadinessHealthPath,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, liveResponse.StatusCode);
        Assert.Equal(
            HttpStatusCode.ServiceUnavailable,
            readyResponse.StatusCode);
        Assert.Equal(
            HttpStatusCode.NotFound,
            defaultLiveResponse.StatusCode);
        Assert.Equal(
            HttpStatusCode.NotFound,
            defaultReadyResponse.StatusCode);
    }

    [Fact]
    public async Task OpenApiEndpoint_ShouldUseConfiguredRouteInDevelopment()
    {
        const string customOpenApiEndpoint = "/metadata/{documentName}.json";
        const string customScalarEndpoint = "/reference";

        var customOpenApiDocumentPath =
            $"/metadata/{TestApiConstants.ApiVersion}.json";

        using var client = factory
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment(
                    TestApiConstants.DevelopmentEnvironment);

                builder.ConfigureAppConfiguration((_, configuration) =>
                {
                    configuration.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        [TestApiConstants.OpenApiEnabledConfigurationKey] = "true",
                        ["OpenApi:Endpoint"] = customOpenApiEndpoint,
                        ["OpenApi:ScalarEndpoint"] = customScalarEndpoint
                    });
                });
            })
            .CreateApiClient(allowAutoRedirect: true);

        using var customDocumentResponse = await client.GetAsync(
            customOpenApiDocumentPath,
            TestContext.Current.CancellationToken);

        using var customScalarResponse = await client.GetAsync(
            customScalarEndpoint,
            TestContext.Current.CancellationToken);

        using var defaultDocumentResponse = await client.GetAsync(
            TestApiConstants.OpenApiDocumentPath,
            TestContext.Current.CancellationToken);

        using var defaultDocsResponse = await client.GetAsync(
            TestApiConstants.DocsPathWithTrailingSlash,
            TestContext.Current.CancellationToken);

        Assert.Equal(
            HttpStatusCode.OK,
            customDocumentResponse.StatusCode);
        Assert.Equal(
            "application/json",
            customDocumentResponse.Content.Headers.ContentType?.MediaType);

        Assert.Equal(
            HttpStatusCode.OK,
            customScalarResponse.StatusCode);
        Assert.Equal(
            "text/html",
            customScalarResponse.Content.Headers.ContentType?.MediaType);

        Assert.Equal(
            HttpStatusCode.NotFound,
            defaultDocumentResponse.StatusCode);
        Assert.Equal(
            HttpStatusCode.NotFound,
            defaultDocsResponse.StatusCode);

        var json = await customDocumentResponse.Content.ReadAsStringAsync(
            TestContext.Current.CancellationToken);

        using var document = JsonDocument.Parse(json);
        var info = document.RootElement.GetProperty("info");

        Assert.Equal(
            TestApiConstants.ApiTitle,
            info.GetProperty("title").GetString());
        Assert.Equal(
            TestApiConstants.ApiVersion,
            info.GetProperty("version").GetString());
    }
}
