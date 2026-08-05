# Dependency Management

**Document Status:** Draft  
**Version:** 1.0  
**Owner:** DiaperScout Project  
**Last Updated:** 2026-08-02

---

# 1. Purpose

This document defines how dependencies are managed throughout the DiaperScout platform.

Dependency management exists to:

- Reduce coupling
- Improve maintainability
- Simplify testing
- Promote clear responsibilities
- Support future evolution

The platform adopts the Dependency Injection facilities built into ASP.NET Core.

---

# 2. Philosophy

Application components should describe **what they need**, not **how to create it**.

Classes should depend upon services supplied by the application rather than constructing dependencies themselves.

Example:

Good:

> "I need something that can search for products."

Poor:

> "Create a PostgreSQL search service."

This separation keeps classes focused on their responsibilities.

---

# 3. Dependency Injection

Dependency Injection (DI) is the primary mechanism for supplying services throughout the platform.

The DI container is responsible for:

- Creating services
- Managing service lifetimes
- Resolving dependencies
- Wiring the application together

Application components should never manually construct complex dependencies using `new` unless creating simple value objects.

---

# 4. Dependency Direction

Dependencies should always point towards the core domain.

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

The Domain layer should remain independent of infrastructure concerns.

---

# 5. Service Registration

Service registrations should occur during application startup.

Registration should remain centralised and easy to audit.

Examples include:

- Application services
- Database contexts
- Storage providers
- Search providers
- Email services

Developers should not register services throughout the application.

---

# 6. Service Lifetimes

Appropriate lifetimes should be selected for each service.

Typical guidance:

| Lifetime | Typical Usage |
|----------|---------------|
| Singleton | Stateless shared services |
| Scoped | Database contexts, business services |
| Transient | Lightweight helper services |

Service lifetimes should reflect the behaviour of the service rather than arbitrary convention.

---

# 7. Business Abstractions

Abstractions should represent business capabilities.

Preferred examples:

- IProductSearchService
- IImageStorageService
- IEmailService
- IObservationProcessor

Avoid abstractions that merely duplicate technology.

Poor examples:

- IPostgreSqlService
- IEntityFrameworkService

The abstraction should describe **what** the platform needs rather than **how** it is implemented.

---

# 8. Entity Framework Core

Entity Framework Core is considered part of the Infrastructure layer.

It should not be abstracted purely for architectural purity.

Developers should avoid creating unnecessary generic repository layers that simply wrap Entity Framework Core.

Where EF Core provides appropriate capabilities directly, those capabilities should be used.

---

# 9. External Services

External technologies should remain isolated within Infrastructure.

Examples include:

- PostgreSQL
- Cloudflare R2
- Email providers
- Future search engines

Application workflows should communicate through business-oriented services rather than technology-specific implementations.

---

# 10. Testing

Dependency Injection enables components to be tested independently.

Tests may substitute implementations where appropriate.

Examples include:

- Fake email services
- In-memory image storage
- Stubbed external APIs

Business logic should not require production infrastructure to execute.

---

# 11. Configuration

Configuration values should be supplied through the ASP.NET Core configuration system.

Services should avoid reading configuration files directly.

Configuration should be injected in the same manner as other dependencies.

---

# 12. Future Evolution

Dependency management should support future platform growth.

Examples include:

- Additional storage providers
- Multiple authentication providers
- Alternative search implementations
- Background processing

New capabilities should integrate through existing dependency management practices rather than introducing parallel mechanisms.

---

# 13. Design Philosophy

Dependency Injection is a tool for reducing coupling—not an objective in itself.

The goal is not to maximise abstraction.

The goal is to make components easier to understand, easier to test and easier to evolve.

DiaperScout favours pragmatic dependency management over architectural dogma.

Abstractions should exist because they simplify the platform, not because a pattern suggests they should.