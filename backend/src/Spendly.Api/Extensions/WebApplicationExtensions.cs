using Spendly.Api.Endpoints;
using Spendly.Api.Errors;
using Spendly.Api.Health;
using Spendly.Api.OpenApi;

namespace Spendly.Api.Extensions;

public static class WebApplicationExtensions
{
    extension(WebApplication app)
    {
        public WebApplication UseApiPipeline()
        {
            app.UseApiProblemDetails();

            app.UseHttpsRedirection();
            app.UseAuthorization();

            return app;
        }

        public WebApplication MapApiEndpoints()
        {
            app.MapApiOpenApi();
            app.MapHealthEndpoints();
            app.MapRootEndpoint();
            app.MapControllers();

            return app;
        }
    }
}
