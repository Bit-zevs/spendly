namespace Spendly.IntegrationTests;

internal static class TestApiConstants
{
    public const string TestingEnvironment = "Testing";
    public const string DevelopmentEnvironment = "Development";
    public const string ProductionEnvironment = "Production";

    public const string ApiTitle = "Spendly API";
    public const string ApiVersion = "v0.2";
    public const string RunningStatus = "Running";

    public const string RootPath = "/";
    public const string DocsPath = "/docs";
    public const string DocsPathWithTrailingSlash = "/docs/";
    public const string LivenessHealthPath = "/health/live";
    public const string ReadinessHealthPath = "/health/ready";
    public const string UnknownEndpointPath = "/api/unknown-endpoint";
    public const string TestUnhandledExceptionPath = "/tests/unhandled-exception";

    public const string HealthChecksEnabledConfigurationKey = "HealthChecks:Enabled";
    public const string OpenApiEnabledConfigurationKey = "OpenApi:Enabled";
    public const string InfrastructureDatabaseProviderConfigurationKey = "Infrastructure:Database:Provider";

    public const string NotConfiguredDatabaseProvider = "NotConfigured";

    public const string OpenApiDocumentPath = "/openapi/" + ApiVersion + ".json";
}
