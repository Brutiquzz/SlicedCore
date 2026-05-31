namespace ProjectTemplate.Generators.Tests;

public class GenerateDbContextGeneratorTests
{
    private const string DomainSource = """
        using ProjectTemplate.Dependencies.Attributes;

        namespace MyApp.Domains.Order;

        [Domain]
        public class OrderDomain
        {
            [PersistenceModel]
            private interface IOrderEntity
            {
                int Id { get; set; }
                string Name { get; set; }
            }
        }
        """;

    [Test]
    public async Task GeneratesDbContextPartialFile()
    {
        var result = GeneratorTestHelper.Run<GenerateDbContextGenerator>(DomainSource);

        await Assert.That(result.GeneratedSources).Count().IsGreaterThanOrEqualTo(1);
    }

    [Test]
    public async Task GeneratedFileContainsAppDbContextPartialClass()
    {
        var result = GeneratorTestHelper.Run<GenerateDbContextGenerator>(DomainSource);
        var sources = GeneratorTestHelper.GetSources(result);
        var source = string.Concat(sources.Values);

        await Assert.That(source).Contains("partial class AppDbContext");
    }

    [Test]
    public async Task GeneratedFileContainsApplyGeneratedConfigurationsMethod()
    {
        var result = GeneratorTestHelper.Run<GenerateDbContextGenerator>(DomainSource);
        var sources = GeneratorTestHelper.GetSources(result);
        var source = string.Concat(sources.Values);

        await Assert.That(source).Contains("ApplyGeneratedConfigurations");
    }

    [Test]
    public async Task GeneratedFileCallsRegisterEntitiesForDomain()
    {
        var result = GeneratorTestHelper.Run<GenerateDbContextGenerator>(DomainSource);
        var sources = GeneratorTestHelper.GetSources(result);
        var source = string.Concat(sources.Values);

        await Assert.That(source).Contains("RegisterEntities(modelBuilder)");
    }

    [Test]
    public async Task ProducesNoDiagnostics()
    {
        var result = GeneratorTestHelper.Run<GenerateDbContextGenerator>(DomainSource);

        await Assert.That(result.Diagnostics).IsEmpty();
    }

    [Test]
    public async Task DomainWithNoPersistenceModelProducesNoOutput()
    {
        const string source = """
            using ProjectTemplate.Dependencies.Attributes;

            namespace MyApp.Domains.Product;

            [Domain]
            public class ProductDomain
            {
                [BusinessModel]
                private interface IProduct
                {
                    string Sku { get; set; }
                }
            }
            """;

        var result = GeneratorTestHelper.Run<GenerateDbContextGenerator>(source);

        await Assert.That(result.GeneratedSources).IsEmpty();
    }

    [Test]
    public async Task MultipleDomainsDriveMultipleRegisterEntitiesCalls()
    {
        const string source = """
            using ProjectTemplate.Dependencies.Attributes;

            namespace MyApp.Domains.Order;

            [Domain]
            public class OrderDomain
            {
                [PersistenceModel]
                private interface IOrderEntity { int Id { get; set; } }
            }

            [Domain]
            public class ProductDomain
            {
                [PersistenceModel]
                private interface IProductEntity { int Id { get; set; } }
            }
            """;

        var result = GeneratorTestHelper.Run<GenerateDbContextGenerator>(source);
        var sources = GeneratorTestHelper.GetSources(result);
        var combined = string.Concat(sources.Values);

        // Each domain must contribute its own RegisterEntities call.
        await Assert.That(combined.Split("RegisterEntities").Length - 1).IsGreaterThanOrEqualTo(2);
    }
}
