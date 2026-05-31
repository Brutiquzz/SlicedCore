using Asp.Versioning;
using System.Reflection;

namespace ProjectTemplate.Framework;

/// <summary>
/// Scans an assembly for <see cref="IEndpoint"/> implementations and registers
/// them all under the versioned route group <c>/api/v{version:apiVersion}</c>.
/// </summary>
public static class ApiEndpointScanner
{
    /// <summary>
    /// Discovers all non-abstract classes in <paramref name="assembly"/> whose name ends with
    /// <c>Endpoint</c> and that implement <see cref="IEndpoint"/>, then maps each one under
    /// a shared API-versioned route group.
    /// </summary>
    /// <param name="builder">The endpoint route builder to register endpoints on.</param>
    /// <param name="assembly">The assembly to scan for endpoint implementations.</param>
    /// <returns>The same <paramref name="builder"/> for chaining.</returns>
    public static IEndpointRouteBuilder MapApiEndpoints(this IEndpointRouteBuilder builder, Assembly assembly)
    {
        var types = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.Name.EndsWith("Endpoint") && typeof(IEndpoint).IsAssignableFrom(t));

        // Group all endpoints under /api/v{version:apiVersion}
        var versionSet = builder.NewApiVersionSet()
            .HasApiVersion(new ApiVersion(1, 0))
            .HasApiVersion(new ApiVersion(2, 0))
            .ReportApiVersions()
            .Build();

        var apiGroup = ((WebApplication)builder)
            .MapGroup("/api/v{version:apiVersion}")
            .WithApiVersionSet(versionSet);

        foreach (var type in types)
        {
            var instance = Activator.CreateInstance(type) as IEndpoint;
            instance?.MapEndpoint(apiGroup);
        }

        return builder;
    }
}

/// <summary>
/// Marker interface for minimal-API endpoint classes.
/// Implement this on a nested <c>Endpoint</c> class inside a feature's
/// <c>PresentationLayer</c> to have <see cref="ApiEndpointScanner.MapApiEndpoints"/> discover and register it.
/// </summary>
public interface IEndpoint
{
    /// <summary>Maps this endpoint's route(s) onto the provided <paramref name="builder"/>.</summary>
    void MapEndpoint(IEndpointRouteBuilder builder);
}
