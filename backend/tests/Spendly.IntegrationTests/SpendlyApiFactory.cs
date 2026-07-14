using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Spendly.IntegrationTests;

public sealed class SpendlyApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(TestApiConstants.TestingEnvironment);

        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                [TestApiConstants.PostgreSqlConnectionStringConfigurationKey] =
                    TestApiConstants.ValidPostgreSqlConnectionString
            });
        });
    }
}
