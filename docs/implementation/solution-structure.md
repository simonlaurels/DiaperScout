# Solution Structure

## Purpose

This document defines the production .NET solution structure for DiaperScout.

The structure provides clear boundaries between:

* application hosting;
* presentation;
* API;
* application logic;
* domain logic;
* infrastructure;
* shared contracts;
* testing.

The solution should remain modular without introducing unnecessary deployment complexity.

---

# Technology Baseline

The production application is based on:

* .NET
* ASP.NET Core
* Blazor Web App
* ASP.NET Core authentication and authorisation
* Entity Framework Core
* PostgreSQL
* .NET Aspire
* Cloudflare R2 for object/media storage where appropriate

The exact framework versions are defined by the production dependency baseline and should be kept current within the supported release strategy.

---

# Solution

The production solution is:

```text
DiaperScout.sln
│
├── DiaperScout.AppHost
├── DiaperScout.ServiceDefaults
│
├── DiaperScout.Web
├── DiaperScout.Api
│
├── DiaperScout.Application
├── DiaperScout.Domain
├── DiaperScout.Infrastructure
├── DiaperScout.Shared
│
└── Tests
    ├── DiaperScout.UnitTests
    ├── DiaperScout.IntegrationTests
    └── DiaperScout.ArchitectureTests
```

These projects represent architectural boundaries.

They are not individual microservices.

---

# DiaperScout.AppHost

## Responsibility

The AppHost is the .NET Aspire application host used to compose the development and deployment environment.

It is responsible for describing application resources and their relationships.

Examples include:

* Web;
* API;
* PostgreSQL;
* object storage integration;
* supporting infrastructure.

The AppHost does not contain domain logic.

It does not contain application business rules.

---

# DiaperScout.ServiceDefaults

## Responsibility

ServiceDefaults contains shared Aspire/service configuration used by application projects.

Examples include:

* telemetry;
* health checks;
* service discovery;
* resilience defaults;
* common hosting configuration.

ServiceDefaults must remain infrastructure-oriented.

It must not contain:

* domain entities;
* business rules;
* application workflows;
* feature-specific logic.

---

# DiaperScout.Web

## Responsibility

The Web project contains the user-facing DiaperScout application.

The initial client is a Blazor Web App designed to support the Progressive Web App experience.

Responsibilities include:

* navigation;
* pages;
* components;
* user interaction;
* client-side state;
* accessibility;
* responsive layouts;
* authentication UX;
* Explorer experience.

The Web project consumes the API/application contracts.

It does not access the production database directly.

---

# DiaperScout.Api

## Responsibility

The API project provides the HTTP boundary for the application.

Responsibilities include:

* API endpoints;
* request validation;
* authentication integration;
* authorisation enforcement;
* API versioning;
* DTO mapping;
* OpenAPI;
* HTTP-specific concerns.

The API should remain thin.

Business rules belong in the Application and Domain projects.

The API must not access Entity Framework Core directly for feature behaviour.

---

# DiaperScout.Application

## Responsibility

The Application project implements use cases and application workflows.

Examples include:

* Product queries;
* Product search;
* Observation submission;
* Evidence submission;
* editorial operations;
* Discovery Task participation;
* Backpack operations;
* account-related application workflows;
* manufacturer submissions.

The Application layer coordinates Domain behaviour and Infrastructure capabilities.

It should not depend on HTTP-specific implementation details.

---

# DiaperScout.Domain

## Responsibility

The Domain project contains the core business model.

It should contain:

* entities;
* value objects;
* domain rules;
* domain events;
* domain services where genuinely required;
* domain exceptions;
* business invariants.

The Domain project must remain independent of:

* ASP.NET Core;
* Blazor;
* Entity Framework Core;
* PostgreSQL;
* Cloudflare R2;
* HTTP;
* UI concerns.

The Domain is the most stable part of the application.

---

# DiaperScout.Infrastructure

## Responsibility

Infrastructure implements technical capabilities required by the Application and Domain layers.

Examples include:

* Entity Framework Core;
* PostgreSQL;
* authentication persistence;
* object storage;
* media storage;
* search infrastructure;
* email delivery;
* external services;
* background processing infrastructure.

Infrastructure may depend on external technologies.

The Application layer should depend on abstractions rather than infrastructure implementations wherever practical.

---

# DiaperScout.Shared

## Responsibility

Shared contains small, stable contracts genuinely shared across application boundaries.

Potential contents include:

* API contracts;
* common result types;
* shared primitives;
* versioned contract definitions.

Shared must remain deliberately small.

It must not become a dumping ground for:

* domain entities;
* business logic;
* utility classes;
* arbitrary helpers;
* persistence models.

If something belongs clearly to Domain, Application or Infrastructure, it should live there.

---

# Tests

Testing is separated according to architectural responsibility.

## DiaperScout.UnitTests

Tests fast, isolated behaviour.

Examples:

* Domain entities;
* value objects;
* business rules;
* application handlers;
* validators.

Unit tests should require minimal infrastructure.

---

## DiaperScout.IntegrationTests

Tests real application boundaries.

Examples:

* API endpoints;
* authentication;
* PostgreSQL persistence;
* EF Core mappings;
* Application-to-Infrastructure workflows;
* complete contribution workflows.

Integration tests may use containerised dependencies where appropriate.

---

## DiaperScout.ArchitectureTests

Tests architectural boundaries.

Examples:

* Domain must not depend on Infrastructure;
* Domain must not depend on ASP.NET Core;
* Web must not access Infrastructure directly;
* API must not contain business logic;
* Application must not depend on Web;
* Shared must remain dependency-light.

Architecture tests protect the structure defined by this document.

---

# Dependency Direction

The primary dependency direction is:

```text
DiaperScout.Web
        │
        ▼
DiaperScout.Api
        │
        ▼
DiaperScout.Application
        │
        ├──────────────► DiaperScout.Domain
        │
        ▼
DiaperScout.Infrastructure
        │
        └──────────────► DiaperScout.Domain
```

Shared contracts may be consumed by appropriate boundary projects.

The Domain must not depend on the outer layers.

---

# Dependency Rules

The following rules are mandatory.

## Domain

May depend on:

* .NET base libraries;
* carefully selected domain-only packages where justified.

Must not depend on:

* Web;
* API;
* Application;
* Infrastructure;
* EF Core;
* ASP.NET Core.

---

## Application

May depend on:

* Domain;
* Shared where appropriate.

May define interfaces implemented by Infrastructure.

Must not depend on:

* Web;
* UI-specific code.

---

## Infrastructure

May depend on:

* Application;
* Domain;
* Shared;
* external infrastructure libraries.

Infrastructure implements application-facing abstractions.

---

## API

May depend on:

* Application;
* Shared;
* authentication infrastructure where required.

Must not contain domain business rules.

---

## Web

May depend on:

* API contracts;
* Shared contracts;
* approved application-facing abstractions where explicitly required.

Must not depend directly on:

* Entity Framework Core;
* PostgreSQL;
* Infrastructure persistence;
* domain persistence mechanisms.

---

# Logical Services

The solution contains logical service boundaries defined by the Backend Services architecture.

These include:

* Atlas;
* Observation;
* Editorial;
* Community;
* Discovery;
* Retail;
* Search;
* Media;
* Notification;
* Authentication.

These are initially implemented as modular application responsibilities rather than separate deployed services.

The code structure should make ownership clear without requiring network boundaries between every responsibility.

---

# No Premature Microservices

The initial production solution must not split every logical service into an independently deployed application.

The first production architecture favours:

> **Modular monolith first.**

This provides:

* simpler deployment;
* simpler debugging;
* simpler local development;
* fewer distributed-system failure modes;
* lower infrastructure overhead;
* easier transactional consistency.

The logical service boundaries remain explicit so that future extraction is possible if justified.

---

# Background Processing

Background processing is initially treated as an application/infrastructure capability.

It does not require a separate `DiaperScout.Workers` project at the beginning.

Background operations may use:

* hosted services;
* queued work;
* scheduled processing;
* event handlers;
* Aspire-managed resources where appropriate.

A separate Worker deployment may be introduced later if operational requirements justify it.

Such a change should not alter the domain model.

---

# Administration

Administrative functionality does not require a separate `DiaperScout.Admin` application initially.

Administrative pages should live within the main Web application and be protected by appropriate authorisation policies.

A separate administrative application may be introduced later if there is a genuine security or operational requirement.

---

# Mobile

There is no initial `DiaperScout.Mobile` project.

The Progressive Web App is the initial cross-device client.

Native mobile applications may be introduced in the future if the product requires capabilities that cannot reasonably be provided by the PWA.

The API remains the stable application boundary regardless of client technology.

---

# Authentication

Authentication is implemented through the established ASP.NET Core authentication infrastructure.

Authentication concerns should remain separated from domain logic.

The application model is:

```text
Authentication
      ↓
User
      ↓
Explorer
      ↓
Application capabilities
```

The solution must not introduce:

* Scout authentication roles;
* Contributor authentication roles;
* Trust-based authentication;
* trust-based authorisation.

---

# Authorisation

Authorisation is implemented through explicit policies.

Initial privileged capabilities include:

* Moderator;
* Administrator;
* Verified Manufacturer.

Explorer and Contributor are not security roles.

Community Trust remains an internal community signal.

---

# Data Access

Only Infrastructure owns persistence implementation.

Entity Framework Core configuration belongs in Infrastructure.

The application should interact with persistence through application-facing abstractions where appropriate.

The Web and API projects must never directly construct database contexts for feature behaviour.

---

# Media

Media storage is an Infrastructure concern.

The application may expose media abstractions such as:

```text
IMediaStorage
```

with an Infrastructure implementation backed by the selected object-storage provider.

The Domain should not know that Cloudflare R2 exists.

---

# External Services

External integrations belong in Infrastructure.

Examples include:

* object storage;
* email providers;
* geolocation services;
* retailer information providers;
* future third-party APIs.

External provider-specific models must not leak into the Domain.

---

# Configuration

Configuration should be supplied through the application hosting environment.

Secrets must not be committed to source control.

Development configuration should use appropriate local secret mechanisms.

Production configuration should be supplied through the deployment environment.

---

# Observability

The application should use the shared service defaults for:

* logging;
* metrics;
* tracing;
* health checks.

Telemetry should identify the logical component responsible for an operation.

Sensitive personal information must not be unnecessarily included in logs.

---

# Naming

Production projects use the `DiaperScout.` prefix consistently.

Examples:

```text
DiaperScout.AppHost
DiaperScout.ServiceDefaults
DiaperScout.Web
DiaperScout.Api
DiaperScout.Application
DiaperScout.Domain
DiaperScout.Infrastructure
DiaperScout.Shared
```

Test projects use the corresponding prefix:

```text
DiaperScout.UnitTests
DiaperScout.IntegrationTests
DiaperScout.ArchitectureTests
```

No project should introduce obsolete terminology such as:

```text
Scout
ScoutService
ScoutPoints
TrustedScout
```

---

# Repository Structure

The repository should broadly follow:

```text
/
├── docs/
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
└── README.md
```

Documentation remains outside the production source tree.

---

# Build Order

The initial solution should be established in this order:

1. Create solution.
2. Create Domain project.
3. Create Application project.
4. Create Infrastructure project.
5. Create Shared project.
6. Create API project.
7. Create Web project.
8. Create ServiceDefaults.
9. Create AppHost.
10. Create test projects.
11. Establish project references.
12. Establish architecture tests.
13. Configure PostgreSQL.
14. Configure authentication.
15. Establish the first vertical slice.

The first feature should be implemented end-to-end rather than building an empty framework indefinitely.

---

# First Vertical Slice

The first production vertical slice should prove the complete architecture.

A suitable first slice is:

```text
Product
   ↓
Product Query
   ↓
Application
   ↓
API
   ↓
Web
```

This establishes:

* Domain;
* Application;
* Infrastructure;
* API;
* Web;
* persistence;
* testing.

A subsequent contribution slice should then prove:

```text
Explorer
   ↓
Observation
   ↓
Evidence
   ↓
Editorial Workflow
   ↓
Atlas
```

---

# Architecture Tests

The solution should include automated checks for the most important boundaries.

At minimum:

```text
Domain → no Infrastructure dependency
Domain → no ASP.NET dependency
Application → no Web dependency
Application → no API dependency
Web → no Infrastructure dependency
API → no direct DbContext feature access
```

These tests exist to prevent architectural drift as the codebase grows.

---

# Evolution

The solution structure may evolve as DiaperScout grows.

New projects should only be introduced where they represent a meaningful architectural or deployment boundary.

A new project should not be created merely because:

* a folder has become large;
* a class has many dependencies;
* a new feature has appeared;
* a service has been named.

The objective is a solution that remains understandable and easy to operate.

---

# Anti-Patterns

The following should not be introduced without explicit architectural review.

## Microservice Explosion

Do not create a deployed service for every logical domain service.

## Mobile Project by Default

Do not create a native mobile application before the PWA has demonstrated a need for one.

## Separate Admin Application by Default

Do not create a separate administration application merely because administrative pages exist.

## Worker Project by Default

Do not create a separate Worker application before background workload requires independent deployment.

## Shared Everything Project

Do not turn Shared into a dumping ground for arbitrary code.

## Domain Infrastructure Leakage

Do not reference EF Core, PostgreSQL, HTTP or object-storage implementations from Domain.

## Database Access from Web

The Web project must never access persistence directly.

## Scout Concepts

No production project, namespace, class or role should introduce obsolete Scout terminology.

---

# Relationship to Other Documents

This document defines the physical .NET solution boundaries.

Related documents include:

* **Project Layout** — defines the detailed directory layout.
* **Implementation Overview** — describes implementation strategy.
* **Domain Model** — defines the business domain.
* **Entity Reference** — defines production entities.
* **Database Model** — defines persistence principles.
* **API Architecture** — defines the API boundary.
* **Backend Services** — defines logical service ownership.
* **Authentication Strategy** — defines identity implementation.
* **Authorization** — defines permission implementation.
* **Testing Strategy** — defines test architecture.

---

# Summary

The production DiaperScout solution is intentionally simple:

```text
                    DiaperScout.sln
                           │
        ┌──────────────────┼──────────────────┐
        │                  │                  │
     Web / API         Application          Tests
        │                  │
        │                  ▼
        │                Domain
        │                  ▲
        └──────────── Infrastructure
                           │
                     PostgreSQL /
                     Object Storage
```

The architecture favours:

* clear boundaries;
* modularity;
* explicit ownership;
* strong domain isolation;
* simple deployment;
* testability;
* future evolution.

The production build should begin as a **modular monolith with a clear domain architecture**, not as a collection of premature microservices.
