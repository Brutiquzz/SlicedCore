# Architecture

This document describes the architectural patterns, conventions, and design decisions in the **SlicedCore** template.

## Overview

SlicedCore is a .NET 10 reference implementation of the **Sliced Core Architecture** — a microservice architecture where:
- Features are isolated slices within a domain, with no coupling between features or across domain boundaries
- Every feature has strictly enforced internal layers (Presentation → Application → Infrastructure)
- **The Core layer is protected per feature** — unlike Clean Architecture or VSA where domain contracts are globally shared, in SlicedCore every DTO interface and domain model is `protected` inside its own feature; only the public outer request/response contracts cross the feature boundary
- A Roslyn source generator produces all boilerplate at compile time, making the architecture viable without verbosity

---

## Solution Structure

```
src/
  ProjectTemplate/                    ← Main web application (solution template)
	Domains/
	  Sample/
		Domain.cs                      ← Domain marker with business/persistence interfaces
		CreateSample.cs                ← Feature logic (Presentation/Application/Infrastructure)
		CreateSample.Contracts.cs      ← DTOs and request/response interfaces (private per feature)
		CreateSample.Endpoint.cs       ← Minimal API endpoint mapping
	  .template/
		feature/
		  .template.config/
			template.json              ← dotnet new item template for features
		domain/
		  .template.config/
			template.json              ← dotnet new item template for domains
	Data/
	  AppDbContext.cs                  ← Internal EF Core context (keyed wrapper)
	  AppDbContextFactory.cs           ← Design-time factory for migrations
	Dependencies/                      ← Layer base classes
	Program.cs                         ← Main entry point
	Program.Services.cs                ← DI configuration
	Program.Middleware.cs              ← Middleware pipeline
	Program.Dependencies.cs            ← Layer-specific service registration
	GlobalUsings.cs

  ProjectTemplate.Framework/           ← Reusable framework package
	Dependencies/
	  Attributes/
		DomainAttribute.cs
		BusinessModelAttribute.cs
		PersistenceModelAttribute.cs
		PersistenceModelConfigurationAttribute.cs
	  PresentationLayerBuilder.cs
	  ApplicationLayerBuilder.cs
	  InfrastructureLayerBuilder.cs
	  CoreLayerBuilder.cs
	  InfrastructureDbContext.cs       ← Keyed EF Core wrapper
	Compliance/
	  DataClassifications.cs
	OpenApi/
	  ApiVersionDocumentTransformer.cs

  ProjectTemplate.Generators/          ← Roslyn incremental generators
	GenerateLayerModelsGenerator.cs
	GenerateDbContextGenerator.cs
	GeneratePresentationBoilerplateGenerator.cs
	GenerateApplicationBoilerplateGenerator.cs
	GenerateInfrastructureBoilerplateGenerator.cs
	GenerateCoreBoilerplateGenerator.cs
	GenerateExtensionsBoilerplateGenerator.cs

  ProjectTemplate.Tests/               ← TUnit + WebApplicationFactory tests
	Domains/
	  Sample/
		CreateSampleTests.cs
		CreateSampleTests.Handler.cs
```

---

## Layer Model

The architecture enforces strict layer boundaries through **keyed dependency injection** and **private nested types**:

```
┌─────────────────────────────────────────────────────┐
│              Presentation Layer                     │
│  (Endpoints, request validation, DTOs)              │
│  Key: ServiceKeys.Presentation                      │
└──────────────────┬──────────────────────────────────┘
				   │ mediator.SendCommandAsync
┌──────────────────▼──────────────────────────────────┐
│              Application Layer                      │
│  (Business logic, domain validation)                │
│  Key: ServiceKeys.Application                       │
└──────────────────┬──────────────────────────────────┘
				   │ mediator.SendCommandAsync
┌──────────────────▼──────────────────────────────────┐
│            Infrastructure Layer                     │
│  (EF Core, persistence, external I/O)               │
│  Key: ServiceKeys.Infrastructure                    │
└─────────────────────────────────────────────────────┘

The Core layer (DTO interfaces, domain contracts) is **protected per feature** — it lives inside
each feature's partial class hierarchy as a `protected sealed` class and is never shared across features or domains.
Only the public outer `I{Feature}Request` and `I{Feature}Response` contracts cross the feature boundary.
This is the fundamental difference from Clean Architecture and VSA.
```

### Encapsulation Rules

1. **Protected Core per feature** — internal DTO interfaces and domain contracts are declared inside each feature's `protected sealed Core` partial class. They are inaccessible from any other feature or domain. Only the public outer `I{Feature}Request` / `I{Feature}Response` contracts cross the feature boundary.
2. **Keyed service registration** — layer-specific services are registered with a layer key (e.g., `ServiceKeys.Infrastructure`) and resolved via `GetRequiredService<T>()` on the layer base class, which internally resolves from the correct keyed scope.
3. **No raw `IServiceProvider`** — layer base classes expose `GetRequiredService<T>()` and `GetService<T>()` scoped to the correct layer. Direct `IServiceProvider` access is not exposed.
4. **Internal `AppDbContext`** — the EF Core context is `internal` and wrapped by `InfrastructureDbContext<TContext>`, preventing direct injection into any layer other than infrastructure.

---

## Typed Mediator Surfaces

Cross-feature dispatch is controlled via two typed surfaces — concrete `readonly struct` types that wrap `IMediator` and expose only the extension methods appropriate to that layer's CQRS role:

| Type | Available in | Exposes | Purpose |
|---|---|---|---|
| `Dependencies.Commands` | `InfrastructureLayer` of command features | Command extension methods (e.g. `commands.CreateOrder(...)`) | Cross-feature writes from orchestration |
| `Dependencies.Queries` | `ApplicationLayer` of query features | Query extension methods (e.g. `queries.GetOrder(...)`) | Cross-feature reads from business logic |

Extension methods are generated per-feature and attached to the correct surface based on `[Feature(FeatureType.Command)]` vs `[Feature(FeatureType.Query)]`. Both expose a **fluent builder**:

```csharp
// InfrastructureLayer of a command feature — cross-feature command dispatch
var result = await commands.CreateOrder()
    .WithCustomerId(customerId)
    .WithItems(items)
    .Send(cancellationToken);

// ApplicationLayer of a query feature — cross-feature query dispatch
var order = await queries.GetOrder()
    .WithId(id)
    .Send(cancellationToken);
```

Endpoints and tests use raw `IMediator` directly, which also has all extension methods attached to it:

```csharp
// Endpoint or test — dispatch via IMediator
var result = await mediator.CreateSample(request, cancellationToken);
var item = await mediator.GetSample().WithId(id).Send(cancellationToken);
```

A query feature's `InfrastructureLayer` does not receive a `Commands` surface — it only has the persistence context. Command dispatch from a query infrastructure layer is therefore not possible by construction.

---

## Source Generators

The repository uses Roslyn incremental generators to emit boilerplate and enforce patterns:

### **1. `GenerateLayerModelsGenerator`**
- **Trigger**: `[Domain]` attribute on a class
- **Input**: 
  - `[BusinessModel]` interface → business logic model
  - `[PersistenceModel]` interfaces → EF Core entities
  - `[PersistenceModelConfiguration(typeof(IEntity))]` methods → EF Core fluent configuration
- **Output**:
  - `Sample.ApplicationLayerModels.g.cs` — private `ISample` interface copy + `Sample` record
  - `Sample.InfrastructureLayerModels.g.cs` — private `ISampleEntity` / `ISampleEntityDetail` copies + `SampleEntity` / `SampleEntityDetail` records + `RegisterEntities(ModelBuilder)` method

### **2. `GenerateDbContextGenerator`**
- **Trigger**: `[Domain]` classes with persistence models
- **Output**: `AppDbContext.g.cs` — partial `internal class AppDbContext : DbContext` with `OnModelCreating` calling all domain `RegisterEntities` methods

### **3. Layer Boilerplate Generators**
- **`GeneratePresentationBoilerplateGenerator`** — emits `PresentationLayer` partial class with handler, validator, event types, DTOs
- **`GenerateApplicationBoilerplateGenerator`** — emits `ApplicationLayer` partial class with handler, validator, event types, DTOs
- **`GenerateInfrastructureBoilerplateGenerator`** — emits `InfrastructureLayer` partial class with handler, DTOs
- **`GenerateCoreBoilerplateGenerator`** — emits `Core` nested class with event interfaces and response DTOs

### **4. `GenerateExtensionsBoilerplateGenerator`**
- **Trigger**: `[Feature]` attribute on a class
- **Output**: Typed fluent builders and mediator extension methods, scoped by feature type:
  - **Command features** → `CreateSample(this Commands)` and `CreateSample(this IMediator)` overloads — callable from command infrastructure layers and from endpoints/tests respectively
  - **Query features** → `GetSample(this Queries)` and `GetSample(this IMediator)` overloads — callable from query application layers and from endpoints/tests respectively
  - Both variants include a fluent `Builder` class enabling `mediator.CreateSample().WithName("...").Send(ct)` and `mediator.GetSample().WithId(1).Send(ct)`

---

## Domain Marker Pattern

A **domain** is a container for business and persistence models. It uses marker attributes to trigger code generation:

### Example: `Domains/Sample/Domain.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using ProjectTemplate.Dependencies.Attributes;

namespace ProjectTemplate.Domains.Sample;

[Domain]
public class SampleDomain
{
	[BusinessModel]
	private interface ISample
	{
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
		entity.Property(e => e.Name).IsRequired().HasMaxLength(256);
		// ... rest of configuration
	}

	[PersistenceModelConfiguration(typeof(ISampleEntityDetail))]
	private static void ConfigureSampleEntityDetail(ModelBuilder modelBuilder)
	{
		var entity = modelBuilder.Entity<ISampleEntityDetail>();
		entity.ToTable("SampleDetails");
		// ... rest of configuration
	}
}
```

**Key points:**
- `[Domain]` triggers the generators
- `[BusinessModel]` defines the application-layer model contract
- `[PersistenceModel]` defines EF Core entity contracts
- `[PersistenceModelConfiguration(typeof(IEntity))]` provides fluent configuration for each entity
- All interfaces are `private` — generators emit layer-local copies

---

## `ProjectTemplate.Framework` Package

The `ProjectTemplate.Framework` project contains reusable support code intended for NuGet packaging:

- **Attributes** — `[Domain]`, `[BusinessModel]`, `[PersistenceModel]`, `[PersistenceModelConfiguration]`, `[PiiData]`, `[PublicData]`
- **Layer builders** — `PresentationLayerBuilder`, `ApplicationLayerBuilder`, `InfrastructureLayerBuilder`, `CoreLayerBuilder` (each wraps `IServiceCollection` with keyed registration scoped to that layer)
- **Layer base classes** — `Presentation`, `Application`, `Infrastructure`, `Core` (injected by the generator into each feature's partial class hierarchy)
- **Infrastructure helpers** — `InfrastructureDbContext<TContext>` keyed wrapper for EF Core contexts
- **Compliance** — `DataClassifications`, `PassThroughRedactor`, `RedactedLog<T>` for PII redaction in logs
- **OpenAPI** — `ApiVersionDocumentTransformer` for JWT security scheme injection and path token replacement
- **Scanners** — `ApiEndpointScanner` (discovers and maps `IEndpoint` implementations), `LayerHandlerScanner` (registers command handlers and layer-scoped validators)

This separation ensures the main application (`ProjectTemplate`) can reference immutable framework code without risk of accidental modification.

---

## Item Templates

Two `dotnet new` item templates live in `src/ProjectTemplate/Domains/.template/`:

### **Feature Template** (`dotnet new feature -n GetSample --operation GET`)
- **Location**: `Domains/.template/feature/.template.config/template.json`
- **Source files**: References `Domains/Sample/CreateSample.cs`, `CreateSample.Contracts.cs`, `CreateSample.Endpoint.cs` via relative path
- **Substitution**: `sourceName: "CreateSample"` — all occurrences of `CreateSample` in file names and contents are replaced with the user-provided name
- **Operation parameter**: `--operation GET|POST|PUT|PATCH|DELETE` — determines the endpoint HTTP method, route pattern, and CQRS feature type (`GET` → query; all others → command)
- **Optional endpoint**: `--include-endpoint false` excludes `CreateSample.Endpoint.cs`

### **Domain Template** (`dotnet new domain -n Order`)
- **Location**: `Domains/.template/domain/.template.config/template.json`
- **Source file**: References `Domains/Sample/Domain.cs` via relative path
- **Substitution**: `sourceName: "Sample"` — all occurrences of `Sample` (in class names, interface names, method names) are replaced with the user-provided name

**Why no file duplication?**  
The `template.json` `sources` property points directly to the existing source files in `Domains/Sample/`. Changes to those files are automatically reflected in the item templates on the next `dotnet new install`.

---

## Testing Strategy

- **Unit/Integration tests** in `ProjectTemplate.Tests` using **TUnit** and **WebApplicationFactory**
- **CI pipeline** runs `dotnet build` + `dotnet test` + item template install smoke-tests on every push/PR to `main`
- Generated output is inspected via `EmitCompilerGeneratedFiles=true` and `CompilerGeneratedFilesOutputPath=Generated` in the csproj

---

## Key Decisions

1. **Protected outer contracts, protected inner Core** — public `I{Feature}Request` / `I{Feature}Response` contracts cross the feature boundary; everything inside `protected sealed Core` is inaccessible outside the feature. This is a stronger isolation guarantee than Clean Architecture or VSA.
2. **Keyed DI** — prevent accidental cross-layer service resolution without sacrificing framework integration (e.g., EF Core, validators)
3. **Internal `AppDbContext`** — wrap in `InfrastructureDbContext<TContext>` to prevent direct controller injection
4. **Source generators over reflection** — compile-time boilerplate generation for zero runtime overhead and better IDE tooling
5. **Item templates reference source files** — no duplication; `Domains/Sample/` is the single source of truth
6. **`ProjectTemplate.Framework` split** — package reusable infrastructure so consumers can reference it without modifying core support code
7. **Typed dispatch surfaces (`Commands` / `Queries` structs)** — each layer receives only the cross-feature dispatch surface appropriate to its CQRS role; `Commands` is available only in command infrastructure layers, `Queries` only in query application layers. Endpoints and tests use raw `IMediator`.
8. **No `IPresentationMediator`** — presentation layers never invoke other features; endpoints dispatch into their own feature's presentation event via raw `IMediator`, and any cross-feature call from presentation is an architecture violation by definition

---

## Adding a New Generator

All generators live in `src/ProjectTemplate.Generators/` and follow the same structure. Here is the minimal skeleton and the conventions to follow:

### 1. Create the generator class

```csharp
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Text;

namespace ProjectTemplate.Generators;

[Generator]
public sealed class GenerateMyFeatureGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // 1. Declare a provider that filters syntax nodes by attribute.
        var candidates = context.SyntaxProvider.ForAttributeWithMetadataName(
            fullyQualifiedMetadataName: "ProjectTemplate.Dependencies.Attributes.DomainAttribute",
            predicate: static (node, _) => node is ClassDeclarationSyntax,
            transform: static (ctx, ct) =>
            {
                ct.ThrowIfCancellationRequested();
                return (INamedTypeSymbol)ctx.TargetSymbol;
            })
            .Where(static s => s is not null);

        // 2. Combine with CompilationProvider when you need type resolution.
        var compilationAndCandidates = context.CompilationProvider.Combine(candidates.Collect());

        // 3. Register a source output.
        context.RegisterSourceOutput(compilationAndCandidates, static (spc, source) =>
        {
            var (compilation, symbols) = source;
            foreach (var symbol in symbols.Distinct(SymbolEqualityComparer.Default).OfType<INamedTypeSymbol>())
            {
                var sourceText = BuildSource(symbol);
                spc.AddSource(GetHintName(symbol), SourceText.From(sourceText, Encoding.UTF8));
            }
        });
    }

    private static string BuildSource(INamedTypeSymbol symbol) { /* ... */ return string.Empty; }

    // Hint name convention: "<Namespace>.<ModelName>.<Purpose>.g.cs"
    private static string GetHintName(INamedTypeSymbol symbol)
        => $"{symbol.ContainingNamespace.ToDisplayString()}.{symbol.Name}.MyFeature.g.cs";
}
```

### 2. Pipeline conventions

| Step | Convention |
|---|---|
| **Trigger attribute** | Always use `ForAttributeWithMetadataName` — never walk the full syntax tree. Provide the fully-qualified metadata name (e.g. `ProjectTemplate.Dependencies.Attributes.DomainAttribute`). |
| **Predicate** | Keep it pure (`static`) and cheap — only check node kind (e.g. `node is ClassDeclarationSyntax`). |
| **Transform** | Cast `ctx.TargetSymbol` to `INamedTypeSymbol`; call `ct.ThrowIfCancellationRequested()` at the start. |
| **Combine** | Use `context.CompilationProvider.Combine(...)` only when you need cross-symbol type resolution (e.g. `compilation.GetTypeByMetadataName`). |
| **Output** | Call `productionContext.AddSource(hintName, SourceText.From(text, Encoding.UTF8))`. |

### 3. Hint name convention

Generated file hint names follow this pattern:

```
{TargetNamespace}.{ModelName}.{Purpose}.g.cs
```

Examples from existing generators:

| Generator | Hint name |
|---|---|
| `GeneratePresentationBoilerplateGenerator` | `ProjectTemplate.Domains.Sample.SampleDomain.PresentationBoilerplate.g.cs` |
| `GenerateApplicationBoilerplateGenerator` | `ProjectTemplate.Domains.Sample.SampleDomain.ApplicationBoilerplate.g.cs` |
| `GenerateLayerModelsGenerator` (application) | `ProjectTemplate.Domains.Sample.SampleDomain.ApplicationLayerModels.g.cs` |
| `GenerateLayerModelsGenerator` (infrastructure) | `ProjectTemplate.Domains.Sample.SampleDomain.InfrastructureLayerModels.g.cs` |
| `GenerateDbContextGenerator` | `ProjectTemplate.Data.AppDbContext.g.cs` |

### 4. Available trigger attributes

| Attribute | Namespace | Used by |
|---|---|---|
| `[Domain]` | `ProjectTemplate.Dependencies.Attributes` | All layer boilerplate generators, `GenerateLayerModelsGenerator`, `GenerateDbContextGenerator` |
| `[BusinessModel]` | `ProjectTemplate.Dependencies.Attributes` | `GenerateLayerModelsGenerator` (application layer model) |
| `[PersistenceModel]` | `ProjectTemplate.Dependencies.Attributes` | `GenerateLayerModelsGenerator` (infrastructure layer models) |
| `[PersistenceModelConfiguration(typeof(IEntity))]` | `ProjectTemplate.Dependencies.Attributes` | `GenerateLayerModelsGenerator` (EF Core fluent config) |

### 5. Testing a new generator

Generator unit tests live in `src/ProjectTemplate.Generators.Tests/`. Use `Microsoft.CodeAnalysis.CSharp.Testing` (or the existing test helpers in that project) to pass a source string through the generator and assert on `GeneratorDriverRunResult.GeneratedTrees`. Run with:

```
dotnet test src/ProjectTemplate.Generators.Tests/ProjectTemplate.Generators.Tests.csproj
```

The generated output files are also written to disk under `src/ProjectTemplate/Generated/` (controlled by `EmitCompilerGeneratedFiles=true` in the csproj) so you can inspect them directly during development.

---

## References

- [Roslyn Source Generators](https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/source-generators-overview)
- [dotnet new custom templates](https://learn.microsoft.com/en-us/dotnet/core/tools/custom-templates)
- [Keyed Services in .NET](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection#keyed-services)
- [EF Core Fluent API](https://learn.microsoft.com/en-us/ef/core/modeling/)
