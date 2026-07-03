namespace Spendly.Api.Configuration;

public static class ConfigurationOptionsExtensions
{
    public static IServiceCollection AddApiConfigurationOptions(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<ApplicationOptions>()
            .Bind(configuration.GetRequiredSection(ApplicationOptions.SectionName))
            .Validate(options => !string.IsNullOrWhiteSpace(options.Name), "Application:Name is required.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.DisplayName), "Application:DisplayName is required.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.Version), "Application:Version is required.")
            .ValidateOnStart();

        services.AddOptions<HealthChecksOptions>()
            .Bind(configuration.GetRequiredSection(HealthChecksOptions.SectionName))
            .Validate(options => !string.IsNullOrWhiteSpace(options.LivePath), "HealthChecks:LivePath is required.")
            .Validate(options => options.LivePath.StartsWith('/'), "HealthChecks:LivePath must start with '/'.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.ReadyPath), "HealthChecks:ReadyPath is required.")
            .Validate(options => options.ReadyPath.StartsWith('/'), "HealthChecks:ReadyPath must start with '/'.")
            .Validate(
                options => !string.Equals(options.LivePath, options.ReadyPath, StringComparison.OrdinalIgnoreCase),
                "HealthChecks:LivePath and HealthChecks:ReadyPath must be different.")
            .ValidateOnStart();

        services.AddOptions<OpenApiOptions>()
            .Bind(configuration.GetRequiredSection(OpenApiOptions.SectionName))
            .Validate(options => !string.IsNullOrWhiteSpace(options.DocumentName), "OpenApi:DocumentName is required.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.Endpoint), "OpenApi:Endpoint is required.")
            .Validate(options => options.Endpoint.StartsWith("/", StringComparison.Ordinal), "OpenApi:Endpoint must start with '/'.")
            .ValidateOnStart();

        services.AddOptions<InfrastructureOptions>()
            .Bind(configuration.GetRequiredSection(InfrastructureOptions.SectionName))
            .ValidateOnStart();

        return services;
    }
}
