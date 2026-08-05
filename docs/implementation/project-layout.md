# Project Layout

**Document Status:** Draft  
**Version:** 1.0  
**Owner:** DiaperScout Project  
**Last Updated:** 2026-08-02

---

# 1. Purpose

This document defines the physical organisation of the DiaperScout source code.

A well-organised solution should make it immediately obvious where new functionality belongs.

The project layout mirrors the architectural layers of the platform and is intended to minimise coupling while maximising clarity.

---

# 2. Design Principles

The solution structure is guided by the following principles.

## Responsibility Before Technology

Projects exist because they have distinct responsibilities, not simply because they use different technologies.

---

## Clear Dependency Direction

Dependencies always point towards the core business domain.

Outer layers depend on inner layers.

Inner layers never depend on presentation technologies.

---

## Discoverability

Developers should be able to locate functionality quickly.

The correct location for new code should usually be obvious.

---

## Consistency

Every project should follow similar organisational patterns wherever practical.

A consistent layout reduces cognitive load for contributors.

---

# 3. Solution Layout

```text
DiaperScout.sln

├── DiaperScout.AppHost
├── DiaperScout.ServiceDefaults
├── DiaperScout.Web
├── DiaperScout.Api
├── DiaperScout.Application
├── DiaperScout.Domain
├── DiaperScout.Infrastructure
├── DiaperScout.Shared
└── DiaperScout.Tests
```

---

# 4. Project Responsibilities

## DiaperScout.AppHost

Responsible for local orchestration using .NET Aspire.

Contains:

- Aspire resources
- Service registrations
- Local infrastructure configuration
- Development startup

No business logic belongs here.

---

## DiaperScout.ServiceDefaults

Provides shared configuration used by all executable services.

Typical responsibilities include:

- OpenTelemetry
- Health checks
- Resilience
- Logging configuration
- Shared middleware defaults

---

## DiaperScout.Web

Contains the Blazor Web App.

Typical folders:

```text
Components/
Layouts/
Pages/
Styles/
wwwroot/
Authentication/
```

Contains presentation logic only.

Business rules must remain outside this project.

---

## DiaperScout.Api

Contains the HTTP API.

Typical folders:

```text
Controllers/
Endpoints/
Middleware/
Filters/
Authentication/
Configuration/
```

Responsible for:

- HTTP endpoints
- Authentication
- Authorisation
- Validation
- API configuration

Should not contain business rules.

---

## DiaperScout.Application

Implements platform workflows.

Typical folders:

```text
Commands/
Queries/
Services/
Interfaces/
Validators/
Mappings/
```

Examples include:

- Create Product
- Submit Review
- Submit Observation
- Search Products
- Award Scout Points

This layer coordinates work across the platform.

---

## DiaperScout.Domain

Represents the business itself.

Typical folders:

```text
Entities/
ValueObjects/
Enums/
Events/
Exceptions/
Services/
```

Example entities:

- Product
- Review
- Observation
- Scout
- Brand
- Manufacturer
- AtlasEntry

This project should remain independent of infrastructure technologies.

---

## DiaperScout.Infrastructure

Contains implementations that connect the platform to external systems.

Typical folders:

```text
Persistence/
Repositories/
Storage/
Search/
Email/
Identity/
Configuration/
```

Responsibilities include:

- Entity Framework Core
- PostgreSQL
- Cloudflare R2
- Email delivery
- Search implementation

---

## DiaperScout.Shared

Contains types shared between multiple projects.

Typical contents:

- DTOs
- Contracts
- Request models
- Response models
- Shared constants

Business logic should not be placed here.

---

## DiaperScout.Tests

Contains automated tests.

Suggested structure:

```text
Unit/
Integration/
Api/
EndToEnd/
Shared/
```

Tests should mirror the structure of the production code where practical.

---

# 5. Dependency Rules

The dependency graph should remain simple.

```text
Web
 │
 ▼
API
 │
 ▼
Application
 │
 ▼
Domain
 ▲
 │
Infrastructure
```

Key rules:

- Domain references nothing else.
- Application references Domain.
- Infrastructure references Domain and Application contracts where required.
- API references Application.
- Web communicates through the API.
- AppHost orchestrates executable projects only.

---

# 6. Naming Conventions

Projects use the prefix:

```text
DiaperScout.
```

Examples:

- DiaperScout.Api
- DiaperScout.Domain
- DiaperScout.Web

Folders should use singular names where representing concepts.

Classes should clearly express intent.

Avoid unnecessary abbreviations.

---

# 7. Feature Organisation

Features should remain vertically cohesive.

For example, "Reviews" should have related components within each appropriate project:

```text
Web
 └── Pages/Reviews

API
 └── Controllers/Reviews

Application
 └── Commands/SubmitReview

Domain
 └── Entities/Review

Infrastructure
 └── Persistence/Reviews
```

This keeps responsibilities aligned while making features easy to follow through the solution.

---

# 8. Future Growth

The layout supports future additions without restructuring the solution.

Potential future projects include:

- DiaperScout.Mobile
- DiaperScout.Admin
- DiaperScout.Workers

These should integrate with the existing dependency model rather than bypass it.

---

# 9. Design Philosophy

The solution should feel intuitive to navigate.

When implementing new functionality, developers should rarely need to ask:

> "Where should this code go?"

Instead, the structure should naturally guide them towards the correct project based on responsibility.

A clear project layout reduces maintenance costs, simplifies onboarding and allows the platform to grow without becoming increasingly difficult to understand.