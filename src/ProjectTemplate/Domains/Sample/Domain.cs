using Microsoft.EntityFrameworkCore;
using ProjectTemplate.Dependencies.Attributes;

namespace ProjectTemplate.Domains.Sample;

/// <summary>
/// Domain container for the <c>Sample</c> bounded context.
/// Defines the business model, persistence model, and EF Core configuration
/// for sample entities. The source generator produces concrete types and
/// registers configurations with <c>AppDbContext</c> at compile time.
/// </summary>
[Domain]
public class SampleDomain
{
    /// <summary>Business model for a sample — properties the domain logic operates on.</summary>
    [BusinessModel]
    private interface ISample
    {
        int Id { get; set; }
        string Name { get; set; }
        string Name2 { get; set; }
    }

    /// <summary>Persistence model for the <c>Samples</c> table.</summary>
    [PersistenceModel]
    private interface ISampleEntity
    {
        int Id { get; set; }
        string Name { get; set; }
        string Name2 { get; set; }
        DateTime CreatedAt { get; set; }
        ISampleEntityDetail Detail { get; set; }
    }

    /// <summary>Persistence model for the <c>SampleDetails</c> table — a one-to-one detail record for each <see cref="ISampleEntity"/>.</summary>
    [PersistenceModel]
    private interface ISampleEntityDetail
    {
        int Id { get; set; }
        int SampleEntityId { get; set; }
        string Detail { get; set; }
    }

    /// <summary>EF Core entity configuration for <see cref="ISampleEntity"/>.</summary>
    [PersistenceModelConfiguration(typeof(ISampleEntity))]
    private static void ConfigureSampleEntity(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<ISampleEntity>();
        entity.ToTable("Samples");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).ValueGeneratedOnAdd();
        entity.Property(e => e.Name).IsRequired().HasMaxLength(256);
        entity.Property(e => e.Name2).IsRequired().HasMaxLength(256);
        entity.Property(e => e.CreatedAt).IsRequired().ValueGeneratedOnAdd();
        entity.HasOne(e => e.Detail)
            .WithOne()
            .HasForeignKey<ISampleEntityDetail>(d => d.SampleEntityId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    /// <summary>EF Core entity configuration for <see cref="ISampleEntityDetail"/>.</summary>
    [PersistenceModelConfiguration(typeof(ISampleEntityDetail))]
    private static void ConfigureSampleEntityDetail(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<ISampleEntityDetail>();
        entity.ToTable("SampleDetails");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).ValueGeneratedOnAdd();
        entity.Property(e => e.SampleEntityId).IsRequired();
        entity.Property(e => e.Detail).IsRequired().HasMaxLength(1024);
    }
}
