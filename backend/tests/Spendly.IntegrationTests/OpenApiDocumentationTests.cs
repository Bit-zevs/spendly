using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Spendly.IntegrationTests;

public sealed class OpenApiDocumentationTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private const string DevelopmentEnvironment = "Development";
    private const string ProductionEnvironment = "Production";

    private const string ExpectedApiTitle = "Spendly API";
    private const string ExpectedApiVersion = "v0.2";

    private const string DocsPath = "/docs";
    private const string DocsPathWithTrailingSlash = "/docs/";

    private const string RootPath = "/";
    private const string LivenessHealthPath = "/health/live";
    private const string ReadinessHealthPath = "/health/ready";
    private const string WeatherForecastPath = "/weatherforecast";

    private const string OpenApiEnabledConfigurationKey = "OpenApi:Enabled";
    private const string OpenApiDocumentPath = "/openapi/" + ExpectedApiVersion + ".json";

    private static readonly Uri TestBaseAddress = new("https://localhost");

    [Fact]
    public async Task OpenApiDocument_ShouldExposeSpendlyApiMetadataInDevelopment()
    {
        var client = factory
            .WithWebHostBuilder(builder => builder.UseEnvironment(DevelopmentEnvironment))
            .CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                BaseAddress = TestBaseAddress
            });

        using var response = await client.GetAsync(
            OpenApiDocumentPath,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

        var json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        var info = root.GetProperty("info");
        Assert.Equal(ExpectedApiTitle, info.GetProperty("title").GetString());
        Assert.Equal(ExpectedApiVersion, info.GetProperty("version").GetString());

        var paths = root.GetProperty("paths");
        var actualPaths = GetOpenApiPaths(paths);

        Assert.True(
            paths.TryGetProperty(RootPath, out _),
            $"OpenAPI document should contain root endpoint '{RootPath}'. Actual paths: {actualPaths}");

        Assert.True(
            paths.TryGetProperty(LivenessHealthPath, out _),
            $"OpenAPI document should contain liveness endpoint '{LivenessHealthPath}'. Actual paths: {actualPaths}");

        Assert.True(
            paths.TryGetProperty(ReadinessHealthPath, out _),
            $"OpenAPI document should contain readiness endpoint '{ReadinessHealthPath}'. Actual paths: {actualPaths}");

        Assert.False(paths.TryGetProperty(WeatherForecastPath, out _));
        Assert.DoesNotContain("weatherforecast", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ScalarDocumentationPage_ShouldBeAvailableInDevelopment()
    {
        var client = factory
            .WithWebHostBuilder(builder => builder.UseEnvironment(DevelopmentEnvironment))
            .CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = true,
                BaseAddress = TestBaseAddress
            });

        using var response = await client.GetAsync(
            DocsPath,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);

        var html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Contains(ExpectedApiTitle, html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ScalarDocumentationPage_ShouldRedirectToTrailingSlashInDevelopment()
    {
        var client = factory
            .WithWebHostBuilder(builder => builder.UseEnvironment(DevelopmentEnvironment))
            .CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                BaseAddress = TestBaseAddress
            });

        using var response = await client.GetAsync(
            DocsPath,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Found, response.StatusCode);

        var location = response.Headers.Location?.OriginalString;

        Assert.False(string.IsNullOrWhiteSpace(location));
        Assert.True(
            location.EndsWith("docs/", StringComparison.OrdinalIgnoreCase),
            $"Expected redirect to 'docs/' or '{DocsPathWithTrailingSlash}', but actual Location was '{location}'.");
    }

    [Theory]
    [InlineData(DocsPath)]
    [InlineData(DocsPathWithTrailingSlash)]
    [InlineData(OpenApiDocumentPath)]
    public async Task OpenApiAndScalar_ShouldNotBeAvailableOutsideDevelopment(string path)
    {
        var client = factory
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment(ProductionEnvironment);

                builder.ConfigureAppConfiguration((_, configuration) =>
                {
                    configuration.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        [OpenApiEnabledConfigurationKey] = "true"
                    });
                });
            })
            .CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                BaseAddress = TestBaseAddress
            });

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
