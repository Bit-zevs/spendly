using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Spendly.Api.Errors;

public static class ProblemDetailsExtensions
{
    public static IServiceCollection AddApiProblemDetails(this IServiceCollection services)
    {
        services.AddExceptionHandler<GlobalExceptionHandler>();

        services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = context =>
            {
                context.ProblemDetails.ApplySpendlyDefaults(context.HttpContext);
            };
        });

        services.Configure<ApiBehaviorOptions>(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                var problemDetails = new ValidationProblemDetails(context.ModelState)
                {
                    Type = ProblemDetailsDefaults.GetType(StatusCodes.Status400BadRequest),
                    Title = "Validation Error",
                    Status = StatusCodes.Status400BadRequest,
                    Detail = "One or more validation errors occurred."
                };

                problemDetails.ApplySpendlyDefaults(context.HttpContext);

                return new BadRequestObjectResult(problemDetails)
                {
                    ContentTypes = { "application/problem+json" }
                };
            };
        });

        return services;
    }

    public static WebApplication UseApiProblemDetails(this WebApplication app)
    {
        app.UseExceptionHandler();

        app.UseStatusCodePages(async statusCodeContext =>
        {
            var httpContext = statusCodeContext.HttpContext;

            if (httpContext.Response.HasStarted)
            {
                return;
            }

            var statusCode = httpContext.Response.StatusCode;

            var problemDetailsService =
                httpContext.RequestServices.GetRequiredService<IProblemDetailsService>();

            var problemDetails = new ProblemDetails
            {
                Type = ProblemDetailsDefaults.GetType(statusCode),
                Title = ProblemDetailsDefaults.GetTitle(statusCode),
                Status = statusCode,
                Detail = ProblemDetailsDefaults.GetDetail(statusCode)
            };

            await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = httpContext,
                ProblemDetails = problemDetails
            });
        });

        return app;
    }

    private static void ApplySpendlyDefaults(
        this ProblemDetails problemDetails,
        HttpContext httpContext)
    {
        var statusCode = problemDetails.Status ?? httpContext.Response.StatusCode;

        if (statusCode < StatusCodes.Status400BadRequest)
        {
            statusCode = StatusCodes.Status500InternalServerError;
        }

        problemDetails.Status ??= statusCode;
        problemDetails.Type ??= ProblemDetailsDefaults.GetType(statusCode);
        problemDetails.Title ??= ProblemDetailsDefaults.GetTitle(statusCode);
        problemDetails.Detail ??= ProblemDetailsDefaults.GetDetail(statusCode);

        problemDetails.Extensions[ProblemDetailsDefaults.TraceIdExtensionName] =
            Activity.Current?.Id ?? httpContext.TraceIdentifier;
    }
}
