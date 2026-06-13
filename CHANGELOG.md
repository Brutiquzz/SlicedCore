# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [Unreleased]

### Added
- `--operation` parameter (`GET` / `POST` / `PUT` / `PATCH` / `DELETE`) on the `feature` item
  template — explicitly declares the intended HTTP operation for the feature. The selected
  operation determines the endpoint HTTP method, route pattern, status codes, and CQRS feature
  type (`GET` → query; all others → command). Defaults to `POST`.
- `isGet`, `isPost`, `isPut`, `isPatch`, `isDelete` computed template symbols — used in
  `CreateSample.Endpoint.cs` to emit the correct `MapGet` / `MapPost` / `MapPut` /
  `MapPatch` / `MapDelete` call with the appropriate route pattern and response declarations.
- `CHANGELOG.md` to track changes between releases.
- `GenerateHttpFileGenerator` source generator that auto-maintains `ProjectTemplate.http`
  by scanning `*FeatureEndpoint` types and emitting a valid C# wrapper; an MSBuild inline
  task then writes the actual `.http` file to disk after each build.
- Opinionated `System.Text.Json` defaults configured via `ConfigureHttpJsonOptions` in
  `Program.Services.cs`: camelCase property naming, null-value omission, and
  case-insensitive deserialization.
- `ProjectTemplate.Generators.Tests` project with Roslyn in-memory test harness covering
  all source generators (`GenerateCoreBoilerplate`, `GenerateExtensionsBoilerplate`,
  `GeneratePresentationBoilerplate`, `GenerateDbContext`,
  `GenerateServiceProviderConstructor`, `GenerateHttpFile`).
- Docker-based sandbox for template smoke tests (`tests/Dockerfile`,
  `tests/Run-InDocker.ps1`) to keep `dotnet new` installs isolated from the host.
- `generator-tests` job in CI (`ci.yml`) to run the new generator test project.
- `Directory.Build.props` and `Directory.Packages.props` for central build and package
  version management.
- `.editorconfig` with common C# and general code-style rules.
- `LICENSE` (MIT).
- `--featureType` parameter (`command` / `query`) on the `feature` item template — controls
  the CQRS event type, generated infrastructure logic, and endpoint HTTP method.
- `--projectName` parameter on both `feature` and `domain` item templates — sets the root
  namespace to match the target project's assembly name.
- `CreateSampleTests.Handler.cs` and `CreateSampleTests.Integration.cs` partial test files
  splitting handler-level and HTTP endpoint integration tests.

### Changed
- **`--featureType` replaced by `--operation`** on the `feature` item template — the new
  `--operation GET|POST|PUT|PATCH|DELETE` parameter supersedes the previous `command`/`query`
  choice. The CQRS feature type is now derived automatically from the operation
  (`GET` → query; `POST`/`PUT`/`PATCH`/`DELETE` → command). The `isCommand` and `isQuery`
  template symbols are preserved for internal template conditions.
- **Feature contract model refactored**: `ICreateSampleRequest` and `ICreateSampleResponse`
  (and their equivalents for all features) are now **public outer contracts** on the feature
  partial class, replacing the previous `ICreateSampleRequestDTO` / `ICreateSampleResponseDTO`
  types that were nested inside `Core`. The `Core` class is now `protected sealed`, keeping
  all internal layer DTOs (`IApplicationRequestDTO`, `IPersistenceRequestDTO`, etc.)
  inaccessible outside the feature.
- Endpoints now receive `ICreateSampleRequest` directly from the HTTP binding and pass it
  straight to `mediator.CreateSample(request, ct)` — no `.Adapt<ICreateSampleRequestDTO>()`
  call required at the endpoint level.
- Generated `Handle(...)` methods on all layer handlers changed to explicit interface
  implementations to prevent accidental public exposure of protected `Core` types.
- `ForwardToApplicationLayer` and `ForwardToInfrastructureLayer` generated helpers changed
  to `private` to avoid accessibility conflicts with protected `Core` contracts.
- Framework base classes (`Core`, `Presentation`, `Application`, `Infrastructure`) are now
  decorated with `[EditorBrowsable(EditorBrowsableState.Never)]` to reduce IntelliSense
  noise for consumers.
- Root `/.template.config/template.json` exclusions updated to cover `.template/**`,
  `Generated/**`, and `*.http` so those paths are never emitted to scaffolded projects.
- CI separated into distinct `build-and-test`, `template-tests`, and `generator-tests`
  jobs; template smoke tests now run inside the Docker sandbox.

### Removed
- `src/ProjectTemplate.Tests/GlobalSetup.cs` — contained only a redundant
  `[assembly: ExcludeFromCodeCoverage]` attribute that suppressed coverage on the test
  assembly.
- Dead private helper methods (`AddLayeredTransient`, `AddLayeredScoped`,
  `AddLayeredSingleton` and their single-implementation overloads) from
  `ServiceCollectionLayerExtensions`.
- `ICreateSampleRequestDTO` and `ICreateSampleResponseDTO` (and their per-feature
  equivalents) — superseded by the public outer `ICreateSampleRequest` /
  `ICreateSampleResponse` contracts.

---

## [0.1.0] — Initial release

### Added
- `ProjectTemplate` web application template with layered architecture
  (Presentation / Application / Infrastructure / Core).
- `ProjectTemplate.Framework` package with base classes, keyed-DI extensions, and
  layer-handler scanner.
- `ProjectTemplate.Generators` source generators for core boilerplate, layer models,
  extensions, presentation boilerplate, DbContext wiring, and service-provider
  constructors.
- Item templates for scaffolding new domains and features
  (`domain` and `feature` templates under `src/ProjectTemplate/Domains/.template`).
- Scalar API documentation endpoint.
- FluentValidation, Mapster, Ardalis.Result, and Cortex.Mediator integrations.
- Entity Framework Core with SQL Server provider.
- API versioning via `Asp.Versioning.Http`.
- Microsoft Extensions telemetry and HmacRedactor compliance.

[Unreleased]: https://github.com/Brutiquzz/Templates/compare/v0.1.0...HEAD
[0.1.0]: https://github.com/Brutiquzz/Templates/releases/tag/v0.1.0
