namespace ProjectTemplate.Generators.Tests;

public class GenerateCoreBoilerplateGeneratorTests
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
            }

            [PersistenceModel]
            private interface IOrderEntity
            {
                int Id { get; set; }
                string Name { get; set; }
            }
        }

        [Feature(FeatureType.Command)]
        public partial class CreateOrder
        {
            public interface ICreateOrderRequest { string Name { get; set; } }
            public interface ICreateOrderResponse { int Id { get; set; } string Name { get; set; } }
        }
        """;

    [Test]
    public async Task GeneratesOneSourceFile()
    {
        var result = GeneratorTestHelper.Run<GenerateCoreBoilerplateGenerator>(DomainSource);

        await Assert.That(result.GeneratedSources).Count().IsEqualTo(1);
    }

    [Test]
    public async Task GeneratedFileHasCorrectHintName()
    {
        var result = GeneratorTestHelper.Run<GenerateCoreBoilerplateGenerator>(DomainSource);

        await Assert.That(result.GeneratedSources[0].HintName).IsEqualTo("MyApp.Domains.Order.CreateOrder.Core.g.cs");
    }

    [Test]
    public async Task GeneratedSourceContainsCorrectNamespace()
    {
        var sources = GeneratorTestHelper.GetSources(
            GeneratorTestHelper.Run<GenerateCoreBoilerplateGenerator>(DomainSource));

        await Assert.That(sources["MyApp.Domains.Order.CreateOrder.Core.g.cs"]).Contains("namespace MyApp.Domains.Order;");
    }

    [Test]
    public async Task GeneratedSourceContainsPartialClassAndCoreClass()
    {
        var sources = GeneratorTestHelper.GetSources(
            GeneratorTestHelper.Run<GenerateCoreBoilerplateGenerator>(DomainSource));

        var source = sources["MyApp.Domains.Order.CreateOrder.Core.g.cs"];
        await Assert.That(source).Contains("public partial class CreateOrder");
        await Assert.That(source).Contains("protected sealed partial class Core");
    }

    [Test]
    public async Task GeneratedSourceContainsPresentationLayerEvent()
    {
        var sources = GeneratorTestHelper.GetSources(
            GeneratorTestHelper.Run<GenerateCoreBoilerplateGenerator>(DomainSource));

        await Assert.That(sources["MyApp.Domains.Order.CreateOrder.Core.g.cs"])
            .Contains("ICreateOrderEventPresentationLayer");
    }

    [Test]
    public async Task GeneratedSourceContainsApplicationLayerEvent()
    {
        var sources = GeneratorTestHelper.GetSources(
            GeneratorTestHelper.Run<GenerateCoreBoilerplateGenerator>(DomainSource));

        await Assert.That(sources["MyApp.Domains.Order.CreateOrder.Core.g.cs"])
            .Contains("ICreateOrderEventApplicationLayer");
    }

    [Test]
    public async Task GeneratedSourceContainsInfrastructureLayerEvent()
    {
        var sources = GeneratorTestHelper.GetSources(
            GeneratorTestHelper.Run<GenerateCoreBoilerplateGenerator>(DomainSource));

        await Assert.That(sources["MyApp.Domains.Order.CreateOrder.Core.g.cs"])
            .Contains("ICreateOrderEventInfrastructureLayer");
    }

    [Test]
    public async Task ProducesNoDiagnostics()
    {
        var result = GeneratorTestHelper.Run<GenerateCoreBoilerplateGenerator>(DomainSource);

        await Assert.That(result.Diagnostics).IsEmpty();
    }

    [Test]
    public async Task DomainClassWithoutDomainAttributeProducesNoOutput()
    {
        const string source = """
            namespace MyApp.Domains.Order;
            public class OrderDomain { }
            """;

        var result = GeneratorTestHelper.Run<GenerateCoreBoilerplateGenerator>(source);

        await Assert.That(result.GeneratedSources).IsEmpty();
    }
}
