namespace Spendly.Api.Configuration;

public sealed class InfrastructureOptions
{
    public const string SectionName = "Infrastructure";

    public DatabaseOptions Database { get; init; } = new();

    public sealed class DatabaseOptions
    {
        public string Provider { get; init; } = "NotConfigured";
    }
}
