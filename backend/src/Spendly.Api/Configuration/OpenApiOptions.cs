namespace Spendly.Api.Configuration;

public sealed class OpenApiOptions
{
    public const string SectionName = "OpenApi";

    public bool Enabled { get; init; }

    public string DocumentName { get; init; } = string.Empty;

    public string Endpoint { get; init; } = string.Empty;
}
