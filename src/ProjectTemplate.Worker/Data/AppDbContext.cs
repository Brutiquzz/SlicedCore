using Microsoft.EntityFrameworkCore;

namespace ProjectTemplate.Data;

/// <summary>Placeholder DbContext required by the domain source generator in worker projects.</summary>
internal partial class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ApplyGeneratedConfigurations(modelBuilder);
        base.OnModelCreating(modelBuilder);
    }

    /// <summary>Partial method implemented by the source generator to apply discovered entity registrations.</summary>
    partial void ApplyGeneratedConfigurations(ModelBuilder modelBuilder);
}
