namespace ProjectTemplate.Generators.Tests;

public class GenerateExtensionsBoilerplateGeneratorTests
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
        """;

    [Test]
    public async Task GeneratesOneSourceFile()
    {
        var result = GeneratorTestHelper.Run<GenerateExtensionsBoilerplateGenerator>(DomainSource);

        await Assert.That(result.GeneratedSources).Count().IsEqualTo(1);
    }

    [Test]
    public async Task GeneratedFileHasCorrectHintName()
    {
        var result = GeneratorTestHelper.Run<GenerateExtensionsBoilerplateGenerator>(DomainSource);

        await Assert.That(result.GeneratedSources[0].HintName).IsEqualTo("CreateOrder.Extensions.g.cs");
    }

    [Test]
    public async Task GeneratedSourceContainsCorrectNamespace()
    {
        var sources = GeneratorTestHelper.GetSources(
            GeneratorTestHelper.Run<GenerateExtensionsBoilerplateGenerator>(DomainSource));

        await Assert.That(sources["CreateOrder.Extensions.g.cs"])
            .Contains("namespace MyApp.Domains.Order;");
    }

    [Test]
    public async Task GeneratedSourceContainsMediatorExtensionMethod()
    {
        var sources = GeneratorTestHelper.GetSources(
            GeneratorTestHelper.Run<GenerateExtensionsBoilerplateGenerator>(DomainSource));

        await Assert.That(sources["CreateOrder.Extensions.g.cs"])
            .Contains("CreateOrder");
    }

    [Test]
    public async Task ProducesNoDiagnostics()
    {
        var result = GeneratorTestHelper.Run<GenerateExtensionsBoilerplateGenerator>(DomainSource);

        await Assert.That(result.Diagnostics).IsEmpty();
    }
}
