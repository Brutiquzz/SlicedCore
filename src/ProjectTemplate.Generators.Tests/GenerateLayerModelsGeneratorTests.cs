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
        await Assert.That(source).Contains("        private interface IOrder");
    }

    [Test]
    public async Task ApplicationModelsContainsInternalConcreteBusinessModel()
    {
        var sources = GeneratorTestHelper.GetSources(
            GeneratorTestHelper.Run<GenerateLayerModelsGenerator>(DomainSource));

        var source = sources["MyApp.Domains.Order.Order.ApplicationLayerModels.g.cs"];
        await Assert.That(source).Contains("        private sealed class Order : IOrder");
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
        await Assert.That(source).Contains("private interface IOrderEntity");
    }

    [Test]
    public async Task InfrastructureModelsContainsInternalConcretePersistenceRecord()
    {
        var sources = GeneratorTestHelper.GetSources(
            GeneratorTestHelper.Run<GenerateLayerModelsGenerator>(DomainSource));

        var source = sources["MyApp.Domains.Order.Order.InfrastructureLayerModels.g.cs"];
        await Assert.That(source).Contains("private sealed record OrderEntity : IOrderEntity");
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

    [Test]
    public async Task BusinessTypesAreNestedInsideApplicationLayer()
    {
        var sources = GeneratorTestHelper.GetSources(
            GeneratorTestHelper.Run<GenerateLayerModelsGenerator>(DomainSource));

        var source = sources["MyApp.Domains.Order.Order.ApplicationLayerModels.g.cs"];

        await Assert.That(source).Contains("partial class ApplicationLayer");
        await Assert.That(source).Contains("        private interface IOrder");
        await Assert.That(source).Contains("        private sealed class Order : IOrder");
    }

    [Test]
    public async Task BusinessTypesAreNotEmittedAtNamespaceLevel()
    {
        var sources = GeneratorTestHelper.GetSources(
            GeneratorTestHelper.Run<GenerateLayerModelsGenerator>(DomainSource));

        var source = sources["MyApp.Domains.Order.Order.ApplicationLayerModels.g.cs"];

        await Assert.That(source).DoesNotContain("\nprivate interface IOrder");
        await Assert.That(source).DoesNotContain("\nprivate sealed class Order");
        await Assert.That(source).DoesNotContain("\ninternal interface IOrder");
        await Assert.That(source).DoesNotContain("\ninternal sealed class Order");
    }

    [Test]
    public async Task EachFeatureGetsItsOwnPrivateBusinessTypes()
    {
        const string multiFeatureSource = """
            using ProjectTemplate.Dependencies.Attributes;

            namespace MyApp.Domains.Order;

            [Domain]
            public class OrderDomain
            {
                [BusinessModel]
                private interface IOrder
                {
                    string Name { get; set; }
                }
            }

            [Feature(FeatureType.Command)]
            public partial class CreateOrder { }

            [Feature(FeatureType.Query)]
            public partial class GetOrder { }
            """;

        var result = GeneratorTestHelper.Run<GenerateLayerModelsGenerator>(multiFeatureSource);
        var sources = GeneratorTestHelper.GetSources(result);
        var appSource = sources["MyApp.Domains.Order.Order.ApplicationLayerModels.g.cs"];

        await Assert.That(appSource).Contains("partial class CreateOrder");
        await Assert.That(appSource).Contains("partial class GetOrder");
        await Assert.That(appSource.Split("private interface IOrder").Length - 1).IsEqualTo(2);
        await Assert.That(appSource.Split("private sealed class Order").Length - 1).IsEqualTo(2);
    }

    [Test]
    public async Task EntityTypesAreNestedInsideInfrastructureLayer()
    {
        var sources = GeneratorTestHelper.GetSources(
            GeneratorTestHelper.Run<GenerateLayerModelsGenerator>(DomainSource));

        var source = sources["MyApp.Domains.Order.Order.InfrastructureLayerModels.g.cs"];

        // Entity types must be declared inside InfrastructureLayer, not at namespace level.
        await Assert.That(source).Contains("partial class InfrastructureLayer");
        await Assert.That(source).Contains("        private interface IOrderEntity");
        await Assert.That(source).Contains("        private sealed record OrderEntity : IOrderEntity");
    }

    [Test]
    public async Task EntityTypesAreNotEmittedAtNamespaceLevel()
    {
        var sources = GeneratorTestHelper.GetSources(
            GeneratorTestHelper.Run<GenerateLayerModelsGenerator>(DomainSource));

        var source = sources["MyApp.Domains.Order.Order.InfrastructureLayerModels.g.cs"];

        // Namespace-level (non-indented) entity declarations must not appear.
        await Assert.That(source).DoesNotContain("\nprivate interface IOrderEntity");
        await Assert.That(source).DoesNotContain("\nprivate sealed record OrderEntity");
        await Assert.That(source).DoesNotContain("\ninternal interface IOrderEntity");
        await Assert.That(source).DoesNotContain("\ninternal sealed record OrderEntity");
    }

    [Test]
    public async Task RegisterEntitiesGeneratedForEveryFeatureInDomain()
    {
        const string multiFeatureSource = """
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

            [Feature(FeatureType.Command)]
            public partial class CreateOrder { }

            [Feature(FeatureType.Query)]
            public partial class GetOrder { }
            """;

        var result = GeneratorTestHelper.Run<GenerateLayerModelsGenerator>(multiFeatureSource);
        var sources = GeneratorTestHelper.GetSources(result);
        var infraSource = sources["MyApp.Domains.Order.Order.InfrastructureLayerModels.g.cs"];

        await Assert.That(infraSource).Contains("partial class CreateOrder");
        await Assert.That(infraSource).Contains("partial class GetOrder");
        await Assert.That(infraSource.Split("internal static void RegisterEntities").Length - 1)
            .IsEqualTo(2);
    }

    [Test]
    public async Task EachFeatureGetsItsOwnPrivateEntityTypes()
    {
        const string multiFeatureSource = """
            using ProjectTemplate.Dependencies.Attributes;

            namespace MyApp.Domains.Order;

            [Domain]
            public class OrderDomain
            {
                [PersistenceModel]
                private interface IOrderEntity
                {
                    int Id { get; set; }
                }
            }

            [Feature(FeatureType.Command)]
            public partial class CreateOrder { }

            [Feature(FeatureType.Query)]
            public partial class GetOrder { }
            """;

        var result = GeneratorTestHelper.Run<GenerateLayerModelsGenerator>(multiFeatureSource);
        var sources = GeneratorTestHelper.GetSources(result);
        var infraSource = sources["MyApp.Domains.Order.Order.InfrastructureLayerModels.g.cs"];

        // Each feature's InfrastructureLayer must declare the entity interface and record.
        await Assert.That(infraSource.Split("private interface IOrderEntity").Length - 1).IsEqualTo(2);
        await Assert.That(infraSource.Split("private sealed record OrderEntity").Length - 1).IsEqualTo(2);
    }
}
