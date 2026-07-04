using Serilog;
using Spendly.Api.Extensions;
using Spendly.Api.Logging;

using var startupLogger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

try
{
    startupLogger.Information("Starting Spendly.Api");

    var builder = WebApplication.CreateBuilder(args);

    builder.AddApiLogging();

    builder.Services.AddApiServices(builder.Configuration);

    var app = builder.Build();

    app.UseApiPipeline();
    app.MapApiEndpoints();

    app.Run();
}
catch (Exception exception) when (!IsTestHostAbortException(exception))
{
    startupLogger.Fatal(exception, "Spendly.Api terminated unexpectedly");

    throw;
}
finally
{
    startupLogger.Information("Stopped Spendly.Api");
}

static bool IsTestHostAbortException(Exception exception)
{
    for (var currentException = exception;
         currentException is not null;
         currentException = currentException.InnerException)
    {
        if (currentException.GetType().Name is "HostAbortedException" or "StopTheHostException")
        {
            return true;
        }
    }

    return false;
}

public partial class Program;
