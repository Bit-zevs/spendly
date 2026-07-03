using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Spendly.IntegrationTests;

public sealed class HealthCheckEndpointTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Theory]
    [InlineData("/health/live")]
    [InlineData("/health/ready")]
    public async Task HealthEndpoint_ShouldReturnHealthyJsonResponse(string path)
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

        using var response = await client.GetAsync(
            path,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

        var json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        Assert.Equal("Healthy", root.GetProperty("status").GetString());
        Assert.True(root.TryGetProperty("totalDuration", out _));
        Assert.True(root.TryGetProperty("entries", out _));
    }

    [Fact]
    public async Task LivenessHealthEndpoint_ShouldNotRunReadinessChecks()
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

        using var response = await client.GetAsync(
            "/health/live",
            TestContext.Current.CancellationToken);

        var json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        using var document = JsonDocument.Parse(json);
        var entries = document.RootElement.GetProperty("entries");

        Assert.Equal(JsonValueKind.Object, entries.ValueKind);
        Assert.Empty(entries.EnumerateObject());
    }

    [Fact]
    public async Task ReadinessHealthEndpoint_ShouldRunSelfCheck()
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

        using var response = await client.GetAsync(
            "/health/ready",
            TestContext.Current.CancellationToken);

        var json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        using var document = JsonDocument.Parse(json);
        var entries = document.RootElement.GetProperty("entries");

        Assert.True(entries.TryGetProperty("self", out var selfCheck));
        Assert.Equal("Healthy", selfCheck.GetProperty("status").GetString());
    }
}
