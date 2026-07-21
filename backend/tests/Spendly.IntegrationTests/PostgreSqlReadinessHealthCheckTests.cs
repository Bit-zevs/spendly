using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Spendly.Infrastructure.HealthChecks;
using Testcontainers.PostgreSql;

namespace Spendly.IntegrationTests;

public sealed class PostgreSqlReadinessHealthCheckTests(
    SpendlyApiFactory factory)
    : IClassFixture<SpendlyApiFactory>
{
    private const string PostgreSqlImage = "postgres:17.10";

    [Fact(Explicit = true)]
    [Trait("Dependency", "Docker")]
    public async Task ReadinessHealthEndpoint_ShouldReturnHealthyWithoutChangingSchema_WhenPostgreSqlIsAvailable()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        await using var postgreSql = new PostgreSqlBuilder(PostgreSqlImage)
            .WithDatabase("spendly_readiness")
            .WithUsername("spendly")
            .WithPassword("spendly_password")
            .Build();

        await postgreSql.StartAsync(cancellationToken);

        var connectionString = postgreSql.GetConnectionString();
        var tablesBeforeHealthCheck = await GetPublicTableNamesAsync(
            connectionString,
            cancellationToken);

        using var configuredFactory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        [TestApiConstants.PostgreSqlConnectionStringConfigurationKey] =
                            connectionString
                    });
            });
        });

        using var client = configuredFactory.CreateApiClient();

        using var response = await client.GetAsync(
            TestApiConstants.ReadinessHealthPath,
            cancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync(cancellationToken);

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        var entries = root.GetProperty("entries");

        Assert.Equal("Healthy", root.GetProperty("status").GetString());
        Assert.Equal(
            "Healthy",
            entries.GetProperty("self").GetProperty("status").GetString());
        Assert.Equal(
            "Healthy",
            entries
                .GetProperty(PostgreSqlHealthCheck.RegistrationName)
                .GetProperty("status")
                .GetString());

        var tablesAfterHealthCheck = await GetPublicTableNamesAsync(
            connectionString,
            cancellationToken);

        Assert.Empty(tablesBeforeHealthCheck);
        Assert.Empty(tablesAfterHealthCheck);
    }

    private static async Task<IReadOnlyList<string>> GetPublicTableNamesAsync(
        string connectionString,
        CancellationToken cancellationToken)
    {
        await using var dataSource = NpgsqlDataSource.Create(connectionString);
        await using var command = dataSource.CreateCommand(
            """
            SELECT table_name
            FROM information_schema.tables
            WHERE table_schema = 'public'
              AND table_type = 'BASE TABLE'
            ORDER BY table_name;
            """);
        await using var reader = await command.ExecuteReaderAsync(
            cancellationToken);

        var tableNames = new List<string>();

        while (await reader.ReadAsync(cancellationToken))
        {
            tableNames.Add(reader.GetString(0));
        }

        return tableNames;
    }
}
