namespace Spendly.Api.Configuration;

public sealed class HealthChecksOptions
{
    public const string SectionName = "HealthChecks";

    public bool Enabled { get; init; } = true;

    public string LivePath { get; init; } = "/health/live";

    public string ReadyPath { get; init; } = "/health/ready";
}
