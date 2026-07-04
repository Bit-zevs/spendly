using Microsoft.AspNetCore.Mvc.Testing;

namespace Spendly.IntegrationTests;

internal static class IntegrationTestClientFactory
{
    private static readonly Uri TestBaseAddress = new("https://localhost");

    public static HttpClient CreateApiClient(
        this WebApplicationFactory<Program> factory,
        bool allowAutoRedirect = false)
    {
        ArgumentNullException.ThrowIfNull(factory);

        return factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = allowAutoRedirect,
            BaseAddress = TestBaseAddress
        });
    }
}
