using Serilog;
using Spendly.Api.Extensions;
using Spendly.Api.Logging;
using Spendly.Infrastructure;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

try
{
    Log.Information("Starting Spendly.Api");

    var builder = WebApplication.CreateBuilder(args);

    builder.AddApiLogging();

    builder.Services.AddApiServices(builder.Configuration);
    builder.Services.AddInfrastructure(builder.Configuration);

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
        var exceptionType = currentException.GetType();

        if (exceptionType.Name == "HostAbortedException"
            && exceptionType.Namespace == "Microsoft.Extensions.Hosting")
        {
            return true;
        }

        if (exceptionType.Name == "StopTheHostException"
            && exceptionType.Namespace?.StartsWith(
                "Microsoft.Extensions.Hosting",
                StringComparison.Ordinal) is true)
        {
            return true;
        }
    }

    return false;
}

public partial class Program;
