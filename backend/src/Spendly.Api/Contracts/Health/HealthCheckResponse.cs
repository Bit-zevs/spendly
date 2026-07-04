namespace Spendly.Api.Contracts.Health;

public sealed record HealthCheckResponse(
    string Status,
    TimeSpan TotalDuration,
    IReadOnlyDictionary<string, HealthCheckEntryResponse> Entries);

public sealed record HealthCheckEntryResponse(
    string Status,
    string? Description,
    TimeSpan Duration);
