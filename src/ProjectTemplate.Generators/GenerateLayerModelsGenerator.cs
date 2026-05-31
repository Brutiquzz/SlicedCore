using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace ProjectTemplate.Generators;

[Generator]
public sealed class GenerateLayerModelsGenerator : IIncrementalGenerator
{
    private sealed class PersistenceEntry
    {
        public INamedTypeSymbol Interface { get; }
        public string? ConfigurationBody { get; }

        public PersistenceEntry(INamedTypeSymbol iface, string? configurationBody)
        {
            Interface = iface;
            ConfigurationBody = configurationBody;
        }
    }

    private sealed class DomainModel
    {
        public INamedTypeSymbol Symbol { get; }
        public INamedTypeSymbol? BusinessInterface { get; }
        public IReadOnlyList<PersistenceEntry> PersistenceEntries { get; }

        public DomainModel(INamedTypeSymbol symbol, INamedTypeSymbol? businessInterface, IReadOnlyList<PersistenceEntry> persistenceEntries)
        {
            Symbol = symbol;
            BusinessInterface = businessInterface;
            PersistenceEntries = persistenceEntries;
        }
    }

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var domainClasses = context.SyntaxProvider.ForAttributeWithMetadataName(
            fullyQualifiedMetadataName: "ProjectTemplate.Dependencies.Attributes.DomainAttribute",
            predicate: static (node, _) => node is ClassDeclarationSyntax,
            transform: static (syntaxContext, cancellationToken) =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                var symbol = (INamedTypeSymbol)syntaxContext.TargetSymbol;
                var classDecl = (ClassDeclarationSyntax)syntaxContext.TargetNode;

                // Build a map from interface name -> configuration body
                // by reading all methods tagged [PersistenceModelConfiguration(typeof(IXxx))]
                var configBodies = new Dictionary<string, string>(StringComparer.Ordinal);
                foreach (var method in classDecl.Members.OfType<MethodDeclarationSyntax>())
                {
                    foreach (var attrList in method.AttributeLists)
                    {
                        foreach (var attr in attrList.Attributes)
                        {
                            var attrName = attr.Name.ToString();
                            if (!attrName.Contains("PersistenceModelConfiguration")) continue;

                            // Extract the typeof() argument to get the interface name
                            var typeofArg = attr.ArgumentList?.Arguments.FirstOrDefault()?.ToString();
                            if (typeofArg is null) continue;

                            // typeof(ISampleEntity) -> ISampleEntity
                            var interfaceName = typeofArg
                                .Replace("typeof(", "")
                                .Replace(")", "")
                                .Trim();

                            if (method.Body is { } body && body.Statements.Count > 0)
                            {
                                var sb = new StringBuilder();
                                foreach (var statement in body.Statements)
                                    sb.AppendLine("            " + statement.ToString().TrimStart());
                                configBodies[interfaceName] = sb.ToString();
                            }
                        }
                    }
                }

                // Collect all [PersistenceModel] interfaces
                var persistenceEntries = new List<PersistenceEntry>();
                foreach (var member in symbol.GetTypeMembers())
                {
                    if (member.TypeKind != TypeKind.Interface) continue;
                    var hasPersistence = member.GetAttributes().Any(a =>
                        a.AttributeClass?.ToDisplayString() ==
                        "ProjectTemplate.Dependencies.Attributes.PersistenceModelAttribute");
                    if (!hasPersistence) continue;

                    configBodies.TryGetValue(member.Name, out var body);
                    persistenceEntries.Add(new PersistenceEntry(member, body));
                }

                // Business interface (still single)
                INamedTypeSymbol? businessInterface = null;
                foreach (var member in symbol.GetTypeMembers())
                {
                    if (member.TypeKind != TypeKind.Interface) continue;
                    var hasBusiness = member.GetAttributes().Any(a =>
                        a.AttributeClass?.ToDisplayString() ==
                        "ProjectTemplate.Dependencies.Attributes.BusinessModelAttribute");
                    if (hasBusiness) { businessInterface = member; break; }
                }

                return new DomainModel(symbol, businessInterface, persistenceEntries);
            })
            .Where(static m => m.Symbol is not null);

        context.RegisterSourceOutput(domainClasses, static (productionContext, domainModel) =>
        {
            var domainSymbol = domainModel.Symbol;
            var targetNamespace = domainSymbol.ContainingNamespace.IsGlobalNamespace
                ? string.Empty
                : domainSymbol.ContainingNamespace.ToDisplayString();

            if (string.IsNullOrWhiteSpace(targetNamespace)) return;

            var rawName = domainSymbol.Name;
            var domainName = rawName.EndsWith("Domain", StringComparison.Ordinal)
                ? rawName.Substring(0, rawName.Length - "Domain".Length)
                : rawName;
            var featureName = $"Create{domainName}";

            if (domainModel.BusinessInterface is not null)
            {
                var source = BuildApplicationLayerModels(targetNamespace, featureName, domainModel.BusinessInterface);
                productionContext.AddSource(
                    $"{targetNamespace}.{domainName}.ApplicationLayerModels.g.cs",
                    SourceText.From(source, Encoding.UTF8));
            }

            if (domainModel.PersistenceEntries.Count > 0)
            {
                var source = BuildInfrastructureLayerModels(targetNamespace, featureName, domainModel.PersistenceEntries);
                productionContext.AddSource(
                    $"{targetNamespace}.{domainName}.InfrastructureLayerModels.g.cs",
                    SourceText.From(source, Encoding.UTF8));
            }
        });
    }

    private static string BuildApplicationLayerModels(
        string targetNamespace, string featureName, INamedTypeSymbol businessInterface)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine($"namespace {targetNamespace};");
        sb.AppendLine();

        // Emit as internal namespace-level types so every feature in this domain can access them.
        var modelName = StripLeadingI(businessInterface.Name);

        sb.AppendLine($"internal interface {businessInterface.Name}");
        sb.AppendLine("{");
        foreach (var prop in GetProperties(businessInterface))
            sb.AppendLine($"    {prop.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)} {prop.Name} {{ get; set; }}");
        sb.AppendLine("}");
        sb.AppendLine();

        sb.AppendLine($"internal sealed class {modelName} : {businessInterface.Name}");
        sb.AppendLine("{");
        foreach (var prop in GetProperties(businessInterface))
        {
            var typeName = prop.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var defaultVal = GetDefault(prop.Type);
            sb.Append($"    public {typeName} {prop.Name} {{ get; set; }}");
            sb.AppendLine(defaultVal is not null ? $" = {defaultVal};" : string.Empty);
        }
        sb.AppendLine("}");

        return sb.ToString();
    }

    private static string BuildInfrastructureLayerModels(
        string targetNamespace, string featureName, IReadOnlyList<PersistenceEntry> entries)
    {
        var ifaceToLocal = entries.ToDictionary(
            e => e.Interface.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            e => StripLeadingI(e.Interface.Name),
            StringComparer.Ordinal);

        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine("using Microsoft.EntityFrameworkCore;");
        sb.AppendLine();
        sb.AppendLine($"namespace {targetNamespace};");
        sb.AppendLine();

        // Emit entity interface + record as internal namespace-level types so every feature
        // in this domain (e.g. GetSample alongside CreateSample) can access them.
        foreach (var entry in entries)
        {
            var entityName = StripLeadingI(entry.Interface.Name);

            sb.AppendLine($"internal interface {entry.Interface.Name}");
            sb.AppendLine("{");
            foreach (var prop in GetProperties(entry.Interface))
            {
                var typeName = ResolveTypeName(prop.Type, ifaceToLocal);
                sb.AppendLine($"    {typeName} {prop.Name} {{ get; set; }}");
            }
            sb.AppendLine("}");
            sb.AppendLine();

            sb.AppendLine($"internal sealed record {entityName} : {entry.Interface.Name}");
            sb.AppendLine("{");
            foreach (var prop in GetProperties(entry.Interface))
            {
                var typeName = ResolveTypeName(prop.Type, ifaceToLocal);
                var defaultVal = GetDefault(prop.Type);
                sb.Append($"    public {typeName} {prop.Name} {{ get; set; }}");
                sb.AppendLine(defaultVal is not null ? $" = {defaultVal};" : string.Empty);
            }
            sb.AppendLine("}");
            sb.AppendLine();
        }

        // RegisterEntities stays on CreateSample.InfrastructureLayer — it's the only feature
        // that owns the schema for this domain. The DbContext generator calls it by FQN.
        sb.AppendLine($"public partial class {featureName}");
        sb.AppendLine("{");
        sb.AppendLine("    partial class InfrastructureLayer");
        sb.AppendLine("    {");
        sb.AppendLine("        internal static void RegisterEntities(ModelBuilder modelBuilder)");
        sb.AppendLine("        {");
        foreach (var entry in entries)
        {
            var entityName = StripLeadingI(entry.Interface.Name);
            if (!string.IsNullOrWhiteSpace(entry.ConfigurationBody))
            {
                var body = entry.ConfigurationBody!;
                foreach (var e in entries)
                {
                    var concreteName = StripLeadingI(e.Interface.Name);
                    body = body
                        .Replace($"Entity<{e.Interface.Name}>()", $"Entity<{concreteName}>()")
                        .Replace($"<{e.Interface.Name}>", $"<{concreteName}>")
                        .Replace(e.Interface.Name + ".", concreteName + ".");
                }
                sb.AppendLine("            {");
                sb.Append(body);
                sb.AppendLine("            }");
            }
            else
            {
                sb.AppendLine($"            modelBuilder.Entity<{entityName}>();");
            }
        }
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    private static string ResolveTypeName(ITypeSymbol type, Dictionary<string, string> ifaceToLocal)
    {
        var fqn = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        return ifaceToLocal.TryGetValue(fqn, out var localName) ? localName : fqn;
    }

    private static IEnumerable<IPropertySymbol> GetProperties(INamedTypeSymbol interfaceSymbol)
        => interfaceSymbol.AllInterfaces
            .Concat(new[] { interfaceSymbol })
            .SelectMany(static t => t.GetMembers().OfType<IPropertySymbol>())
            .GroupBy(static p => p.Name, StringComparer.Ordinal)
            .Select(static g => g.First());

    private static string StripLeadingI(string name)
        => name.Length > 1 && name[0] == 'I' && char.IsUpper(name[1])
            ? name.Substring(1)
            : name;

    private static string? GetDefault(ITypeSymbol type)
    {
        if (type.SpecialType == SpecialType.System_String) return "string.Empty";
        if (type.IsValueType && type.NullableAnnotation != NullableAnnotation.Annotated) return "default";
        // Non-nullable reference type navigation properties must be initialised; use null! to satisfy the compiler.
        if (!type.IsValueType && type.NullableAnnotation != NullableAnnotation.Annotated) return "null!";
        return null;
    }
}
