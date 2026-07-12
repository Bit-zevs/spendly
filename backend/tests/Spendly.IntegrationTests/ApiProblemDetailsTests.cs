using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Spendly.IntegrationTests;

public sealed class ApiProblemDetailsTests(SpendlyApiFactory factory)
    : IClassFixture<SpendlyApiFactory>
{
    [Fact]
    public async Task UnknownEndpoint_ShouldReturnProblemDetails()
    {
        var client = factory.CreateApiClient();

        using var response = await client.GetAsync(
            TestApiConstants.UnknownEndpointPath,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        Assert.Equal("about:blank", root.GetProperty("type").GetString());
        Assert.Equal("Not Found", root.GetProperty("title").GetString());
        Assert.Equal(404, root.GetProperty("status").GetInt32());
        Assert.Equal("The requested resource was not found.", root.GetProperty("detail").GetString());
        Assert.Equal("not_found", root.GetProperty("code").GetString());

        Assert.True(root.TryGetProperty("instance", out var instance));
        Assert.Equal(TestApiConstants.UnknownEndpointPath, instance.GetString());

        Assert.True(root.TryGetProperty("traceId", out var traceId));
        Assert.False(string.IsNullOrWhiteSpace(traceId.GetString()));
    }

    [Fact]
    public async Task UnhandledException_ShouldReturnProblemDetailsWithoutTechnicalDetails()
    {
        var client = factory
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment(TestApiConstants.ProductionEnvironment);

                builder.ConfigureServices(services =>
                {
                    services
                        .AddControllers()
                        .AddApplicationPart(typeof(ThrowUnhandledExceptionController).Assembly);
                });
            })
            .CreateApiClient();

        using var response = await client.GetAsync(
            TestApiConstants.TestUnhandledExceptionPath,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        Assert.Equal("about:blank", root.GetProperty("type").GetString());
        Assert.Equal("Internal Server Error", root.GetProperty("title").GetString());
        Assert.Equal(500, root.GetProperty("status").GetInt32());
        Assert.Equal("An unexpected error occurred.", root.GetProperty("detail").GetString());
        Assert.Equal("internal_server_error", root.GetProperty("code").GetString());

        Assert.True(root.TryGetProperty("instance", out var instance));
        Assert.Equal(TestApiConstants.TestUnhandledExceptionPath, instance.GetString());

        Assert.True(root.TryGetProperty("traceId", out var traceId));
        Assert.False(string.IsNullOrWhiteSpace(traceId.GetString()));

        Assert.DoesNotContain("Spendly integration test exception", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("InvalidOperationException", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("StackTrace", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Microsoft.AspNetCore", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UnsupportedHttpMethod_ShouldReturnProblemDetails()
    {
        var client = factory.CreateApiClient();

        using var response = await client.PostAsync(
            TestApiConstants.RootPath,
            null,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        Assert.Equal("about:blank", root.GetProperty("type").GetString());
        Assert.Equal("Method Not Allowed", root.GetProperty("title").GetString());
        Assert.Equal(405, root.GetProperty("status").GetInt32());
        Assert.Equal("The HTTP method is not supported for this resource.", root.GetProperty("detail").GetString());
        Assert.Equal("method_not_allowed", root.GetProperty("code").GetString());

        Assert.True(root.TryGetProperty("instance", out var instance));
        Assert.Equal(TestApiConstants.RootPath, instance.GetString());

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
