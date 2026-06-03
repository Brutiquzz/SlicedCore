using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace ProjectTemplate.Generators.Tests;

/// <summary>
/// Shared helper that builds an in-memory Roslyn compilation pre-loaded with the
/// stub attribute definitions the generators depend on, then runs a generator and
/// returns the produced sources and diagnostics.
/// </summary>
internal static class GeneratorTestHelper
{
    /// <summary>
    /// Stubs for all framework attributes referenced by the generators.
    /// The generator project only depends on Roslyn — it never references the
    /// real ProjectTemplate.Framework assembly, so we supply lightweight stubs.
    /// </summary>
    private const string AttributeStubs = """
        namespace ProjectTemplate.Dependencies.Attributes
        {
            [System.AttributeUsage(System.AttributeTargets.Class, Inherited = false)]
            public sealed class DomainAttribute : System.Attribute { }

            [System.AttributeUsage(System.AttributeTargets.Interface)]
            public sealed class BusinessModelAttribute : System.Attribute { }

            [System.AttributeUsage(System.AttributeTargets.Interface)]
            public sealed class PersistenceModelAttribute : System.Attribute { }

            public enum FeatureType { Command = 0, Query = 1 }

            [System.AttributeUsage(System.AttributeTargets.Class, Inherited = false)]
            public sealed class FeatureAttribute : System.Attribute
            {
                public FeatureType FeatureType { get; }
                public FeatureAttribute(FeatureType featureType = FeatureType.Command) => FeatureType = featureType;
            }

            [System.AttributeUsage(System.AttributeTargets.Method, AllowMultiple = false)]
            public sealed class PersistenceModelConfigurationAttribute : System.Attribute
            {
                public System.Type ModelInterface { get; }
                public PersistenceModelConfigurationAttribute(System.Type modelInterface) => ModelInterface = modelInterface;
            }
        }

        // Minimal stubs so generated code that references these types compiles cleanly.
        namespace ProjectTemplate.Dependencies
        {
            public abstract class Core
            {
                protected Core(System.IServiceProvider sp) { }
            }
        }

        namespace Cortex.Mediator.Commands
        {
            public interface ICommand<TResult> { }
            public interface ICommandHandler<TCommand, TResult> { }
        }

        namespace Ardalis.Result
        {
            public class Result<T> { }
        }

        namespace Microsoft.EntityFrameworkCore
        {
            public class ModelBuilder { }
        }
        """;

    /// <summary>
    /// Runs <typeparamref name="TGenerator"/> against a compilation that contains
    /// <paramref name="userSource"/> plus the framework attribute stubs.
    /// </summary>
    public static GeneratorRunResult Run<TGenerator>(string userSource)
        where TGenerator : IIncrementalGenerator, new()
    {
        var compilation = BuildCompilation(userSource);
        var generator = new TGenerator();
        var driver = CSharpGeneratorDriver.Create(generator)
            .RunGeneratorsAndUpdateCompilation(compilation, out _, out _);

        return driver.GetRunResult().Results[0];
    }

    /// <summary>
    /// Returns all generated source text for a given generator run, keyed by hint name.
    /// </summary>
    public static IReadOnlyDictionary<string, string> GetSources(GeneratorRunResult result)
        => result.GeneratedSources.ToImmutableDictionary(
            s => s.HintName,
            s => s.SourceText.ToString());

    private static CSharpCompilation BuildCompilation(string userSource)
    {
        var references = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrWhiteSpace(a.Location))
            .Select(a => MetadataReference.CreateFromFile(a.Location))
            .Cast<MetadataReference>()
            .ToList();

        return CSharpCompilation.Create(
            assemblyName: "TestAssembly",
            syntaxTrees:
            [
                CSharpSyntaxTree.ParseText(AttributeStubs),
                CSharpSyntaxTree.ParseText(userSource)
            ],
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }
}
