using Serilog;
using Spendly.Api.Extensions;
using Spendly.Api.Logging;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

try
{
    Log.Information("Starting Spendly.Api");

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
    Log.Fatal(exception, "Spendly.Api terminated unexpectedly");

    throw;
}
finally
{
    Log.Information("Stopped Spendly.Api");
    await Log.CloseAndFlushAsync();
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
