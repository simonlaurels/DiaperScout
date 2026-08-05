# Solution Structure

**Document Status:** Draft  
**Version:** 1.0  
**Owner:** DiaperScout Project  
**Last Updated:** 2026-08-02

---

# 1. Purpose

This document defines the organisation of the DiaperScout solution.

The objective is to produce a solution that is:

- Easy to understand
- Easy to navigate
- Easy to maintain
- Easy to extend

Each project within the solution has a single, clearly defined responsibility.

The solution structure reflects the architectural separation between presentation, application logic, domain modelling and infrastructure.

---

# 2. Design Principles

The solution structure follows several core principles.

## Single Responsibility

Every project should have one primary purpose.

Projects should not accumulate unrelated responsibilities.

---

## Clear Dependencies

Dependencies should always point inward towards the core domain.

Outer layers depend upon inner layers.

Inner layers never depend upon presentation technologies.

---

## Technology Independence

The Domain project should not depend upon:

- ASP.NET Core
- Blazor
- PostgreSQL
- Entity Framework Core
- Cloudflare
- Aspire

The business model should remain independent of implementation technologies wherever practical.

---

## API First

The Web application is treated as a client of the API.

Future clients, including native mobile applications, should communicate with the same platform APIs.

---

# 3. Solution Overview

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

Each project has a distinct responsibility.

---

# 4. Project Responsibilities

## DiaperScout.AppHost

Purpose:

Local application orchestration using .NET Aspire.

Responsibilities:

- Starting services
- Configuration
- Local infrastructure
- Service discovery
- Development dashboard

This project exists solely to support development.

It is not part of the deployed application.

---

## DiaperScout.ServiceDefaults

Purpose:

Shared Aspire configuration.

Responsibilities:

- Health checks
- Telemetry
- OpenTelemetry configuration
- Resilience defaults
- Common service configuration

This project promotes consistency across all executable projects.

---

## DiaperScout.Web

Purpose:

User interface.

Responsibilities:

- Blazor pages
- Components
- Navigation
- Layouts
- Styling
- User interaction

The Web project should contain presentation logic only.

Business rules must not be implemented here.

---

## DiaperScout.Api

Purpose:

HTTP interface to the platform.

Responsibilities:

- REST endpoints
- Authentication
- Authorisation
- Request validation
- Response formatting

The API coordinates requests.

It should not contain business logic.

---

## DiaperScout.Application

Purpose:

Application workflows.

Responsibilities:

- Use cases
- Commands
- Queries
- Business processes
- Coordination between services

Examples include:

- Create Review
- Search Products
- Submit Observation
- Award Scout Points

Application defines what the platform does.

---

## DiaperScout.Domain

Purpose:

The business model of DiaperScout.

Responsibilities:

- Domain entities
- Value objects
- Business rules
- Domain services
- Enumerations

Examples include:

- Product
- Review
- Observation
- Scout
- Manufacturer
- Brand

The Domain project represents the platform independently of implementation technologies.

---

## DiaperScout.Infrastructure

Purpose:

External technology integration.

Responsibilities:

- Entity Framework Core
- PostgreSQL
- Cloudflare R2
- Email
- File storage
- Search implementation
- External services

Infrastructure exists to connect the platform to the outside world.

---

## DiaperScout.Shared

Purpose:

Shared contracts between projects.

Examples include:

- DTOs
- Request models
- Response models
- Shared constants

This project should remain intentionally small.

Business logic does not belong here.

---

## DiaperScout.Tests

Purpose:

Automated testing.

Responsibilities:

- Unit tests
- Integration tests
- API tests
- End-to-end testing

Tests are considered first-class components of the solution.

---

# 5. Dependency Direction

Dependencies should follow the diagram below.

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

Important observations:

- Web depends on API.
- API depends on Application.
- Application depends on Domain.
- Infrastructure depends on Domain.
- Domain depends on nothing.

This ensures the business model remains independent of technology choices.

---

# 6. Typical Request Flow

A typical request progresses through the solution as follows:

```text
User

↓

Web

↓

API

↓

Application

↓

Domain

↓

Infrastructure

↓

PostgreSQL / Cloudflare R2
```

The response then returns through the same layers.

Each layer contributes its own responsibility without assuming the responsibilities of another.

---

# 7. Adding New Features

When implementing new functionality, responsibilities should be allocated according to project purpose.

Example:

Adding Product Reviews:

Web

- Review page
- Components

API

- Review endpoint

Application

- Submit review workflow

Domain

- Review entity
- Review rules

Infrastructure

- Persist review
- Store images

This separation promotes maintainability and testability.

---

# 8. Future Expansion

The solution structure has been designed to support future growth.

Potential additions include:

- Native mobile clients
- Administrative applications
- Background workers
- Operational tooling

These additions should integrate with the existing architecture rather than require structural redesign.

---

# 9. Design Philosophy

Projects exist to separate responsibilities—not to increase complexity.

The objective is to make the correct location for new code obvious.

Developers should be able to answer the question:

> "Where does this belong?"

without ambiguity.

A well-structured solution reduces cognitive load, simplifies maintenance and enables the platform to evolve without becoming increasingly difficult to understand.