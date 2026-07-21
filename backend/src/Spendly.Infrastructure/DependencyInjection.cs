using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Npgsql;
using Spendly.Infrastructure.HealthChecks;
using Spendly.Infrastructure.Persistence;
using Spendly.Infrastructure.Persistence.Configuration;

namespace Spendly.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services
            .AddOptionsWithValidateOnStart<
                PostgreSqlOptions,
                PostgreSqlOptionsValidator>()
            .Configure(options =>
            {
                options.ConnectionString =
                    configuration.GetConnectionString(
                        PostgreSqlOptions.ConnectionStringName)
                    ?? string.Empty;
            });

        services.AddSingleton<NpgsqlDataSource>(serviceProvider =>
        {
            var postgreSqlOptions = serviceProvider
                .GetRequiredService<IOptions<PostgreSqlOptions>>()
                .Value;

            return NpgsqlDataSource.Create(
                postgreSqlOptions.ConnectionString);
        });

        services.AddDbContext<SpendlyDbContext>((serviceProvider, options) =>
        {
            var dataSource = serviceProvider
                .GetRequiredService<NpgsqlDataSource>();

            options.UseNpgsql(
                dataSource,
                npgsqlOptions =>
                {
                    npgsqlOptions.MigrationsAssembly(
                        typeof(SpendlyDbContext).Assembly.GetName().Name!);
                });
        });

        services.AddHealthChecks()
            .AddCheck<PostgreSqlHealthCheck>(
                name: PostgreSqlHealthCheck.RegistrationName,
                failureStatus: HealthStatus.Unhealthy,
                tags: [PostgreSqlHealthCheck.ReadinessTag],
                timeout: PostgreSqlHealthCheck.Timeout);

        return services;
    }
}
