namespace ProjectTemplate.Generators.Tests;

public class GenerateServiceProviderConstructorGeneratorTests
{
    // The generator looks up CreateOrder+PresentationLayer / ApplicationLayer / InfrastructureLayer
    // in the compilation.  Each must be a *partial* class whose base type is not System.Object,
    // and must not already have an IServiceProvider constructor.
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

        // Stub base class so the layer types satisfy CanGenerateFor (base != System.Object).
        public abstract class LayerBase
        {
            protected LayerBase(System.IServiceProvider serviceProvider) { }
        }

        public partial class CreateOrder
        {
            public partial class PresentationLayer : LayerBase { }
            public partial class ApplicationLayer : LayerBase { }
            public partial class InfrastructureLayer : LayerBase { }
        }
        """;

    [Test]
    public async Task GeneratesAtLeastOneSourceFile()
    {
        var result = GeneratorTestHelper.Run<GenerateServiceProviderConstructorGenerator>(DomainSource);

        await Assert.That(result.GeneratedSources).Count().IsGreaterThanOrEqualTo(1);
    }

    [Test]
    public async Task GeneratedSourceContainsCorrectNamespace()
    {
        var result = GeneratorTestHelper.Run<GenerateServiceProviderConstructorGenerator>(DomainSource);
        var combined = string.Concat(GeneratorTestHelper.GetSources(result).Values);

        await Assert.That(combined).Contains("namespace MyApp.Domains.Order;");
    }

    [Test]
    public async Task GeneratedSourceContainsPartialClass()
    {
        var result = GeneratorTestHelper.Run<GenerateServiceProviderConstructorGenerator>(DomainSource);
        var combined = string.Concat(GeneratorTestHelper.GetSources(result).Values);

        await Assert.That(combined).Contains("public partial class CreateOrder");
    }

    [Test]
    public async Task GeneratedSourceContainsIServiceProviderConstructorForEachLayer()
    {
        var result = GeneratorTestHelper.Run<GenerateServiceProviderConstructorGenerator>(DomainSource);
        var combined = string.Concat(GeneratorTestHelper.GetSources(result).Values);

        await Assert.That(combined).Contains("PresentationLayer");
        await Assert.That(combined).Contains("ApplicationLayer");
        await Assert.That(combined).Contains("InfrastructureLayer");
        await Assert.That(combined).Contains("global::System.IServiceProvider serviceProvider");
    }

    [Test]
    public async Task ProducesNoDiagnostics()
    {
        var result = GeneratorTestHelper.Run<GenerateServiceProviderConstructorGenerator>(DomainSource);

        await Assert.That(result.Diagnostics).IsEmpty();
    }
}

