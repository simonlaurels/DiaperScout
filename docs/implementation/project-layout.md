# Project Layout

## Purpose

This document defines the physical repository and project layout for the DiaperScout production codebase.

The layout reflects the architectural boundaries defined by the Solution Structure.

The repository should make those boundaries visible.

---

# Repository Root

The production repository uses the following structure:

```text
/
├── docs/
├── src/
├── tests/
├── DiaperScout.sln
├── Directory.Build.props
├── Directory.Packages.props
├── global.json
├── .editorconfig
├── .gitignore
└── README.md
```

Additional repository-level files may be introduced where required by tooling or deployment.

---

# Documentation

All project documentation lives beneath:

```text
docs/
```

Documentation should remain separate from production source code.

The documentation structure broadly follows:

```text
docs/
├── world/
├── architecture/
├── implementation/
├── experience/
└── decisions/
```

The exact documentation taxonomy is governed by the documentation architecture.

---

# Source

Production projects live beneath:

```text
src/
```

The initial production projects are:

```text
src/
├── DiaperScout.AppHost/
├── DiaperScout.ServiceDefaults/
├── DiaperScout.Web/
├── DiaperScout.Api/
├── DiaperScout.Application/
├── DiaperScout.Domain/
├── DiaperScout.Infrastructure/
└── DiaperScout.Shared/
```

No additional production project should be introduced without a clear architectural reason.

---

# DiaperScout.Domain

The Domain project contains the core business model.

A typical layout is:

```text
DiaperScout.Domain/
├── Common/
├── Products/
├── Retail/
├── Observations/
├── Editorial/
├── Discovery/
├── Community/
└── DiaperScout.Domain.csproj
```

The precise feature folders may evolve as the domain grows.

The Domain project must not contain:

* EF Core configuration;
* ASP.NET Core code;
* HTTP models;
* PostgreSQL-specific code;
* object-storage implementations;
* UI components.

---

# Domain Feature Organisation

Domain features should normally be grouped by bounded concept rather than by technical type.

Prefer:

```text
Products/
    Product.cs
    ProductVariant.cs
    SizeVariant.cs
```

over:

```text
Entities/
    Product.cs
    ProductVariant.cs
    SizeVariant.cs
```

This keeps related behaviour close together.

---

# DiaperScout.Application

The Application project contains use cases and application workflows.

A typical layout is:

```text
DiaperScout.Application/
├── Products/
├── Observations/
├── Editorial/
├── Discovery/
├── Community/
├── Retail/
├── Backpack/
├── Authentication/
├── Common/
└── DiaperScout.Application.csproj
```

Application features should be organised around capabilities.

A feature may contain:

```text
Products/
├── Queries/
├── Commands/
├── Handlers/
├── Validators/
└── DTOs/
```

The exact organisation should remain pragmatic.

Do not create unnecessary layers simply to satisfy a pattern.

---

# DiaperScout.Infrastructure

Infrastructure contains implementations of technical concerns.

A typical layout is:

```text
DiaperScout.Infrastructure/
├── Persistence/
│   ├── Configurations/
│   ├── Migrations/
│   └── DiaperScoutDbContext.cs
│
├── Authentication/
├── Authorization/
├── Media/
├── Search/
├── Email/
├── Background/
├── External/
└── DiaperScout.Infrastructure.csproj
```

Infrastructure-specific implementations remain isolated from Domain.

---

# Persistence Layout

Entity Framework Core persistence belongs under:

```text
Infrastructure/Persistence/
```

For example:

```text
Persistence/
├── Configurations/
├── Migrations/
├── DiaperScoutDbContext.cs
└── DesignTimeDbContextFactory.cs
```

Entity configuration should be kept separate from domain entities.

---

# DiaperScout.Api

The API project contains the HTTP boundary.

A typical layout is:

```text
DiaperScout.Api/
├── Endpoints/
├── Filters/
├── Middleware/
├── OpenApi/
├── Configuration/
└── DiaperScout.Api.csproj
```

Endpoints should be grouped by domain capability.

For example:

```text
Endpoints/
├── Products/
├── Locations/
├── Observations/
├── DiscoveryTasks/
├── Backpack/
├── Editorial/
└── ManufacturerSubmissions/
```

API endpoints should remain thin.

---

# DiaperScout.Web

The Web project contains the Blazor Web App and Progressive Web App experience.

A typical layout is:

```text
DiaperScout.Web/
├── Components/
├── Pages/
├── Layout/
├── Features/
├── Services/
├── Authentication/
├── wwwroot/
└── DiaperScout.Web.csproj
```

The exact Blazor structure may follow the current framework conventions where they provide a cleaner result.

---

# Web Feature Organisation

Where practical, UI should be organised by feature.

For example:

```text
Features/
├── Products/
├── Locations/
├── Discovery/
├── Contributions/
├── Backpack/
└── Account/
```

Feature organisation should make it easy to locate all UI associated with a user capability.

---

# Static Assets

Web static assets belong beneath:

```text
DiaperScout.Web/wwwroot/
```

Examples include:

```text
wwwroot/
├── css/
├── images/
├── icons/
├── fonts/
├── manifest.webmanifest
└── service-worker/
```

Brand assets should follow the approved DiaperScout visual system.

---

# DiaperScout.Shared

Shared contains genuinely shared contracts and primitives.

A typical layout is:

```text
DiaperScout.Shared/
├── Contracts/
├── Results/
├── Pagination/
└── DiaperScout.Shared.csproj
```

Shared must remain deliberately small.

Do not place:

* domain entities;
* EF entities;
* database models;
* arbitrary helpers;
* business rules;

in Shared merely to make them accessible from multiple projects.

---

# DiaperScout.AppHost

The Aspire AppHost remains intentionally small.

Typical contents include:

```text
DiaperScout.AppHost/
├── Program.cs
└── DiaperScout.AppHost.csproj
```

The AppHost describes application resources and their relationships.

It does not contain application logic.

---

# DiaperScout.ServiceDefaults

ServiceDefaults contains common Aspire/service configuration.

Typical contents include:

```text
DiaperScout.ServiceDefaults/
├── Extensions.cs
└── DiaperScout.ServiceDefaults.csproj
```

It should remain infrastructure-oriented and reusable.

---

# Tests

Tests live beneath:

```text
tests/
```

The initial test projects are:

```text
tests/
├── DiaperScout.UnitTests/
├── DiaperScout.IntegrationTests/
└── DiaperScout.ArchitectureTests/
```

---

# Unit Tests

Unit tests should mirror domain and application features where practical.

Example:

```text
DiaperScout.UnitTests/
├── Domain/
│   ├── Products/
│   ├── Observations/
│   └── Community/
│
├── Application/
│   ├── Products/
│   ├── Observations/
│   └── Discovery/
│
└── DiaperScout.UnitTests.csproj
```

Tests should focus on behaviour rather than implementation details.

---

# Integration Tests

Integration tests verify real boundaries.

Example:

```text
DiaperScout.IntegrationTests/
├── Api/
├── Persistence/
├── Authentication/
├── Editorial/
├── Contributions/
├── Discovery/
└── DiaperScout.IntegrationTests.csproj
```

These tests may use real PostgreSQL and other containerised dependencies.

---

# Architecture Tests

Architecture tests protect dependency boundaries.

Example:

```text
DiaperScout.ArchitectureTests/
├── DependencyRules/
├── NamingRules/
├── LayerRules/
└── DiaperScout.ArchitectureTests.csproj
```

Examples of protected rules include:

* Domain cannot reference Infrastructure.
* Domain cannot reference ASP.NET Core.
* Web cannot reference Infrastructure.
* API cannot directly access persistence.
* Application cannot reference Web.
* Shared remains dependency-light.

---

# Naming

Namespaces should match project names.

Examples:

```text
DiaperScout.Domain.Products
DiaperScout.Application.Observations
DiaperScout.Infrastructure.Persistence
DiaperScout.Api.Endpoints.Products
DiaperScout.Web.Features.Products
```

Avoid obsolete terminology.

The production codebase must not introduce:

```text
Scout
ScoutService
ScoutPoints
TrustedScout
ScoutTask
```

---

# Feature Naming

Feature names should use the canonical domain vocabulary.

Use:

```text
Observations
Discovery
Community
Products
Locations
Editorial
Backpack
```

rather than technical or obsolete terminology.

---

# Files and Classes

One primary public type should normally occupy one file.

Names should be descriptive and match the type.

Examples:

```text
Product.cs
ProductVariant.cs
Observation.cs
Evidence.cs
DiscoveryTask.cs
CommunityTrust.cs
```

Where a concept is represented as value data rather than an independent entity, its implementation should follow the Domain Model rather than this naming convention mechanically.

---

# Configuration Files

Configuration should be kept separate from application logic.

Examples include:

```text
appsettings.json
appsettings.Development.json
```

Secrets must not be committed to source control.

Environment-specific configuration should be supplied through the appropriate hosting/deployment mechanism.

---

# Generated Files

Generated files should not be manually edited.

Examples include:

* build output;
* generated OpenAPI artefacts;
* generated migration snapshots where applicable;
* IDE files.

Generated output should normally be excluded from source control where appropriate.

---

# Database Migrations

EF Core migrations belong within:

```text
DiaperScout.Infrastructure/Persistence/Migrations/
```

Migrations are production artefacts and must be reviewed as part of schema changes.

A migration should correspond to an intentional change in the persistent model.

---

# Media

Media binaries should not be stored within the source repository.

Application media belongs in object storage.

The repository contains:

* media handling code;
* media metadata models;
* approved static application assets.

User-submitted media is not source-controlled.

---

# No Premature Projects

The initial repository must not include separate projects for:

* Mobile;
* Admin;
* Workers;
* individual logical services.

These may be introduced later where independently deployable boundaries become justified.

---

# Logical Services in the Layout

Logical services are represented through feature/module boundaries rather than separate projects.

For example:

```text
Application/
├── Observations/
├── Editorial/
├── Community/
└── Discovery/
```

and:

```text
Infrastructure/
├── Persistence/
├── Media/
├── Search/
└── Background/
```

This keeps the modular architecture visible without creating unnecessary deployment complexity.

---

# Repository Overview

The intended production repository is therefore:

```text
DiaperScout/
│
├── docs/
│
├── src/
│   ├── DiaperScout.AppHost/
│   ├── DiaperScout.ServiceDefaults/
│   ├── DiaperScout.Web/
│   ├── DiaperScout.Api/
│   ├── DiaperScout.Application/
│   ├── DiaperScout.Domain/
│   ├── DiaperScout.Infrastructure/
│   └── DiaperScout.Shared/
│
├── tests/
│   ├── DiaperScout.UnitTests/
│   ├── DiaperScout.IntegrationTests/
│   └── DiaperScout.ArchitectureTests/
│
├── DiaperScout.sln
├── Directory.Build.props
├── Directory.Packages.props
├── global.json
├── .editorconfig
├── .gitignore
└── README.md
```

---

# Evolution

The project layout may evolve as the production application grows.

New folders and feature modules are expected.

New projects require stronger justification.

The preferred order of complexity is:

```text
New file
   ↓
New feature folder
   ↓
New module
   ↓
New project
   ↓
New deployment boundary
```

The architecture should always choose the smallest boundary that clearly represents the responsibility.

---

# Anti-Patterns

The following should not be introduced without explicit architectural review.

## God Project

Do not collapse the entire application into Web or API.

## Shared Dumping Ground

Do not place arbitrary reusable code into Shared.

## Infrastructure Leakage

Do not place database or provider-specific code in Domain.

## Technical Folder Explosion

Do not create deep generic folders such as:

```text
Services/
Managers/
Helpers/
Utilities/
Processors/
```

unless the structure genuinely improves discoverability.

## Microservice-by-Folder

Do not create separate projects simply because logical services exist.

## Obsolete Terminology

Do not introduce `Scout` terminology into namespaces, folders, classes or API contracts.

---

# Relationship to Other Documents

This document defines the physical repository layout.

Related documents include:

* **Solution Structure** — defines project boundaries.
* **Implementation Overview** — defines implementation strategy.
* **Domain Model** — defines business concepts.
* **Entity Reference** — defines production entities.
* **Database Model** — defines persistence principles.
* **API Architecture** — defines the API boundary.
* **Backend Services** — defines logical service responsibilities.
* **Testing Strategy** — defines test organisation and coverage.
* **Coding Standards** — defines implementation conventions.

---

# Summary

The DiaperScout production repository is intentionally structured around architectural boundaries rather than technical fashion.

The initial implementation is a modular .NET application:

```text
Source
  ├── Web
  ├── API
  ├── Application
  ├── Domain
  ├── Infrastructure
  └── Shared

Infrastructure
  └── AppHost / ServiceDefaults

Tests
  ├── Unit
  ├── Integration
  └── Architecture
```

This provides a clean foundation for building the production Atlas while retaining the ability to evolve individual responsibilities into separate deployment boundaries if the product eventually requires it.
