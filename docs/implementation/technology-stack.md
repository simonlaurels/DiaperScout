# Technology Stack

**Document Status:** Draft  
**Version:** 1.0  
**Owner:** DiaperScout Project  
**Last Updated:** 2026-08-02

---

# 1. Purpose

This document defines the approved technology stack for the DiaperScout platform.

The objective is to provide a cohesive, maintainable and production-ready foundation that aligns with the architectural principles of the project.

Technology selections are based on technical suitability, long-term maintainability and developer productivity rather than vendor preference.

---

# 2. Guiding Principles

The technology stack has been selected according to the following principles:

- Prefer a cohesive developer experience.
- Reduce unnecessary cognitive load.
- Minimise the number of programming languages.
- Embrace mature, well-supported technologies.
- Optimise for a hosted platform.
- Avoid unnecessary abstraction.
- Introduce additional technologies only when they solve a real problem.

---

# 3. Platform Overview

| Area | Technology |
|-------|------------|
| Programming Language | C# |
| Runtime | .NET |
| Backend Framework | ASP.NET Core |
| Frontend Framework | Blazor Web App |
| Local Orchestration | .NET Aspire |
| Data Access | Entity Framework Core |
| Database | PostgreSQL |
| Object Storage | Cloudflare R2 |
| CDN / Edge | Cloudflare |
| Version Control | Git |
| Repository Hosting | GitHub |

---

# 4. Backend

## .NET

.NET provides the runtime and application framework for the platform.

Reasons for selection:

- Mature ecosystem
- Excellent performance
- Cross-platform support
- Long-term support releases
- Strong tooling
- Excellent integration with ASP.NET Core

---

## ASP.NET Core

ASP.NET Core provides the API layer of the platform.

Responsibilities include:

- Authentication
- Authorisation
- API endpoints
- Validation
- Dependency Injection
- Application hosting

The API is considered the primary interface to the DiaperScout platform.

---

# 5. Frontend

## Blazor Web App

The primary client application is implemented using Blazor Web App.

Reasons for selection:

- Unified C# development experience
- Modern component model
- Progressive Web App support
- Excellent ASP.NET Core integration
- Suitable foundation for an API-first architecture

Blazor was selected following evaluation against modern JavaScript frameworks.

---

## Progressive Web App

The initial DiaperScout client is delivered as a Progressive Web App (PWA).

Reasons include:

- Cross-platform availability
- Installation from the browser
- Offline capabilities
- Reduced development overhead
- Rapid iteration

Native mobile applications remain a future expansion rather than an initial objective.

---

# 6. Data

## PostgreSQL

PostgreSQL is the sole relational database used by DiaperScout.

Reasons for selection:

- Mature open-source platform
- Excellent reliability
- Advanced indexing
- Full-text search
- JSON support
- Rich extension ecosystem
- Excellent hosting support

Database portability is not considered an implementation objective.

Where PostgreSQL provides platform-specific capabilities that materially improve the platform, those capabilities should be embraced.

---

## Entity Framework Core

Entity Framework Core provides object-relational mapping between the domain model and PostgreSQL.

Reasons for selection:

- Strong .NET integration
- Excellent developer productivity
- Migration support
- LINQ querying
- Mature tooling

Entity Framework Core should be understood as an abstraction over SQL rather than a replacement for SQL.

Developers should maintain a working understanding of SQL and database behaviour.

---

# 7. Storage

## Cloudflare R2

Images and other large binary assets are stored within Cloudflare R2.

Responsibilities include:

- Product gallery images
- Observation photographs
- Future media assets

Structured application data remains within PostgreSQL.

---

# 8. Edge Platform

## Cloudflare

Cloudflare provides:

- CDN
- Edge caching
- HTTPS
- Global delivery
- Static asset distribution

Where appropriate, Cloudflare services may be used to improve application performance and operational simplicity.

---

# 9. Local Development

## .NET Aspire

.NET Aspire provides the local orchestration environment.

Responsibilities include:

- Running services
- Managing configuration
- Local dependencies
- Development dashboard
- Service discovery

Aspire improves consistency between local development and production deployments.

---

# 10. Authentication

The platform adopts a passwordless authentication strategy.

Initial implementation:

- Email magic links
- Passkeys (WebAuthn)

Future expansion may include:

- Sign in with Apple
- Sign in with Google

Third-party identity providers should complement—not replace—the core authentication experience.

---

# 11. Development Philosophy

The technology stack intentionally favours a small number of complementary technologies.

Where practical, implementation should remain within the .NET ecosystem.

However, mature external technologies should be adopted where they clearly provide superior solutions.

Examples include:

- Browser APIs
- Barcode scanning libraries
- Image processing
- Mapping components

The objective is to build the DiaperScout platform rather than recreate existing technology.

---

# 12. Future Evolution

The technology stack is expected to evolve over time.

Future additions may include:

- Native mobile clients
- Advanced analytics
- AI-assisted moderation (if ever adopted)
- Additional storage services
- Operational tooling

New technologies should only be introduced when they provide clear architectural or operational value.

Technology should remain an enabler rather than a source of unnecessary complexity.