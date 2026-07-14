using Microsoft.Extensions.Options;
using Npgsql;

namespace Spendly.Api.Configuration;

internal sealed class PostgreSqlOptionsValidator
    : IValidateOptions<PostgreSqlOptions>
{
    public ValidateOptionsResult Validate(
        string? name,
        PostgreSqlOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            return ValidateOptionsResult.Fail(
                $"Connection string '{PostgreSqlOptions.ConnectionStringName}' is required.");
        }

        NpgsqlConnectionStringBuilder connectionStringBuilder;

        try
        {
            connectionStringBuilder = new NpgsqlConnectionStringBuilder(
                options.ConnectionString);
        }
        catch (ArgumentException)
        {
            return ValidateOptionsResult.Fail(
                $"Connection string '{PostgreSqlOptions.ConnectionStringName}' is not a valid PostgreSQL connection string.");
        }

        var failures = new List<string>(capacity: 3);

        if (string.IsNullOrWhiteSpace(connectionStringBuilder.Host))
        {
            failures.Add(
                $"Connection string '{PostgreSqlOptions.ConnectionStringName}' must define Host.");
        }

        if (string.IsNullOrWhiteSpace(connectionStringBuilder.Database))
        {
            failures.Add(
                $"Connection string '{PostgreSqlOptions.ConnectionStringName}' must define Database.");
        }

        if (string.IsNullOrWhiteSpace(connectionStringBuilder.Username))
        {
            failures.Add(
                $"Connection string '{PostgreSqlOptions.ConnectionStringName}' must define Username.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}
