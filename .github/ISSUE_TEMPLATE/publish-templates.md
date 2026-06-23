---
name: Publish SlicedCore Templates to NuGet
about: Track publishing of SlicedCore templates as a NuGet package
title: Publish SlicedCore Templates to NuGet Package
---

## Problem
The SlicedCore templates (project template + item templates for features/domains) are fully functional but only available locally by cloning the repository. Developers can't use them via the standard `dotnet new` flow.

## Current State
- Solution template: `.template.config/template.json` in repository root
- Feature item template: `src/ProjectTemplate/Domains/.template/feature/`
- Domain item template: `src/ProjectTemplate/Domains/.template/domain/`
- Installation: Manual clone + `dotnet new install ./`

## Desired State
Developers should be able to install templates with a single command:

```bash
dotnet new install SlicedCore.Templates
```

And use them like:
```bash
# Create new API project
dotnet new my-web -n MyApi

# Add a feature
dotnet new feature -n CreateOrder --projectName MyApi --operation POST

# Add a domain
dotnet new domain -n Order --projectName MyApi
```

## Solution
Package and publish templates to **NuGet.org** as `SlicedCore.Templates`

### Tasks

1. **Create NuGet package structure**
   - [ ] Set up `.nuspec` or use project file for packaging
   - [ ] Include all three template directories:
     - Root solution template (`.template.config/`)
     - Feature item template (`src/ProjectTemplate/Domains/.template/feature/`)
     - Domain item template (`src/ProjectTemplate/Domains/.template/domain/`)
   - [ ] Exclude unnecessary files (bin, obj, .git, etc.)

2. **Package Configuration**
   - [ ] Set package ID: `SlicedCore.Templates`
   - [ ] Define versioning strategy (align with repository releases)
   - [ ] Add package metadata (description, author, repository link)
   - [ ] Include comprehensive README with:
     - Installation instructions
     - Usage examples for each template
     - Parameter documentation
     - Link to architecture documentation

3. **Publish to NuGet.org**
   - [ ] Create NuGet.org account / API key setup
   - [ ] Test package locally: `dotnet new install SlicedCore.Templates`
   - [ ] Publish to NuGet.org

4. **CI/CD Integration**
   - [ ] Add workflow to auto-publish on repository releases
   - [ ] Ensure version in package matches Git tag

5. **Documentation**
   - [ ] Update main README with installation command:
     ```bash
     dotnet new install SlicedCore.Templates
     ```
   - [ ] Add "Quick Start" section linking to examples
   - [ ] Document template parameters and usage

## Acceptance Criteria
- [ ] NuGet package `SlicedCore.Templates` created and published to NuGet.org
- [ ] `dotnet new install SlicedCore.Templates` command works for public users
- [ ] All three templates functional after installation
- [ ] README updated with installation command and usage examples
- [ ] Release notes documented for each template version
- [ ] CI/CD pipeline configured for automated publishing on releases

## References
- [.NET Custom Templates Documentation](https://learn.microsoft.com/en-us/dotnet/core/tools/custom-templates)
- [NuGet Package Publishing Guide](https://learn.microsoft.com/en-us/nuget/quickstart/create-and-publish-a-package-using-the-dotnet-cli)
- [template.json Reference](https://github.com/dotnet/templating/wiki/Reference-for-template.json)
