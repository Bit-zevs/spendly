using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Spendly.IntegrationTests;

public sealed class OpenApiDocumentationTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task OpenApiDocument_ShouldExposeSpendlyApiMetadataInDevelopment()
    {
        var client = factory
            .WithWebHostBuilder(builder => builder.UseEnvironment(TestApiConstants.DevelopmentEnvironment))
            .CreateApiClient();

        using var response = await client.GetAsync(
            TestApiConstants.OpenApiDocumentPath,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

        var json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        var info = root.GetProperty("info");
        Assert.Equal(TestApiConstants.ApiTitle, info.GetProperty("title").GetString());
        Assert.Equal(TestApiConstants.ApiVersion, info.GetProperty("version").GetString());

        var paths = root.GetProperty("paths");
        var actualPaths = GetOpenApiPaths(paths);

        Assert.True(
            paths.TryGetProperty(TestApiConstants.RootPath, out _),
            $"OpenAPI document should contain root endpoint '{TestApiConstants.RootPath}'. Actual paths: {actualPaths}");

        Assert.True(
            paths.TryGetProperty(TestApiConstants.LivenessHealthPath, out _),
            $"OpenAPI document should contain liveness endpoint '{TestApiConstants.LivenessHealthPath}'. Actual paths: {actualPaths}");

        Assert.True(
            paths.TryGetProperty(TestApiConstants.ReadinessHealthPath, out _),
            $"OpenAPI document should contain readiness endpoint '{TestApiConstants.ReadinessHealthPath}'. Actual paths: {actualPaths}");

        Assert.False(paths.TryGetProperty(TestApiConstants.WeatherForecastPath, out _));
        Assert.DoesNotContain("weatherforecast", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ScalarDocumentationPage_ShouldBeAvailableInDevelopment()
    {
        var client = factory
            .WithWebHostBuilder(builder => builder.UseEnvironment(TestApiConstants.DevelopmentEnvironment))
            .CreateApiClient(allowAutoRedirect: true);

        using var response = await client.GetAsync(
            TestApiConstants.DocsPath,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);

        var html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Contains(TestApiConstants.ApiTitle, html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ScalarDocumentationPage_ShouldRedirectToTrailingSlashInDevelopment()
    {
        var client = factory
            .WithWebHostBuilder(builder => builder.UseEnvironment(TestApiConstants.DevelopmentEnvironment))
            .CreateApiClient();

        using var response = await client.GetAsync(
            TestApiConstants.DocsPath,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Found, response.StatusCode);

        var location = response.Headers.Location?.OriginalString;

        Assert.False(string.IsNullOrWhiteSpace(location));
        Assert.True(
            location.EndsWith("docs/", StringComparison.OrdinalIgnoreCase),
            $"Expected redirect to 'docs/' or '{TestApiConstants.DocsPathWithTrailingSlash}', but actual Location was '{location}'.");
    }

    [Theory]
    [InlineData(TestApiConstants.DocsPath)]
    [InlineData(TestApiConstants.DocsPathWithTrailingSlash)]
    [InlineData(TestApiConstants.OpenApiDocumentPath)]
    public async Task OpenApiAndScalar_ShouldNotBeAvailableOutsideDevelopment(string path)
    {
        var client = factory
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment(TestApiConstants.ProductionEnvironment);

                builder.ConfigureAppConfiguration((_, configuration) =>
                {
                    configuration.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        [TestApiConstants.OpenApiEnabledConfigurationKey] = "true"
                    });
                });
            })
            .CreateApiClient();

        using var response = await client.GetAsync(
            path,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static string GetOpenApiPaths(JsonElement paths)
    {
        return string.Join(
            ", ",
            paths.EnumerateObject().Select(path => path.Name));
    }
}
