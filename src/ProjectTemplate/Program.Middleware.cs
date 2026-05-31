using ProjectTemplate.Dependencies;
using ProjectTemplate.Framework;
using System.Reflection;

namespace ProjectTemplate;

public partial class Program
{
    /// <summary>
    /// Configures the HTTP middleware pipeline.
    /// Middleware is registered in execution order — order matters.
    /// </summary>
    private static void ConfigureMiddleware(WebApplication app)
    {
        // Global exception handler — catches any unhandled exception that escapes feature logic,
        // logs it via the built-in ILogger, and returns an RFC 9457 problem details response.
        // In Development the full exception detail is included; in other environments it is suppressed.
        app.UseExceptionHandler();
        app.UseStatusCodePages();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();

            // Enable Swagger UI
            // https://localhost:7199/swagger/index.html
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/openapi/v1.json", "ProjectTemplate API V1");
                c.SwaggerEndpoint("/openapi/v2.json", "ProjectTemplate API V2");
            });

            // Enable ReDoc
            // https://localhost:7199/api-docs/index.html
            app.UseReDoc(c =>
            {
                c.DocumentTitle = "ProjectTemplate API Documentation";
                c.SpecUrl = "/openapi/v1.json";
            });

            // Enable Scalar API Reference
            // https://localhost:7199/scalar/v1
            app.MapScalarApiReference("/scalar", options => options
                .WithTitle("ProjectTemplate API"));
        }

        app.UseHttpsRedirection();

        app.UseCors(CorsPolicies.Default);

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapApiEndpoints(Assembly.GetExecutingAssembly());
    }
}
