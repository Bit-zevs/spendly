namespace Spendly.Api.Configuration;

public sealed class HealthChecksOptions
{
    public const string SectionName = "HealthChecks";

    public bool Enabled { get; init; } = true;

    public string Path { get; init; } = "/health";
}
