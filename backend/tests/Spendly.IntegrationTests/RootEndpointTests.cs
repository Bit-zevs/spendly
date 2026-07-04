using System.Net;
using System.Text.Json;

namespace Spendly.IntegrationTests;

public sealed class RootEndpointTests(SpendlyApiFactory factory)
    : IClassFixture<SpendlyApiFactory>
{
    [Fact]
    public async Task RootEndpoint_ShouldReturnConfiguredApiStatusContract()
    {
        var client = factory.CreateApiClient();

        using var response = await client.GetAsync(
            TestApiConstants.RootPath,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

        var json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        Assert.Equal(TestApiConstants.ApiTitle, root.GetProperty("application").GetString());
        Assert.Equal(TestApiConstants.RunningStatus, root.GetProperty("status").GetString());
    }
}
