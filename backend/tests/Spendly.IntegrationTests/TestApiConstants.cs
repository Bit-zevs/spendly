namespace Spendly.IntegrationTests;

internal static class TestApiConstants
{
    public const string DevelopmentEnvironment = "Development";
    public const string ProductionEnvironment = "Production";

    public const string ApiTitle = "Spendly API";
    public const string ApiVersion = "v0.2";

    public const string RootPath = "/";
    public const string DocsPath = "/docs";
    public const string DocsPathWithTrailingSlash = "/docs/";
    public const string LivenessHealthPath = "/health/live";
    public const string ReadinessHealthPath = "/health/ready";
    public const string UnknownEndpointPath = "/api/unknown-endpoint";
    public const string WeatherForecastPath = "/weatherforecast";
    public const string TestUnhandledExceptionPath = "/tests/unhandled-exception";

    public const string OpenApiEnabledConfigurationKey = "OpenApi:Enabled";
    public const string OpenApiDocumentPath = "/openapi/" + ApiVersion + ".json";
}
