namespace Spendly.Api.Configuration;

public static class ConfigurationOptionsExtensions
{
    private const string RootEndpointPath = "/";
    private const string OpenApiDocumentNamePlaceholder = "{documentName}";

    public static IServiceCollection AddApiConfigurationOptions(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<ApplicationOptions>()
            .Bind(configuration.GetRequiredSection(ApplicationOptions.SectionName))
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.Name),
                "Application:Name is required.")
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.DisplayName),
                "Application:DisplayName is required.")
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.Version),
                "Application:Version is required.")
            .Validate(
                options => IsValidDocumentName(options.Version),
                "Application:Version may contain only Latin letters, digits, '.', '_' and '-'; it must start with a letter or digit.")
            .ValidateOnStart();

        services.AddOptions<HealthChecksOptions>()
            .Bind(configuration.GetRequiredSection(HealthChecksOptions.SectionName))
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.LivePath),
                "HealthChecks:LivePath is required.")
            .Validate(
                options => IsValidEndpointPath(options.LivePath),
                "HealthChecks:LivePath must be an absolute application path without a query string or fragment.")
            .Validate(
                options => !IsRootEndpoint(options.LivePath),
                "HealthChecks:LivePath must not use the root endpoint '/'.")
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.ReadyPath),
                "HealthChecks:ReadyPath is required.")
            .Validate(
                options => IsValidEndpointPath(options.ReadyPath),
                "HealthChecks:ReadyPath must be an absolute application path without a query string or fragment.")
            .Validate(
                options => !IsRootEndpoint(options.ReadyPath),
                "HealthChecks:ReadyPath must not use the root endpoint '/'.")
            .Validate(
                options => !PathsEqual(options.LivePath, options.ReadyPath),
                "HealthChecks:LivePath and HealthChecks:ReadyPath must be different.")
            .ValidateOnStart();

        services.AddOptions<OpenApiOptions>()
            .Bind(configuration.GetRequiredSection(OpenApiOptions.SectionName))
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.Endpoint),
                "OpenApi:Endpoint is required.")
            .Validate(
                options => IsValidEndpointPath(options.Endpoint),
                "OpenApi:Endpoint must be an absolute application path without a query string or fragment.")
            .Validate(
                options => CountOccurrences(
                    options.Endpoint,
                    OpenApiDocumentNamePlaceholder) == 1,
                "OpenApi:Endpoint must contain exactly one '{documentName}' placeholder.")
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.ScalarEndpoint),
                "OpenApi:ScalarEndpoint is required.")
            .Validate(
                options => IsValidEndpointPath(options.ScalarEndpoint),
                "OpenApi:ScalarEndpoint must be an absolute application path without a query string or fragment.")
            .Validate(
                options => !IsRootEndpoint(options.ScalarEndpoint),
                "OpenApi:ScalarEndpoint must not use the root endpoint '/'.")
            .Validate(
                options => !PathsEqual(options.Endpoint, options.ScalarEndpoint),
                "OpenApi:Endpoint and OpenApi:ScalarEndpoint must be different.")
            .Validate(
                options => ConfiguredEndpointPathsAreUnique(configuration, options),
                "Root, health-check, OpenAPI and Scalar endpoint paths must be unique.")
            .ValidateOnStart();

        services.AddOptions<InfrastructureOptions>()
            .Bind(configuration.GetRequiredSection(InfrastructureOptions.SectionName))
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.Database.Provider),
                "Infrastructure:Database:Provider is required.")
            .Validate(
                options => IsSupportedDatabaseProvider(options.Database.Provider),
                "Infrastructure:Database:Provider must be 'NotConfigured' or 'PostgreSQL'.")
            .ValidateOnStart();

        return services;
    }

    private static bool IsValidDocumentName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        if (!IsAsciiLetterOrDigit(value[0]))
        {
            return false;
        }

        return value.All(character =>
            IsAsciiLetterOrDigit(character) ||
            character is '.' or '_' or '-');
    }

    private static bool IsAsciiLetterOrDigit(char character)
    {
        return character is >= 'A' and <= 'Z'
               or >= 'a' and <= 'z'
               or >= '0' and <= '9';
    }

    private static bool IsValidEndpointPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || path[0] != '/')
        {
            return false;
        }

        return !path.StartsWith("//", StringComparison.Ordinal)
               && !path.Contains('?')
               && !path.Contains('#')
               && !path.Contains("//", StringComparison.Ordinal)
               && !path.Any(char.IsWhiteSpace);
    }

    private static bool IsRootEndpoint(string? path)
    {
        return PathsEqual(path, RootEndpointPath);
    }

    private static bool PathsEqual(string? left, string? right)
    {
        return string.Equals(
            NormalizePath(left),
            NormalizePath(right),
            StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        var normalizedPath = path.Trim();

        return normalizedPath.Length > 1
            ? normalizedPath.TrimEnd('/')
            : normalizedPath;
    }

    private static int CountOccurrences(
        string? value,
        string searchedValue)
    {
        if (string.IsNullOrEmpty(value))
        {
            return 0;
        }

        var count = 0;
        var searchIndex = 0;

        while (true)
        {
            var foundIndex = value.IndexOf(
                searchedValue,
                searchIndex,
                StringComparison.Ordinal);

            if (foundIndex < 0)
            {
                return count;
            }

            count++;
            searchIndex = foundIndex + searchedValue.Length;
        }
    }

    private static bool ConfiguredEndpointPathsAreUnique(
        IConfiguration configuration,
        OpenApiOptions openApiOptions)
    {
        var applicationVersion = configuration[
            $"{ApplicationOptions.SectionName}:{nameof(ApplicationOptions.Version)}"];

        var livePath = configuration[
            $"{HealthChecksOptions.SectionName}:{nameof(HealthChecksOptions.LivePath)}"];

        var readyPath = configuration[
            $"{HealthChecksOptions.SectionName}:{nameof(HealthChecksOptions.ReadyPath)}"];

        if (!CanValidateEndpointPathUniqueness(
                applicationVersion,
                livePath,
                readyPath,
                openApiOptions))
        {
            return true;
        }

        var openApiPath = openApiOptions.Endpoint.Replace(
            OpenApiDocumentNamePlaceholder,
            applicationVersion!,
            StringComparison.Ordinal);

        var paths = new[]
        {
            RootEndpointPath,
            livePath!,
            readyPath!,
            openApiPath,
            openApiOptions.ScalarEndpoint
        }
        .Select(NormalizePath)
        .ToArray();

        return paths.Length == paths
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();
    }

    private static bool CanValidateEndpointPathUniqueness(
        string? applicationVersion,
        string? livePath,
        string? readyPath,
        OpenApiOptions openApiOptions)
    {
        return IsValidDocumentName(applicationVersion)
               && IsValidEndpointPath(livePath)
               && !IsRootEndpoint(livePath)
               && IsValidEndpointPath(readyPath)
               && !IsRootEndpoint(readyPath)
               && !PathsEqual(livePath, readyPath)
               && IsValidEndpointPath(openApiOptions.Endpoint)
               && CountOccurrences(
                   openApiOptions.Endpoint,
                   OpenApiDocumentNamePlaceholder) == 1
               && IsValidEndpointPath(openApiOptions.ScalarEndpoint)
               && !IsRootEndpoint(openApiOptions.ScalarEndpoint)
               && !PathsEqual(
                   openApiOptions.Endpoint,
                   openApiOptions.ScalarEndpoint);
    }

    private static bool IsSupportedDatabaseProvider(string? provider)
    {
        return string.Equals(
                   provider,
                   InfrastructureOptions.DatabaseOptions.NotConfiguredProvider,
                   StringComparison.OrdinalIgnoreCase)
               || string.Equals(
                   provider,
                   InfrastructureOptions.DatabaseOptions.PostgreSqlProvider,
                   StringComparison.OrdinalIgnoreCase);
    }
}
