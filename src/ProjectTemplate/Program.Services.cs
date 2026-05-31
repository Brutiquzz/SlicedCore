using Asp.Versioning;
using Cortex.Mediator.DependencyInjection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Compliance.Redaction;
using ProjectTemplate.Dependencies;
using ProjectTemplate.Dependencies.OpenApi;
using ProjectTemplate.Framework;
using System.Reflection;

namespace ProjectTemplate;

public partial class Program
{
    /// <summary>
    /// Registers all application services with the DI container.
    /// Called once at startup before the application pipeline is built.
    /// </summary>
    private static void ConfigureServices(WebApplicationBuilder builder)
    {
        // API Versioning
        builder.Services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
            options.ApiVersionReader = ApiVersionReader.Combine(
                new UrlSegmentApiVersionReader(),
                new HeaderApiVersionReader("X-Api-Version"),
                new QueryStringApiVersionReader("api-version")
            );
        });

        // OpenAPI
        builder.Services.AddOpenApi("v1", options => options.AddDocumentTransformer<ApiVersionDocumentTransformer>());
        builder.Services.AddOpenApi("v2", options => options.AddDocumentTransformer<ApiVersionDocumentTransformer>());

        // Authentication: JWT Bearer tokens validated against an external identity provider.
        // Configure Authority and Audience in appsettings.json or via environment variables/secret manager.
        builder.Services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                var jwtSection = builder.Configuration.GetSection("JwtBearer");
                options.Authority = jwtSection["Authority"];
                options.Audience = jwtSection["Audience"];
                options.RequireHttpsMetadata = jwtSection.GetValue<bool>("RequireHttpsMetadata", true);
            });

        // Authorization: single coarse-grained policy requiring any authenticated JWT.
        // Endpoints opt in via .RequireAuthorization(AuthorizationPolicies.AuthenticatedUser).
        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy(
                AuthorizationPolicies.AuthenticatedUser,
                policy => policy.RequireAuthenticatedUser());
        });

        // Redaction: in Development PII is logged in plain text for easy debugging.
        // In all other environments PII fields are HMAC-hashed before reaching any log sink.
        // Rotate production keys via your secret manager; never commit real keys to source control.
        builder.Services.AddRedaction(redaction =>
        {
            redaction.SetRedactor<PassThroughRedactor>(DataClassifications.Public);

            if (builder.Environment.IsDevelopment())
            {
                redaction.SetRedactor<PassThroughRedactor>(DataClassifications.Pii);
            }
            else
            {
                redaction.SetHmacRedactor(
                    builder.Configuration.GetSection("HmacRedactorOptions"),
                    DataClassifications.Pii);
            }
        });
        builder.Logging.EnableRedaction();


        // Cortex Mediator
        builder.Services.AddCortexMediator(
            new[] { typeof(Program) },
            options => options.AddDefaultBehaviors()
        );

        builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        builder.Services.AddLayerValidators(Assembly.GetExecutingAssembly());

        // Layer handlers and dependencies
        builder.Services.AddLayerHandlers(Assembly.GetExecutingAssembly());

        AddCoreLayerDependencies(new CoreLayerBuilder(builder));
        AddInfrastructureLayerDependencies(new InfrastructureLayerBuilder(builder));
        AddApplicationLayerDependencies(new ApplicationLayerBuilder(builder));
        AddPresentationLayerDependencies(new PresentationLayerBuilder(builder));

        builder.Services.AddProblemDetails();

        // CORS: origins, methods, and headers are configured in appsettings.json under "Cors".
        // In development all origins are allowed for convenience.
        // In production restrict AllowedOrigins to known client URLs.
        var corsSection = builder.Configuration.GetSection("Cors");
        builder.Services.AddCors(options =>
        {
            options.AddPolicy(CorsPolicies.Default, policy =>
            {
                var origins = corsSection.GetSection("AllowedOrigins").Get<string[]>() ?? [];
                var methods = corsSection.GetSection("AllowedMethods").Get<string[]>() ?? [];
                var headers = corsSection.GetSection("AllowedHeaders").Get<string[]>() ?? [];
                var allowCredentials = corsSection.GetValue<bool>("AllowCredentials");

                if (builder.Environment.IsDevelopment())
                    policy.AllowAnyOrigin();
                else
                    policy.WithOrigins(origins);

                policy.WithMethods(methods)
                      .WithHeaders(headers);

                if (allowCredentials && !builder.Environment.IsDevelopment())
                    policy.AllowCredentials();
            });
        });
    }
}
