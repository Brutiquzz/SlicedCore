using Microsoft.EntityFrameworkCore;
using ProjectTemplate.Dependencies.Attributes;

namespace ProjectTemplate.Domains.Sample;

[Domain]
public class SampleDomain
{
    [BusinessModel]
    private interface ISample
    {
        int Id { get; set; }
        string Name { get; set; }
        string Name2 { get; set; }
    }

    [PersistenceModel]
    private interface ISampleEntity
    {
        int Id { get; set; }
        string Name { get; set; }
        string Name2 { get; set; }
        DateTime CreatedAt { get; set; }
        ISampleEntityDetail Detail { get; set; }
    }

    [PersistenceModel]
    private interface ISampleEntityDetail
    {
        int Id { get; set; }
        int SampleEntityId { get; set; }
        string Detail { get; set; }
    }

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
