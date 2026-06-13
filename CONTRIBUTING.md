# Contributing to SlicedCore

Thank you for your interest in contributing! This document provides guidelines and instructions for contributing to the repository.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Git](https://git-scm.com/)
- A code editor (e.g., Visual Studio 2022 or later, Visual Studio Code, or Rider)

## Getting Started

1. **Fork and clone** the repository:
   ```bash
   git clone https://github.com/Brutiquzz/Templates.git
   cd Templates
   ```

2. **Restore and build**:
   ```bash
   dotnet restore
   dotnet build
   ```

3. **Run tests**:
   ```bash
   dotnet test
   ```

## Branch Naming Convention

Use the following prefixes:
- `feature/` — new features or enhancements
- `fix/` — bug fixes
- `docs/` — documentation updates
- `refactor/` — code refactoring without behavioral changes

Example: `feature/add-pagination-support`

## Commit Message Convention

Follow [Conventional Commits](https://www.conventionalcommits.org/):

- `feat: add new domain template parameter`
- `fix: resolve generator null reference in multi-entity config`
- `docs: update ARCHITECTURE.md with new layer pattern`
- `refactor: extract common DI helper to framework project`
- `test: add integration tests for feature template`

## Working with Source Generators

The repository uses Roslyn incremental generators in `src\ProjectTemplate.Generators`:

- **`GenerateLayerModelsGenerator.cs`** — generates layer-local private model copies and EF Core configuration
- **`GenerateDbContextGenerator.cs`** — generates `AppDbContext` partial class
- **`GenerateApplicationBoilerplateGenerator.cs`, `GeneratePresentationBoilerplateGenerator.cs`, `GenerateInfrastructureBoilerplateGenerator.cs`, `GenerateCoreBoilerplateGenerator.cs`** — generate layer handlers, events, and DTOs

### Viewing Generated Output

Generated files are emitted to `src\ProjectTemplate\Generated\` for inspection (not compiled directly):

```xml
<PropertyGroup>
	<EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
	<CompilerGeneratedFilesOutputPath>Generated</CompilerGeneratedFilesOutputPath>
</PropertyGroup>
```

Always review changes in the `Generated\` folder when modifying generators.

## Working with Item Templates

The repository contains two item templates in `src\ProjectTemplate\Domains\.template\`:

### **Feature template** (`feature`)
- **Location**: `Domains/.template/feature/.template.config/template.json`
- **Source files**: `Domains/Sample/CreateSample.cs`, `CreateSample.Contracts.cs`, `CreateSample.Endpoint.cs`
- **Parameters**:
  - `--name` / `-n` — feature name (e.g. `GetOrder`); replaces `CreateSample` in all file names and symbols
  - `--projectName` — root namespace / assembly name of the target project (defaults to `ProjectTemplate`)
  - `--operation` — `GET`, `POST` (default), `PUT`, `PATCH`, or `DELETE`; determines the endpoint HTTP method, route pattern, status codes, and CQRS feature type (`GET` → query; all others → command)
  - `--include-endpoint` — `true` (default) or `false`; omit the `.Endpoint.cs` file when `false`
- **Usage**: `dotnet new feature -n GetSample --operation GET [--include-endpoint false]`

### **Domain template** (`domain`)
- **Location**: `Domains/.template/domain/.template.config/template.json`
- **Source file**: `Domains/Sample/Domain.cs`
- **Parameters**:
  - `--name` / `-n` — domain name (e.g. `Order`); replaces `Sample` in all symbols
  - `--projectName` — root namespace / assembly name of the target project (defaults to `ProjectTemplate`)
- **Usage**: `dotnet new domain -n Order`

### Testing Item Templates Locally

1. **Install the templates**:
   ```bash
   dotnet new install .\src\ProjectTemplate\Domains\.template\feature
   dotnet new install .\src\ProjectTemplate\Domains\.template\domain
   ```

2. **Create a test directory**:
   ```bash
   mkdir TestFeature
   cd TestFeature
   ```

3. **Instantiate a template**:
   ```bash
   dotnet new feature -n TestGetSample --operation GET
   ```

4. **Verify output** and ensure all placeholders (e.g., `CreateSample`) are replaced correctly.

5. **Uninstall after testing**:
   ```bash
   dotnet new uninstall Brutiquzz.Templates.Feature
   dotnet new uninstall Brutiquzz.Templates.Domain
   ```

### Keeping Templates in Sync

The item templates **reference** the actual source files in `Domains/Sample/` via the `source` property in `template.json`. **Do not duplicate files** — the templates automatically pick up changes to the source files.

If you modify:
- `Domains/Sample/CreateSample.cs`
- `Domains/Sample/CreateSample.Contracts.cs`
- `Domains/Sample/CreateSample.Endpoint.cs`
- `Domains/Sample/Domain.cs`

...the item templates will reflect those changes immediately on the next install.

## Adding a New Domain

1. Create a new folder under `src\ProjectTemplate\Domains\` (e.g., `Order`)
2. Add a `Domain.cs` file with the `[Domain]` attribute
3. Define business and persistence interfaces using `[BusinessModel]` and `[PersistenceModel]`
4. Add configuration methods tagged with `[PersistenceModelConfiguration(typeof(IYourEntity))]`
5. Build the solution to trigger generator output

## Adding a New Feature

1. From within the domain folder (e.g., `src\ProjectTemplate\Domains\Order`), run:
   ```bash
   dotnet new feature -n CreateOrder --projectName MyProject --operation POST
   ```
2. The item template emits three files: `CreateOrder.Contracts.cs`, `CreateOrder.cs`, and `CreateOrder.Endpoint.cs`
3. Implement business logic in `ApplicationLayer` and persistence logic in `InfrastructureLayer` — the generators emit all layer handlers, DTOs, and boilerplate automatically
4. Use the existing `CreateSample` files as a reference if you need to understand the expected structure

## Pull Request Process

1. Ensure your branch is up to date with `main`
2. Run `dotnet build` and `dotnet test` — all must pass
3. Review generated output in `Generated/` folder if generators were modified
4. Update `README.md` or `ARCHITECTURE.md` if user-facing behavior changed
5. Open a PR and fill out the PR template checklist
6. Wait for CI to pass and address any review feedback

## Code Style

- Follow existing conventions in the codebase
- XML doc summaries (`/// <summary>`) are required on all public types and members
- Nullable reference types are enabled — never use `!` (null-forgiving) unless unavoidable
- Use `latest` C# language version — prefer records, pattern matching, collection expressions, and primary constructors
- `TreatWarningsAsErrors` is enabled — all code must build warning-free
- Keep layer encapsulation strict — no direct access to `IServiceProvider` in user code; use the layer base class helpers
- Each feature's `Core` class is `protected sealed` — never reference another feature's internal layer DTOs (`IApplicationRequestDTO`, `IPersistenceRequestDTO`, etc.) from outside that feature
- Public outer request/response contracts (`ICreateSampleRequest`, `ICreateSampleResponse`) are intentionally visible across the domain, but should not be depended upon by other features — each feature owns its own contracts end to end

## Questions?

Open a [discussion](https://github.com/Brutiquzz/Templates/discussions) or file an [issue](https://github.com/Brutiquzz/Templates/issues).
