namespace Spendly.Api.Configuration;

public sealed class PostgreSqlOptions
{
    public const string ConnectionStringName = "SpendlyDatabase";

    public const string ConfigurationKey =
        "ConnectionStrings:" + ConnectionStringName;

    public string ConnectionString { get; set; } = string.Empty;
}
