using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Spendly.IntegrationTests;

public sealed class ApiProblemDetailsTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task UnhandledException_ShouldReturnProblemDetailsWithoutTechnicalDetails()
    {
        var client = factory
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Production");

                builder.ConfigureServices(services =>
                {
                    services
                        .AddControllers()
                        .AddApplicationPart(typeof(ThrowUnhandledExceptionController).Assembly);
                });
            })
            .CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                BaseAddress = new Uri("https://localhost")
            });

        using var response = await client.GetAsync(
            "/tests/unhandled-exception",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        Assert.Equal("https://httpstatuses.com/500", root.GetProperty("type").GetString());
        Assert.Equal("Internal Server Error", root.GetProperty("title").GetString());
        Assert.Equal(500, root.GetProperty("status").GetInt32());
        Assert.Equal("An unexpected error occurred.", root.GetProperty("detail").GetString());
        Assert.Equal("internal_server_error", root.GetProperty("code").GetString());

        Assert.True(root.TryGetProperty("instance", out var instance));
        Assert.Equal("/tests/unhandled-exception", instance.GetString());

        Assert.True(root.TryGetProperty("traceId", out var traceId));
        Assert.False(string.IsNullOrWhiteSpace(traceId.GetString()));

        Assert.DoesNotContain("Spendly integration test exception", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("InvalidOperationException", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("StackTrace", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Microsoft.AspNetCore", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UnknownEndpoint_ShouldReturnProblemDetails()
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

        using var response = await client.GetAsync(
            "/api/unknown-endpoint",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        Assert.Equal("https://httpstatuses.com/404", root.GetProperty("type").GetString());
        Assert.Equal("Not Found", root.GetProperty("title").GetString());
        Assert.Equal(404, root.GetProperty("status").GetInt32());
        Assert.Equal("The requested resource was not found.", root.GetProperty("detail").GetString());
        Assert.Equal("not_found", root.GetProperty("code").GetString());

        Assert.True(root.TryGetProperty("instance", out var instance));
        Assert.Equal("/api/unknown-endpoint", instance.GetString());

        Assert.True(root.TryGetProperty("traceId", out var traceId));
        Assert.False(string.IsNullOrWhiteSpace(traceId.GetString()));
    }
}

[ApiController]
[Route("tests/unhandled-exception")]
public sealed class ThrowUnhandledExceptionController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        throw new InvalidOperationException("Spendly integration test exception");
    }
}
