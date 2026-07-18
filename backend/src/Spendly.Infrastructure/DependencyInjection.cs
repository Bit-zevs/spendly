using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
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

        services.AddDbContext<SpendlyDbContext>((serviceProvider, options) =>
        {
            var postgreSqlOptions = serviceProvider
                .GetRequiredService<IOptions<PostgreSqlOptions>>()
                .Value;

            options.UseNpgsql(
                postgreSqlOptions.ConnectionString,
                npgsqlOptions =>
                {
                    npgsqlOptions.MigrationsAssembly(
                        typeof(SpendlyDbContext).Assembly.GetName().Name!);
                });
        });

        return services;
    }
}
