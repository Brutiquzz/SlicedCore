using Microsoft.EntityFrameworkCore;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("ProjectTemplate.Tests")]

namespace ProjectTemplate.Data;

/// <summary>
/// The application's primary EF Core database context.
/// Entity type configurations are applied via the source-generated
/// <c>ApplyGeneratedConfigurations</c> partial method, which is populated by
/// the <c>ProjectTemplate.Generators</c> source generator at compile time.
/// </summary>
/// <remarks>
/// This type is <c>internal</c> and only accessible from the infrastructure layer
/// via <see cref="ProjectTemplate.Dependencies.InfrastructureDbContext{TContext}"/>
/// and the <c>GetRequiredDbContext&lt;AppDbContext&gt;()</c> helper.
/// </remarks>
internal partial class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ApplyGeneratedConfigurations(modelBuilder);
        base.OnModelCreating(modelBuilder);
    }

    /// <summary>
    /// Partial method implemented by the source generator to apply all
    /// <see cref="ProjectTemplate.Dependencies.Attributes.PersistenceModelConfigurationAttribute"/>-annotated
    /// configurations discovered at compile time.
    /// </summary>
    partial void ApplyGeneratedConfigurations(ModelBuilder modelBuilder);
}
