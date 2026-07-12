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
                    Type = ProblemDetailsDefaults.GetTypeUri(),
                    Title = "Validation Error",
                    Status = StatusCodes.Status400BadRequest,
                    Detail = "One or more validation errors occurred."
                };

                problemDetails.Extensions[ProblemDetailsDefaults.CodeExtensionName] = "validation_error";
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

            if (statusCode < StatusCodes.Status400BadRequest)
            {
                return;
            }

            var problemDetailsService =
                httpContext.RequestServices.GetRequiredService<IProblemDetailsService>();

            var problemDetails = new ProblemDetails
            {
                Type = ProblemDetailsDefaults.GetTypeUri(),
                Title = ProblemDetailsDefaults.GetTitle(statusCode),
                Status = statusCode,
                Detail = ProblemDetailsDefaults.GetDetail(statusCode)
            };

            problemDetails.ApplySpendlyDefaults(httpContext);

            await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = httpContext,
                ProblemDetails = problemDetails
            });
        });

        return app;
    }

    internal static void ApplySpendlyDefaults(
        this ProblemDetails problemDetails,
        HttpContext httpContext)
    {
        var statusCode = problemDetails.Status ?? httpContext.Response.StatusCode;

        if (statusCode < StatusCodes.Status400BadRequest)
        {
            statusCode = StatusCodes.Status500InternalServerError;
        }

        httpContext.Response.StatusCode = statusCode;

        problemDetails.Status ??= statusCode;
        problemDetails.Type ??= ProblemDetailsDefaults.GetTypeUri();
        problemDetails.Title ??= ProblemDetailsDefaults.GetTitle(statusCode);
        problemDetails.Detail ??= ProblemDetailsDefaults.GetDetail(statusCode);
        problemDetails.Instance ??= httpContext.Request.Path.Value;

        if (!problemDetails.Extensions.ContainsKey(ProblemDetailsDefaults.CodeExtensionName))
        {
            problemDetails.Extensions[ProblemDetailsDefaults.CodeExtensionName] =
                ProblemDetailsDefaults.GetCode(statusCode);
        }

        if (!problemDetails.Extensions.ContainsKey(ProblemDetailsDefaults.TraceIdExtensionName))
        {
            problemDetails.Extensions[ProblemDetailsDefaults.TraceIdExtensionName] =
                Activity.Current?.Id ?? httpContext.TraceIdentifier;
        }
    }
}
