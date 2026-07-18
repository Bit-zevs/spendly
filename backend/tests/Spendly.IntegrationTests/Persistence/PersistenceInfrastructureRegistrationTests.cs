using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Spendly.Infrastructure.Persistence;

namespace Spendly.IntegrationTests.Persistence;

public sealed class PersistenceInfrastructureRegistrationTests(
    SpendlyApiFactory factory)
    : IClassFixture<SpendlyApiFactory>
{
    [Fact]
    public void ApiHost_ShouldRegisterSpendlyDbContextWithNpgsqlProvider()
    {
        using var scope = factory.Services.CreateScope();

        var options = scope.ServiceProvider
            .GetRequiredService<DbContextOptions<SpendlyDbContext>>();

        var providerExtension = Assert.Single(options.Extensions, extension => extension.Info.IsDatabaseProvider);

        Assert.Equal(
            "Npgsql.EntityFrameworkCore.PostgreSQL",
            providerExtension.GetType().Assembly.GetName().Name);
    }

    [Fact]
    public async Task ApiHost_ShouldNotAccessDatabaseDuringStartup()
    {
        using var configuredFactory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        [TestApiConstants.PostgreSqlConnectionStringConfigurationKey] =
                            "Host=database.invalid;Database=spendly_tests;Username=spendly;Password=test;Timeout=1"
                    });
            });
        });

        using var client = configuredFactory.CreateApiClient();

        using var response = await client.GetAsync(
            TestApiConstants.RootPath,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void SpendlyDbContext_ShouldHaveScopedLifetime()
    {
        using var firstScope = factory.Services.CreateScope();
        using var secondScope = factory.Services.CreateScope();

        var firstContext = firstScope.ServiceProvider
            .GetRequiredService<SpendlyDbContext>();

        var sameScopeContext = firstScope.ServiceProvider
            .GetRequiredService<SpendlyDbContext>();

        var secondContext = secondScope.ServiceProvider
            .GetRequiredService<SpendlyDbContext>();

        Assert.Same(firstContext, sameScopeContext);
        Assert.NotSame(firstContext, secondContext);
    }
}
