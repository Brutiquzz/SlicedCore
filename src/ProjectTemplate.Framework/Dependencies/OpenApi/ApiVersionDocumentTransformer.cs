using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace ProjectTemplate.Dependencies.OpenApi;

/// <summary>
/// Adds JWT Bearer authentication security scheme to the OpenAPI document
/// and replaces the literal {version} route parameter in paths with the
/// actual API version number so Swagger UI generates correct request URLs.
/// </summary>
public sealed class ApiVersionDocumentTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        // Add JWT Bearer security scheme
        document.Components ??= new OpenApiComponents();
        if (!document.Components.SecuritySchemes!.ContainsKey("Bearer"))
        {
            document.Components.SecuritySchemes.Add("Bearer", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "JWT Authorization header using the Bearer scheme. Enter your token in the text input below."
            });
        }

        // The document name is "v1", "v2", etc. — extract the numeric part.
        var versionValue = context.DocumentName.TrimStart('v');

        var pathsToReplace = document.Paths?
            .Where(p => p.Key.Contains("{version}"))
            .ToList();

        if (pathsToReplace is null || pathsToReplace.Count == 0)
            return Task.CompletedTask;

        foreach (var (originalPath, pathItem) in pathsToReplace)
        {
            var resolvedPath = originalPath.Replace("{version}", versionValue);

            // Remove the {version} parameter from every operation on this path
            foreach (var operation in (IEnumerable<OpenApiOperation>?)pathItem.Operations?.Values ?? [])
            {
                var versionParam = operation.Parameters?
                    .FirstOrDefault(p => p.Name == "version");

                if (versionParam is not null && operation.Parameters is not null)
                    operation.Parameters.Remove(versionParam);
            }

            document.Paths!.Remove(originalPath);
            document.Paths!.Add(resolvedPath, pathItem);
        }

        return Task.CompletedTask;
    }
}
