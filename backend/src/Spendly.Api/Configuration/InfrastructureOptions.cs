namespace Spendly.Api.Configuration;

public sealed class InfrastructureOptions
{
    public const string SectionName = "Infrastructure";

    public DatabaseOptions Database { get; init; } = new();

    public sealed class DatabaseOptions
    {
        public const string NotConfiguredProvider = "NotConfigured";

        public const string PostgreSqlProvider = "PostgreSQL";

        public string Provider { get; init; } = NotConfiguredProvider;
    }
}
