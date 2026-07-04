using Serilog;
using Serilog.Events;

namespace Spendly.Api.Logging;

public static class ApiLoggingExtensions
{
    public static WebApplicationBuilder AddApiLogging(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddSerilog(
            (services, loggerConfiguration) =>
            {
                loggerConfiguration
                    .ReadFrom.Configuration(builder.Configuration)
                    .ReadFrom.Services(services);
            },
            preserveStaticLogger: true);

        return builder;
    }

    public static WebApplication UseApiRequestLogging(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.UseSerilogRequestLogging(options =>
        {
            options.MessageTemplate =
                "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";

            options.GetLevel = static (httpContext, _, exception) =>
            {
                if (exception is not null ||
                    httpContext.Response.StatusCode >= StatusCodes.Status500InternalServerError)
                {
                    return LogEventLevel.Error;
                }

                if (httpContext.Response.StatusCode >= StatusCodes.Status400BadRequest)
                {
                    return LogEventLevel.Warning;
                }

                return LogEventLevel.Information;
            };

            options.EnrichDiagnosticContext = static (diagnosticContext, httpContext) =>
            {
                diagnosticContext.Set("TraceId", httpContext.TraceIdentifier);
                diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
                diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);

                var endpoint = httpContext.GetEndpoint();

                if (endpoint is not null)
                {
                    diagnosticContext.Set("EndpointName", endpoint.DisplayName);
                }
            };
        });

        return app;
    }
}
