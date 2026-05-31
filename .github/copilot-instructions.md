# Copilot Instructions

## What is this project?
This is the **SlicedCore** template — the official .NET 10 reference implementation of the **Sliced Core Architecture**.

### Sliced Core Architecture
Sliced Core is a software architecture designed for microservices that enforces strict isolation at every level — including the domain/core contracts that in Clean Architecture or VSA are typically shared globally.

**The core ideas:**
- The application is divided into **Domains**. A domain encapsulates a set of related features.
- Each **Feature** is a self-contained slice within a domain. Features have no knowledge of other features and no coupling outside their domain.
- Every feature has **enforced internal layers**: Presentation → Application → Infrastructure. Layers communicate only via the mediator — never by direct injection across layer boundaries.
- **The Core layer is private per feature.** Unlike Clean Architecture or VSA — where Core/domain contracts are globally visible — in SlicedCore every DTO interface, domain model, and contract is scoped to its own feature. Nothing leaks.

**Why it was built:**
The primary driver is **AI-assisted development**. As AI coding assistants become a standard part of development workflows, architecture must be resilient to hallucination and corner-cutting. SlicedCore uses compile-time enforcement (keyed DI, source generators, layer-scoped service resolution) to make it structurally impossible — not just conventionally discouraged — to violate layer boundaries. Human developers benefit from the same guardrails: consistent structure, predictable patterns, and no hidden coupling.

**What the source generator does:**
The architecture is intentionally verbose — boilerplate that humans and AI would both get wrong. The `ProjectTemplate.Generators` Roslyn source generator eliminates that boilerplate entirely: it produces concrete DTO records, command events, mediator extension methods, and EF Core model registration from interface annotations at compile time. The generator is not optional tooling — it is load-bearing infrastructure that makes the architecture viable.

**Key differences from other architectures:**
| | Clean Architecture | VSA | SlicedCore |
|---|---|---|---|
| Feature isolation | ❌ Shared layers | ✅ Per feature | ✅ Per feature |
| Core/contracts scope | ❌ Global | ❌ Global | ✅ Private per feature |
| Layer enforcement | Advisory | None | Compile-time |
| AI/hallucination safe | ❌ | ❌ | ✅ |

---

## Project Overview
This template implements SlicedCore for .NET 10 microservices. Every feature lives in `src/ProjectTemplate/Domains/{DomainName}/` and is split across exactly four files:
- `Domain.cs` — entity and business model definitions
- `{Feature}.Contracts.cs` — DTO interfaces across all layers
- `{Feature}.cs` — layer logic (`PresentationLayer`, `ApplicationLayer`, `InfrastructureLayer`)
- `{Feature}.Endpoint.cs` — minimal-API endpoint registration

Use `src/ProjectTemplate/Domains/Sample/` as the canonical reference for all patterns.

---

## Architecture & Layer Rules

### Layer Isolation
Each layer can only access its own keyed DI services:
- `PresentationLayer` → `GetRequiredService<T>()` resolves presentation-keyed services
- `ApplicationLayer` → `GetRequiredService<T>()` resolves application-keyed services
- `InfrastructureLayer` → `GetRequiredService<T>()` and `GetRequiredDbContext<T>()` resolve infrastructure-keyed services
- Never inject services across layer boundaries directly

### Layer Communication
Layers communicate exclusively via the mediator using generated command events:
- Presentation → Application: `mediator.SendCommandAsync<ICreateXxxEventApplicationLayer, Result<IApplicationResponseDTO>>(...)`
- Application → Infrastructure: `mediator.SendCommandAsync<ICreateXxxEventInfrastructureLayer, Result<IPersistenceResponseDTO>>(...)`

### Result Pattern
Always use `Result<T>` (Ardalis.Result) for return types. Check `.IsError()` after every mediator call and propagate errors before continuing.

---

## Code Generation Guidelines

### Adding a New Feature
1. Create `{Feature}.Contracts.cs` with a `Core` partial class containing all DTO interfaces (`IXxxRequestDTO`, `IXxxResponseDTO`, `IApplicationRequestDTO`, `IApplicationResponseDTO`, `IPersistenceRequestDTO`, `IPersistenceResponseDTO`)
2. Create `{Feature}.cs` with `PresentationLayer`, `ApplicationLayer`, and `InfrastructureLayer` partial classes — each containing logic and a nested validator
3. Create `{Feature}.Endpoint.cs` with a nested `CreateXxxFeatureEndpoint : IEndpoint` inside `PresentationLayer`
4. The source generator produces concrete DTO records, command events, and mediator extensions automatically from the contracts interfaces — do not create these manually

### Adding a New Domain Entity
Annotate interfaces inside a `[Domain]`-decorated class:
- `[BusinessModel]` — domain/business model (no persistence concerns)
- `[PersistenceModel]` — EF Core entity
- `[PersistenceModelConfiguration(typeof(IXxx))]` — static method providing EF Core `ModelBuilder` configuration
The source generator produces concrete classes and registers them with `AppDbContext` at compile time.

### DTO Interface Conventions
- Tag PII properties with `[PiiData]`, safe-to-log properties with `[PublicData]`
- Interface names follow the pattern `IXxxRequestDTO`, `IXxxResponseDTO` per layer
- Use `Mapster` (`.Adapt<T>()`) for all DTO-to-DTO and DTO-to-entity mapping

---

## Dependency Registration
Register services in `src/ProjectTemplate/Program.Dependencies.cs` using the layer builders:
```csharp
// Presentation layer
builder.AddTransient<IMyService, MyService>();

// Infrastructure layer (also supports AddDbContext<T>)
builder.AddScoped<IMyRepo, MyRepo>();
```
Never call `IServiceCollection` directly from `Program.Dependencies.cs` — always go through the typed layer builder.

---

## Packages & Versions
All package versions are centrally managed in `src/Directory.Packages.props`. Never set `Version=` in individual `.csproj` files. To add a package: add a `<PackageVersion>` entry in `Directory.Packages.props` and a `<PackageReference>` (no version) in the project file.

Key packages in use:
| Purpose | Package |
|---|---|
| Mediator | `Cortex.Mediator` |
| Validation | `FluentValidation` |
| Mapping | `Mapster` |
| Results | `Ardalis.Result` |
| ORM | `Microsoft.EntityFrameworkCore` (SqlServer) |
| Auth | `Microsoft.AspNetCore.Authentication.JwtBearer` |
| API versioning | `Asp.Versioning.Http` |
| OpenAPI | `Microsoft.AspNetCore.OpenApi`, `Scalar.AspNetCore` |
| PII redaction | `Microsoft.Extensions.Compliance.Redaction` |
| Testing | `TUnit` |

---

## Code Style
- XML doc summaries (`/// <summary>`) are required on all public types and members
- Nullable reference types are enabled — never use `!` (null-forgiving) unless unavoidable; prefer null checks or null-coalescing
- Use `latest` C# language version — prefer records, pattern matching, collection expressions, and primary constructors where appropriate
- `TreatWarningsAsErrors` is enabled — all code must build warning-free
- Target framework: `net10.0` (except `ProjectTemplate.Generators` which stays on `netstandard2.0` — Roslyn analyzers requirement)

---

## Configuration
- Connection strings: `appsettings.json` → `ConnectionStrings:DefaultConnection`
- JWT: `appsettings.json` → `JwtBearer:Authority`, `JwtBearer:Audience`
- CORS: `appsettings.json` → `Cors:AllowedOrigins`, `Cors:AllowedMethods`, `Cors:AllowedHeaders`, `Cors:AllowCredentials`
- Never commit real secrets — use `appsettings.Development.json` (git-ignored) or environment variables

---

## Testing
Tests live in `src/ProjectTemplate.Tests/`. Use `TUnit` for all tests. Integration tests use `Microsoft.AspNetCore.Mvc.Testing` with `Microsoft.EntityFrameworkCore.Sqlite` as the in-memory store.
