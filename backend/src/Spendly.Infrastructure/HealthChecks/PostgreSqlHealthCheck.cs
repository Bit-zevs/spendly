using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;

namespace Spendly.Infrastructure.HealthChecks;

internal sealed class PostgreSqlHealthCheck(
    NpgsqlDataSource dataSource)
    : IHealthCheck
{
    internal const string RegistrationName = "postgresql";
    internal const string ReadinessTag = "ready";

    internal static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    private const string HealthyDescription = "PostgreSQL is available.";
    private const string UnhealthyDescription = "PostgreSQL is unavailable.";

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        try
        {
            await using var connection =
                await dataSource.OpenConnectionAsync(cancellationToken);

            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1;";

            var result = await command.ExecuteScalarAsync(cancellationToken);

            return result is int value && value == 1
                ? HealthCheckResult.Healthy(HealthyDescription)
                : HealthCheckResult.Unhealthy(UnhealthyDescription);
        }
        catch (Exception exception) when (
            exception is NpgsqlException
                or TimeoutException
                or OperationCanceledException)
        {
            return HealthCheckResult.Unhealthy(UnhealthyDescription);
        }
    }
}
