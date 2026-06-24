# SlicedCore Template

![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet) ![License: MIT](https://img.shields.io/badge/License-MIT-green) ![CI](https://github.com/Brutiquzz/Templates/actions/workflows/ci.yml/badge.svg)

The official .NET 10 reference implementation of the **Sliced Core Architecture** — a microservice architecture built to be structurally impossible to violate, whether the code is written by a human or an AI assistant.

---

## What is Sliced Core?

Sliced Core is a software architecture where:

- The application is divided into **Domains**. A domain encapsulates a set of related features.
- Each **Feature** is a fully isolated slice. Features have no knowledge of the internal logic of any other feature — whether inside the same domain or across domains. There is no coupling between features anywhere in the codebase. The domain is an organisational grouping only, not a coupling boundary.
- Every feature has **enforced internal layers**: Presentation → Application → Infrastructure. Layers communicate only via the mediator — never by direct injection across boundaries.
- **The Core layer is protected per feature.** Unlike Clean Architecture or VSA — where domain contracts are globally visible — in SlicedCore every internal DTO interface and layer contract is `protected` inside its own feature. Only the public outer request/response contracts cross the feature boundary. Nothing internal leaks.

| | Clean Architecture | VSA | SlicedCore |
|---|---|---|---|
| Feature isolation | ❌ Shared layers | ✅ Per feature | ✅ Per feature |
| Core/contracts scope | ❌ Global | ❌ Global | ✅ Protected per feature |
| Layer enforcement | Advisory | None | Compile-time |
| AI/hallucination safe | ❌ | ❌ | ✅ |

The "AI/hallucination safe" row means the architecture is structurally resistant to common AI coding mistakes. Layer boundaries are enforced by the compiler via keyed DI and source-generated types — bypassing them is a compile error, not a convention violation that slips through code review.

---

## Features

- **Compile-time layer enforcement** via keyed dependency injection — wrong-layer access does not compile
- **Protected Core per feature** — internal layer contracts are `protected` and cannot be referenced outside their own feature; only the public outer request/response contracts are visible to callers
- **Roslyn source generator** eliminates all boilerplate: DTO records, command events, mediator extensions, and EF Core registration are produced at compile time
- **Domain-driven scaffolding** via marker attributes (`[Domain]`, `[BusinessModel]`, `[PersistenceModel]`)
- **`dotnet new` item templates** for rapid feature and domain creation
- **EF Core** with internal `AppDbContext` wrapped by `InfrastructureDbContext<T>` to prevent direct injection
- **FluentValidation**, **Mapster**, **Cortex.Mediator**, **Ardalis.Result**
- **OpenAPI** with API versioning and Scalar UI
- **JWT Bearer authentication** and **CORS** configured from `appsettings.json`
- **PII redaction** via `Microsoft.Extensions.Compliance.Redaction`
- **Opinionated JSON serialization** — camelCase, null-omitting, case-insensitive via `System.Text.Json`
- **ASP.NET Core Health Checks** with `/healthz`, `/healthz/live`, and `/healthz/ready` endpoints
- **Built-in rate limiting** — global partitioned limiter plus `fixed`, `sliding`, `token`, and `strict` named policies configured from `appsettings.json`
- **TUnit** integration tests with `WebApplicationFactory`

---

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Git](https://git-scm.com/)

### Install the solution template

From the repository root:

```bash
dotnet new install ./
```

### Create a new solution

```bash
dotnet new my-web -n MyApi
```

This generates a complete solution with:
- `src/MyApi/` — main web application
- `src/MyApi.Framework/` — reusable framework package
- `src/MyApi.Generators/` — Roslyn source generator
- `src/MyApi.Tests/` — TUnit integration tests

### Install the item templates

Run these from the repository root. The paths point to the item template sources inside the template repo:

```bash
dotnet new install ./src/ProjectTemplate/Domains/.template/feature
dotnet new install ./src/ProjectTemplate/Domains/.template/domain
```

---

## Usage

### Creating a New Domain

```bash
cd src/MyApi/Domains
mkdir Order
cd Order
dotnet new domain -n Order --projectName MyApi
```

| Parameter | Required | Description |
|---|---|---|
| `-n`, `--name` | ✅ | Domain name. Used as the class name in `Domain.cs` and to name the file. |
| `--projectName` | | Root namespace of the target project (e.g. `MyApi`). Must match the project's assembly name. Defaults to `ProjectTemplate` — always set this explicitly. |

This creates `Domain.cs` with business model, persistence entity, and EF Core configuration stubs — all wired to the source generator.

### Creating a New Feature

```bash
cd src/MyApi/Domains/Order
dotnet new feature -n CreateOrder --projectName MyApi --operation POST
```

| Parameter | Required | Description |
|---|---|---|
| `-n`, `--name` | ✅ | Feature name (e.g. `CreateOrder`, `GetOrder`). The domain name is derived automatically by stripping the verb prefix (`Create`, `Get`, `Update`, `Delete`, `List`, `Patch`). |
| `--projectName` | | Root namespace of the target project (e.g. `MyApi`). Must match the project's assembly name. Defaults to `ProjectTemplate` — always set this explicitly. |
| `--operation` | | HTTP operation: `GET`, `POST` (default), `PUT`, `PATCH`, or `DELETE`. Controls the endpoint HTTP method, route pattern, status codes, and generated CQRS feature type (`GET` → query; all others → command). |
| `--include-endpoint` | | `true` (default) or `false`. Set to `false` to skip generating the endpoint file. |

This creates:
- `CreateOrder.Contracts.cs` — public `ICreateOrderRequest` / `ICreateOrderResponse` contracts and the `protected sealed Core` with all layer DTO interfaces
- `CreateOrder.cs` — `PresentationLayer`, `ApplicationLayer`, and `InfrastructureLayer` logic stubs with validators
- `CreateOrder.Endpoint.cs` — minimal API endpoint (omitted when `--include-endpoint false`)

**Examples:**

Scaffold a GET (query) feature without an endpoint:

```bash
dotnet new feature -n GetOrder --projectName MyApi --operation GET --include-endpoint false
```

Scaffold a POST (create) feature with an endpoint:

```bash
dotnet new feature -n CreateOrder --projectName MyApi --operation POST
```

Scaffold a PUT (replace) feature:

```bash
dotnet new feature -n UpdateOrder --projectName MyApi --operation PUT
```

Scaffold a PATCH (partial update) feature:

```bash
dotnet new feature -n PatchOrderStatus --projectName MyApi --operation PATCH
```

Scaffold a DELETE feature:

```bash
dotnet new feature -n DeleteOrder --projectName MyApi --operation DELETE
```

The source generator automatically produces the concrete DTO records, command events, and mediator extension methods from the contract interfaces — do not write these manually.

### Building and Testing

```bash
dotnet build
dotnet test
```

---

## Architecture

This template is the reference implementation of the **Sliced Core Architecture**. For the full theoretical background — including why the architecture was designed this way, how CQRS is used as a layer contract, and what makes it resilient to AI-assisted development — read the article:

📄 **[docs/sliced-core-architecture-article.md](docs/sliced-core-architecture-article.md)**

The sections below show how the architecture manifests in actual code.

---

### File Layout

Every feature lives in `src/{YourProject}/Domains/{DomainName}/` and is split across up to four files:

```
Domains/
  {Domain}/               ← Organisational grouping only — no coupling between features
    Domain.cs                   ← Business and persistence model definitions
    {Feature}.Contracts.cs      ← Public request/response contracts + protected Core layer DTOs
    {Feature}.cs                ← Layer logic: Presentation, Application, Infrastructure
    {Feature}.Endpoint.cs       ← Minimal API endpoint registration (optional)
```

Layers communicate exclusively via the dispatcher:

```
Presentation  ──dispatcher──▶  Application  ──dispatcher──▶  Infrastructure
     │                              │                               │
  (keyed DI)                   (keyed DI)                     (keyed DI)
```

---

### Domain Models

Domain models and persistence entities are declared as private interfaces inside a `[Domain]`-annotated class. The source code generator reads the marker attributes at compile time and produces concrete classes, registering them automatically with `AppDbContext`.

```csharp
[Domain]
public class SampleDomain
{
    [BusinessModel]
    private interface ISample
    {
        int Id { get; set; }
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
    }

    [PersistenceModelConfiguration(typeof(ISampleEntity))]
    private static void ConfigureSampleEntity(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<ISampleEntity>();
        entity.ToTable("Samples");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Name).IsRequired().HasMaxLength(256);
    }
}
```

The `[BusinessModel]` interface becomes the domain model used inside business logic. The `[PersistenceModel]` interface becomes the EF Core entity. Both are `private` on the declaring `[Domain]` class — they cannot be referenced outside it.

---

### Feature Contracts

Every feature declares two public outer contracts — the inbound request shape and the outbound response shape visible to callers — plus a `protected sealed Core` class that holds all internal layer-to-layer DTO interfaces. The outer contracts are usable by endpoints and tests; the `Core` contracts are inaccessible outside the feature.

```csharp
public partial class CreateSample
{
    /// <summary>Inbound request contract exposed to callers (e.g. HTTP request body).</summary>
    public interface ICreateSampleRequest
    {
        [PiiData] string Name { get; set; }
        [PiiData] string Name2 { get; set; }
    }

    /// <summary>Outbound response contract returned to the caller on success.</summary>
    public interface ICreateSampleResponse
    {
        int Id { get; set; }
        string Name { get; set; }
        string Name2 { get; set; }
    }

    protected sealed partial class Core : Dependencies.Core
    {
        // Application layer inbound DTO — forwarded from Presentation
        public interface IApplicationRequestDTO
        {
            [PiiData] string Name { get; set; }
            [PublicData] string Name2 { get; set; }
        }

        // Application layer outbound DTO — returned to Presentation
        public interface IApplicationResponseDTO { ... }

        // Infrastructure layer inbound DTO — forwarded from Application for persistence
        public interface IPersistenceRequestDTO { ... }

        // Infrastructure layer outbound DTO — returned after the entity is persisted
        public interface IPersistenceResponseDTO { ... }
    }
}
```

`ICreateSampleRequest` and `ICreateSampleResponse` are the only contracts that cross the feature boundary — usable by endpoints, mediator dispatch, and tests. Everything inside `Core` is inaccessible outside the feature. The source code generator produces all concrete record implementations — you never write these manually.

---

### Layer Logic — Command Feature

Features are classified as `FeatureType.Command` or `FeatureType.Query`. This determines the CQRS event type used at every layer boundary and controls which dispatch surfaces are available.

> `Sample` and `SampleEntity` in the examples below are the concrete classes produced by the source generator from the `[BusinessModel]` and `[PersistenceModel]` interfaces in `Domain.cs`. You never write them manually.

```csharp
[Feature(FeatureType.Command)]
public partial class CreateSample
{
    partial class PresentationLayer
    {
        private async Task<Result<ICreateSampleResponse>> PresentationLogic(
            ICreateSampleRequest request, CancellationToken cancellationToken)
        {
            // Validate input, map to inner Core DTO, then forward to Application
            var appResponse = await ForwardToApplicationLayer(
                request.Adapt<Core.IApplicationRequestDTO>(), cancellationToken);

            if (appResponse.IsError())
                return Result.Error(new ErrorList(appResponse.Errors));

            return Result.Created((ICreateSampleResponse)appResponse.Value.Adapt<CreateSampleResponse>());
        }
    }

    partial class ApplicationLayer
    {
        private async Task<Result<Core.IApplicationResponseDTO>> ApplicationLogic(
            Sample sample, CancellationToken cancellationToken)
        {
            // Business validation runs here against the domain model
            // Then forward the write intent to Infrastructure
            var persistenceResponse = await ForwardToInfrastructureLayer(
                sample.Adapt<Core.IPersistenceRequestDTO>(), cancellationToken);

            if (persistenceResponse.IsError())
                return Result.Error(new ErrorList(persistenceResponse.Errors));

            return Result.Created((Core.IApplicationResponseDTO)persistenceResponse.Value.Adapt<ApplicationResponseDTO>());
        }
    }

    partial class InfrastructureLayer
    {
        private async Task<Result<Core.IPersistenceResponseDTO>> InfrastructureLogic(
            SampleEntity entity, CancellationToken cancellationToken)
        {
            // Persistence — the only layer that touches the database directly
            var dbContext = GetRequiredDbContext<AppDbContext>();
            entity.CreatedAt = DateTime.UtcNow;
            dbContext.Set<SampleEntity>().Add(entity);
            await dbContext.SaveChangesAsync(cancellationToken);

            return Result.Created((Core.IPersistenceResponseDTO)entity.Adapt<PersistenceResponseDTO>());
        }
    }
}
```

`ForwardToApplicationLayer` and `ForwardToInfrastructureLayer` are generated by the source code generator. They dispatch through the mediator using the correct typed event for this feature's classification. Neither can be called across feature boundaries — Presentation has no cross-feature dispatch surface at all.

---

### Layer Logic — Query Feature

Query features follow the same three-layer structure but are classified as `FeatureType.Query`. Every dispatcher event through the layers is typed as a query, not a command. The infrastructure layer of a query feature cannot issue cross-feature commands — the compiler enforces this.

```csharp
[Feature(FeatureType.Query)]
public partial class GetSample
{
    partial class PresentationLayer
    {
        private async Task<Result<IGetSampleResponse>> PresentationLogic(
            IGetSampleRequest request, CancellationToken cancellationToken)
        {
            var appResponse = await ForwardToApplicationLayer(
                request.Adapt<Core.IApplicationRequestDTO>(), cancellationToken);

            if (appResponse.IsError())
                return Result.Error(new ErrorList(appResponse.Errors));

            return Result.Success((IGetSampleResponse)appResponse.Value.Adapt<GetSampleResponse>());
        }
    }

    partial class ApplicationLayer
    {
        private async Task<Result<Core.IApplicationResponseDTO>> ApplicationLogic(
            Sample sample, CancellationToken cancellationToken)
        {
            var persistenceResponse = await ForwardToInfrastructureLayer(
                sample.Adapt<Core.IPersistenceRequestDTO>(), cancellationToken);

            if (persistenceResponse.IsError())
                return Result.Error(new ErrorList(persistenceResponse.Errors));

            if (persistenceResponse.Value is null)
                return Result.NotFound();

            return Result.Success((Core.IApplicationResponseDTO)persistenceResponse.Value.Adapt<ApplicationResponseDTO>());
        }
    }

    partial class InfrastructureLayer
    {
        private async Task<Result<Core.IPersistenceResponseDTO>> InfrastructureLogic(
            SampleEntity entity, CancellationToken cancellationToken)
        {
            var dbContext = GetRequiredDbContext<AppDbContext>();
            var found = await dbContext.Set<SampleEntity>()
                .FindAsync(new object[] { entity.Id }, cancellationToken);

            if (found is null)
                return Result.NotFound();

            return Result.Success((Core.IPersistenceResponseDTO)found.Adapt<PersistenceResponseDTO>());
        }
    }
}
```

---

### Endpoints and Dispatcher

Endpoints live inside `PresentationLayer` and are the entry point into a feature's pipeline. The dispatcher call style differs between command and query features.

**Command feature** — the endpoint receives the public `ICreateSampleRequest` directly and passes it straight to the mediator:

```csharp
builder.MapPost("/sample", async (ICreateSampleRequest request, IMediator mediator, CancellationToken cancellationToken) =>
{
    return (await mediator.CreateSample(request, cancellationToken)).ToMinimalApiResult();
})
    .Produces<ICreateSampleResponse>(StatusCodes.Status201Created);
```

**Query feature** — uses the fluent builder generated per-feature by the source code generator:

```csharp
builder.MapGet("/sample/{id:int}", async (int id, IMediator mediator, CancellationToken cancellationToken) =>
{
    return (await mediator.GetSample().WithId(id).Send(cancellationToken)).ToMinimalApiResult();
})
    .Produces<IGetSampleResponse>(StatusCodes.Status200OK);
```

The fluent builder (`GetSample().WithId(...).Send(...)`) is generated at compile time. It is only available on the correct dispatcher surface for the feature's type — a query builder cannot be used to issue a command, and vice versa.

---

### Worker Jobs (ProjectTemplate.Worker)

`ProjectTemplate.Worker` is a companion background-worker project that shares the same Sliced Core layer model. It uses [TickerQ](https://github.com/TickerQ/TickerQ) for cron-based job scheduling and calls back into the API via the generated `ProjectTemplateClient` (Refit client). The worker has no HTTP listener — it runs as a .NET Generic Host.

#### File layout

Each worker feature adds one extra file alongside the standard contracts and layer-logic files:

```
Domains/
  Sample/
    Domain.cs                     # business model + persistence model (identical to API pattern)
    CreateSample.Contracts.cs     # ICreateSampleRequest / ICreateSampleResponse + Core DTOs
    CreateSample.cs               # PresentationLayer / ApplicationLayer / InfrastructureLayer logic
    CreateSample.Job.cs           # TickerQ job class + RegisterJob helper
```

#### Job file (`CreateSample.Job.cs`)

The job class lives inside `PresentationLayer` and implements `ITickerFunction`. The static `RegisterJob` helper registers the cron schedule:

```csharp
partial class PresentationLayer
{
    internal sealed class CreateSampleJob(ILogger<CreateSampleJob> logger, IMediator mediator) : ITickerFunction
    {
        public async Task ExecuteAsync(TickerFunctionContext context, CancellationToken cancellationToken = default)
        {
            var result = await mediator.CreateSample()
                .WithId(1)
                .Send(cancellationToken);

            if (result.IsError())
                throw new InvalidOperationException(string.Join(", ", result.Errors));
        }
    }
}

internal static IServiceCollection RegisterJob(IServiceCollection services)
{
    services.MapTickerGroup("Sample", group =>
    {
        group.MapTicker<PresentationLayer.CreateSampleJob>("CreateSample")
            .WithCron("0 * * * * *")
            .WithMaxConcurrency(1);
    });
    return services;
}
```

Call `RegisterJob` from `Program.Services.cs` to activate the schedule.

#### Layer logic differences

- **ApplicationLayer** — calls the typed Refit client (`ProjectTemplateClient`) to read data from the API.
- **InfrastructureLayer** — calls the typed Refit client to write data back through the API; does not access a database directly.
- The worker's `AppDbContext` is present for the source generator's EF Core model registration, but typical worker features delegate persistence to the API rather than writing to the database directly.

#### Worker configuration

| Section | Key | Description |
|---|---|---|
| `ProjectTemplateClient` | `BaseUri` | Base URI of the API that this worker calls (e.g., `https://localhost:5001/api/v1`). |
| `HmacRedactorOptions` | `Key` | Base-64 HMAC key for PII redaction (same as the API project). |
| `HmacRedactorOptions` | `KeyId` | Numeric key identifier for key rotation. |

#### Adding a new worker feature

Follow the same steps as adding an API feature (see [Creating a New Feature](#creating-a-new-feature)), then add a `{Feature}.Job.cs` file that wires the TickerQ cron schedule.

---

### What the Source Code Generator Produces

From the contract interfaces you declare, the source code generator produces at compile time:

| From | Generator produces |
|---|---|
| `ICreateSampleRequest`, `ICreateSampleResponse` (outer public contracts) | Concrete `record` implementations of the public request/response contracts |
| `Core.IApplicationRequestDTO`, `Core.IPersistenceRequestDTO`, etc. | Concrete `record` implementations for every inner Core DTO interface |
| `[Feature(FeatureType.Command)]` | `ICreateSampleEventPresentationLayer` and `ICreateSampleEventApplicationLayer` command event interfaces and their concrete event classes |
| `[Feature(FeatureType.Query)]` | `IGetSampleQueryEventPresentationLayer` and `IGetSampleQueryEventApplicationLayer` query event interfaces and their concrete event classes |
| Feature classification | `ForwardToApplicationLayer` and `ForwardToInfrastructureLayer` partial methods wired to the correct CQRS event type |
| Command feature | `mediator.CreateSample(request, ct)` extension on `IMediator` and `Commands` dispatch surfaces |
| Query feature | Fluent builder extension methods (`mediator.GetSample().WithId(...).Send(...)`) on the `Queries` dispatch surface |
| `[Domain]`, `[PersistenceModel]`, `[PersistenceModelConfiguration]` | Concrete entity classes and EF Core `ModelBuilder` registration in `AppDbContext` |

None of this is written manually. Attempting to write any of it manually will produce a compile error — the generator owns these types.

See [ARCHITECTURE.md](ARCHITECTURE.md) for full documentation of the source code generator internals, keyed DI mechanics, and how to extend the generator.

---

### Dependency Registration

Services are registered in `src/{YourProject}/Program.Dependencies.cs`. Each layer has a dedicated static method that receives a typed builder — never call `IServiceCollection` directly:

```csharp
private static void AddPresentationLayerDependencies(PresentationLayerBuilder builder)
{
    builder.AddTransient<IPresentationService, PresentationService>();
}

private static void AddApplicationLayerDependencies(ApplicationLayerBuilder builder)
{
    builder.AddScoped<IApplicationService, ApplicationService>();
}

private static void AddInfrastructureLayerDependencies(InfrastructureLayerBuilder builder)
{
    builder.AddScoped<IMyRepository, MyRepository>();

    // EF Core DbContext registration — also uses the typed builder:
    builder.AddDbContext<AppDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
}

private static void AddCoreLayerDependencies(CoreLayerBuilder builder)
{
    builder.AddTransient<IDomainService, DomainService>();
}
```

Each builder resolves only its own keyed services at runtime. A service registered via `InfrastructureLayerBuilder` is invisible to `PresentationLayerBuilder` and vice versa — this is the compile-time isolation in action.

---

### Configuration

All runtime configuration lives in `appsettings.json`. Override per environment in `appsettings.{Environment}.json`. Never commit real secrets — use `appsettings.Development.json` (git-ignored) or environment variables.

| Section | Key | Description |
|---|---|---|
| `ConnectionStrings` | `DefaultConnection` | EF Core SQL Server connection string |
| `JwtBearer` | `Authority` | Identity provider issuer URL |
| `JwtBearer` | `Audience` | API audience identifier |
| `JwtBearer` | `RequireHttpsMetadata` | `true` in production; `false` for local HTTP IdPs |
| `Cors` | `AllowedOrigins` | Array of permitted origins |
| `Cors` | `AllowedMethods` | Array of permitted HTTP methods |
| `Cors` | `AllowedHeaders` | Array of permitted request headers |
| `Cors` | `AllowCredentials` | `true` / `false` |
| `HmacRedactorOptions` | `Key` | Base-64 HMAC key used by PII redaction. Generate with `openssl rand -base64 32`. |
| `HmacRedactorOptions` | `KeyId` | Numeric key identifier for key rotation. |
| `Resilience` | `Retry.MaxRetryAttempts` | Maximum number of retry attempts (default: `4`). |
| `Resilience` | `Retry.DelaySeconds` | Base delay in seconds between retries (default: `2`). |
| `Resilience` | `Retry.UseJitter` | Add random jitter to retry delay to avoid thundering herds (default: `true`). |
| `Resilience` | `Retry.BackoffType` | `Exponential` (default), `Linear`, or `Constant`. |
| `Resilience` | `CircuitBreaker.FailureRatio` | Failure ratio 0–1 that trips the circuit (default: `0.5`). |
| `Resilience` | `CircuitBreaker.SamplingDurationSeconds` | Sampling window in seconds (default: `30`). |
| `Resilience` | `CircuitBreaker.MinimumThroughput` | Minimum requests before circuit can trip (default: `10`). |
| `Resilience` | `CircuitBreaker.BreakDurationSeconds` | Duration in seconds the circuit stays open (default: `15`). |
| `Resilience` | `Timeout.AttemptTimeoutSeconds` | Per-attempt timeout in seconds (default: `10`). |
| `RateLimiting` | `Global.PermitLimit` | Max requests per identity per window for the global baseline (default: `100`). |
| `RateLimiting` | `Global.WindowSeconds` | Global window size in seconds (default: `60`). |
| `RateLimiting` | `Global.QueueLimit` | Global queue depth; `0` = reject immediately (default: `10`). |
| `RateLimiting` | `Fixed.PermitLimit` | Max requests for the `fixed` named policy (default: `60`). |
| `RateLimiting` | `Fixed.WindowSeconds` | Window size in seconds for `fixed` (default: `60`). |
| `RateLimiting` | `Fixed.QueueLimit` | Queue depth for `fixed`; `0` = reject immediately (default: `5`). |
| `RateLimiting` | `Sliding.PermitLimit` | Max requests for the `sliding` named policy (default: `60`). |
| `RateLimiting` | `Sliding.WindowSeconds` | Window size in seconds for `sliding` (default: `60`). |
| `RateLimiting` | `Sliding.SegmentsPerWindow` | Number of sliding segments (default: `6`). |
| `RateLimiting` | `Sliding.QueueLimit` | Queue depth for `sliding` (default: `5`). |
| `RateLimiting` | `Token.TokenLimit` | Maximum bucket capacity for the `token` named policy (default: `100`). |
| `RateLimiting` | `Token.TokensPerPeriod` | Tokens added each replenishment period for `token` (default: `20`). |
| `RateLimiting` | `Token.ReplenishmentPeriodSeconds` | Replenishment interval in seconds for `token` (default: `10`). |
| `RateLimiting` | `Token.QueueLimit` | Queue depth for `token` (default: `10`). |
| `RateLimiting` | `Strict.PermitLimit` | Max requests for the `strict` named policy (default: `10`). |
| `RateLimiting` | `Strict.WindowSeconds` | Window size in seconds for `strict` (default: `60`). |
| `RateLimiting` | `Strict.QueueLimit` | Queue depth for `strict`; `0` = reject immediately (default: `0`). |

By default, the template keeps endpoint/port selection in environment/template configuration:

- Local run profiles are defined in `src/ProjectTemplate/Properties/launchSettings.json` (for example, `https://localhost:7199;http://localhost:5242`).
- The generated `ProjectTemplate.http` file starts with `@ProjectTemplate_HostAddress = http://localhost:5000` and can be adjusted per environment.
- Kestrel hosting code does **not** hard-code ports/listeners.

For HTTPS endpoints, `ConfigureHosting` configures:

- `Http1AndHttp2AndHttp3` protocol negotiation enabled
- Automatic fallback to HTTP/2 or HTTP/1.1 when HTTP/3 is unavailable

This protocol configuration is applied in `Program.Services.cs` via `ConfigureHosting`.

---

### JSON Serialization

The template configures opinionated `System.Text.Json` defaults for all minimal-API requests and responses in `Program.Services.cs`.

| Setting | Value | Benefit |
|---|---|---|
| `PropertyNamingPolicy` | `CamelCase` | Consistent camelCase property names in all JSON responses |
| `DefaultIgnoreCondition` | `WhenWritingNull` | Reduces payload size by omitting properties with `null` values |
| `PropertyNameCaseInsensitive` | `true` | Accepts JSON request bodies with any property casing |

These defaults are applied via `ConfigureHttpJsonOptions` and affect all minimal-API endpoints. To override a setting for a specific endpoint, return `Results.Json(value, options)` / `TypedResults.Json(value, options)` with a custom `JsonSerializerOptions` instance.

---

### Health Checks

The template registers ASP.NET Core health check middleware and exposes three endpoints:

| Endpoint | Purpose |
|---|---|
| `GET /healthz` | Overall status — aggregates every registered check |
| `GET /healthz/live` | Liveness — always `200 OK` while the host is running; skips all dependency checks |
| `GET /healthz/ready` | Readiness — only checks tagged `"ready"` must pass before traffic is routed |

Dependency-specific checks are added in `Program.Services.cs` by chaining extension methods on the `IHealthChecksBuilder` returned by `AddHealthChecks()`.

**SQL Server check** (requires [`AspNetCore.HealthChecks.SqlServer`](https://www.nuget.org/packages/AspNetCore.HealthChecks.SqlServer)):

```csharp
builder.Services.AddHealthChecks()
    .AddSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")!,
        name: "sql",
        tags: ["ready"]);
```

**EF Core DbContext check** (requires [`Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore`](https://www.nuget.org/packages/Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore)):

```csharp
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>(name: "db", tags: ["ready"]);
```

Tag a check with `"ready"` to include it in the `/healthz/ready` probe. The `/healthz/live` probe always returns `200 OK` regardless of dependency checks — it is purely a host-is-alive signal used by Kubernetes liveness probes.

**Kubernetes probe configuration example:**

```yaml
livenessProbe:
  httpGet:
    path: /healthz/live
    port: 8080
  initialDelaySeconds: 5
  periodSeconds: 10
readinessProbe:
  httpGet:
    path: /healthz/ready
    port: 8080
  initialDelaySeconds: 10
  periodSeconds: 15
```

**`appsettings.Development.json` example** (copy from `appsettings.Development.json.example`):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=MyApiDev;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "HmacRedactorOptions": {
    "KeyId": 1,
    "Key": "<generate with: openssl rand -base64 32>"
  }
}
```

JWT and CORS fall back to the defaults in `appsettings.json`. Override them in `appsettings.Development.json` if your local IdP differs.

---

### Rate Limiting

The template registers `Microsoft.AspNetCore.RateLimiting` middleware via `AddSlicedCoreRateLimiting` in `Program.Services.cs`. Rate limiting is applied **after** authentication and authorization so the global partitioner can use the authenticated user identity as a partition key.

#### Global limiter

Every request passes through a global fixed-window limiter before reaching any named policy. Traffic is partitioned by:

1. Authenticated user identity (`HttpContext.User.Identity.Name`)
2. Remote IP address
3. Fall-through key `anonymous`

#### Named policies

Apply a named policy to any minimal-API endpoint:

```csharp
app.MapPost("/api/login", handler)
   .RequireRateLimiting(RateLimitingPolicies.Strict);

app.MapGet("/api/data", handler)
   .RequireRateLimiting(RateLimitingPolicies.Sliding);
```

Or via attribute on an endpoint class:

```csharp
[EnableRateLimiting(RateLimitingPolicies.Token)]
public class MyEndpoint { }
```

Opt out for internal or system endpoints:

```csharp
app.MapGet("/internal/status", handler)
   .DisableRateLimiting();
```

| Policy | Strategy | Default limit | Intended use |
|---|---|---|---|
| `fixed` | Fixed window | 60 req / 60 s | General traffic control |
| `sliding` | Sliding window | 60 req / 60 s, 6 segments | Fair distribution under sustained load |
| `token` | Token bucket | 100 tokens, +20 / 10 s | Burst-friendly APIs |
| `strict` | Fixed window | 10 req / 60 s, no queue | Authentication / login endpoints |

All limits are configurable via the `RateLimiting` section of `appsettings.json` — see the [Configuration](#configuration) table above for all keys. Exceeding any limit returns **HTTP 429 Too Many Requests**.

---

## Documentation

- **[ARCHITECTURE.md](ARCHITECTURE.md)** — layer model, source generators, domain marker pattern, framework package, item templates
- **[CONTRIBUTING.md](CONTRIBUTING.md)** — setup, branch conventions, working with generators and templates, PR process
- **[.github/PULL_REQUEST_TEMPLATE.md](.github/PULL_REQUEST_TEMPLATE.md)** — PR checklist
- **[.github/ISSUE_TEMPLATE/](.github/ISSUE_TEMPLATE/)** — bug report and feature request templates

---

## Contributing

Contributions are welcome! Please read [CONTRIBUTING.md](CONTRIBUTING.md) before opening a PR.

---

## Acknowledgments

Built with:
- [.NET 10](https://dotnet.microsoft.com/)
- [Roslyn Source Generators](https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/source-generators-overview)
- [EF Core](https://learn.microsoft.com/en-us/ef/core/)
- [FluentValidation](https://fluentvalidation.net/)
- [Mapster](https://github.com/MapsterMapper/Mapster)
- [Cortex.Mediator](https://github.com/buildersoftio/cortex)
- [Ardalis.Result](https://github.com/ardalis/Result)
- [TUnit](https://github.com/thomhurst/TUnit)
- [Scalar](https://scalar.com/)

---

## License

[MIT](LICENSE)
