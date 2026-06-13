namespace ProjectTemplate.Generators.Tests;

public class GenerateLayerModelsGeneratorTests
{
    private const string DomainSource = """
        using ProjectTemplate.Dependencies.Attributes;

        namespace MyApp.Domains.Order;

        [Domain]
        public class OrderDomain
        {
            [BusinessModel]
            private interface IOrder
            {
                string Name { get; set; }
                int Quantity { get; set; }
            }

            [PersistenceModel]
            private interface IOrderEntity
            {
                int Id { get; set; }
                string Name { get; set; }
            }
        }
        """;

    [Test]
    public async Task GeneratesTwoSourceFiles_ApplicationAndInfrastructureModels()
    {
        var result = GeneratorTestHelper.Run<GenerateLayerModelsGenerator>(DomainSource);

        await Assert.That(result.GeneratedSources).Count().IsEqualTo(2);
    }

    [Test]
    public async Task ApplicationModelsFileHasCorrectHintName()
    {
        var result = GeneratorTestHelper.Run<GenerateLayerModelsGenerator>(DomainSource);
        var hints = result.GeneratedSources.Select(s => s.HintName).ToList();

        await Assert.That(hints).Contains("MyApp.Domains.Order.Order.ApplicationLayerModels.g.cs");
    }

    [Test]
    public async Task InfrastructureModelsFileHasCorrectHintName()
    {
        var result = GeneratorTestHelper.Run<GenerateLayerModelsGenerator>(DomainSource);
        var hints = result.GeneratedSources.Select(s => s.HintName).ToList();

        await Assert.That(hints).Contains("MyApp.Domains.Order.Order.InfrastructureLayerModels.g.cs");
    }

    [Test]
    public async Task ApplicationModelsContainsInternalBusinessInterface()
    {
        var sources = GeneratorTestHelper.GetSources(
            GeneratorTestHelper.Run<GenerateLayerModelsGenerator>(DomainSource));

        var source = sources["MyApp.Domains.Order.Order.ApplicationLayerModels.g.cs"];
        await Assert.That(source).Contains("internal interface IOrder");
    }

    [Test]
    public async Task ApplicationModelsContainsInternalConcreteBusinessModel()
    {
        var sources = GeneratorTestHelper.GetSources(
            GeneratorTestHelper.Run<GenerateLayerModelsGenerator>(DomainSource));

        var source = sources["MyApp.Domains.Order.Order.ApplicationLayerModels.g.cs"];
        await Assert.That(source).Contains("internal sealed class Order : IOrder");
    }

    [Test]
    public async Task ApplicationModelsContainsBusinessInterfaceProperties()
    {
        var sources = GeneratorTestHelper.GetSources(
            GeneratorTestHelper.Run<GenerateLayerModelsGenerator>(DomainSource));

        var source = sources["MyApp.Domains.Order.Order.ApplicationLayerModels.g.cs"];
        await Assert.That(source).Contains("string Name");
        await Assert.That(source).Contains("int Quantity");
    }

    [Test]
    public async Task InfrastructureModelsContainsInternalPersistenceInterface()
    {
        var sources = GeneratorTestHelper.GetSources(
            GeneratorTestHelper.Run<GenerateLayerModelsGenerator>(DomainSource));

        var source = sources["MyApp.Domains.Order.Order.InfrastructureLayerModels.g.cs"];
        await Assert.That(source).Contains("internal interface IOrderEntity");
    }

    [Test]
    public async Task InfrastructureModelsContainsInternalConcretePersistenceRecord()
    {
        var sources = GeneratorTestHelper.GetSources(
            GeneratorTestHelper.Run<GenerateLayerModelsGenerator>(DomainSource));

        var source = sources["MyApp.Domains.Order.Order.InfrastructureLayerModels.g.cs"];
        await Assert.That(source).Contains("internal sealed record OrderEntity : IOrderEntity");
    }

    [Test]
    public async Task InfrastructureModelsContainsRegisterEntitiesMethod()
    {
        var sources = GeneratorTestHelper.GetSources(
            GeneratorTestHelper.Run<GenerateLayerModelsGenerator>(DomainSource));

        var source = sources["MyApp.Domains.Order.Order.InfrastructureLayerModels.g.cs"];
        await Assert.That(source).Contains("internal static void RegisterEntities(ModelBuilder modelBuilder)");
    }

    [Test]
    public async Task ProducesNoDiagnostics()
    {
        var result = GeneratorTestHelper.Run<GenerateLayerModelsGenerator>(DomainSource);

        await Assert.That(result.Diagnostics).IsEmpty();
    }

    [Test]
    public async Task DomainWithNoPersistenceModelProducesOnlyApplicationModels()
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

        var result = GeneratorTestHelper.Run<GenerateLayerModelsGenerator>(source);

        await Assert.That(result.GeneratedSources).Count().IsEqualTo(1);
        await Assert.That(result.GeneratedSources[0].HintName)
            .Contains("ApplicationLayerModels");
    }
}
