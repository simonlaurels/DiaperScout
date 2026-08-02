# API Architecture

## Purpose

This document defines the architectural principles governing the DiaperScout API.

The API is the public interface to the Atlas.

Every official application, including the Progressive Web App (PWA), consumes the same API.

The API exists to expose the Atlas rather than the underlying implementation.

It should present a stable, understandable and technology-independent contract between DiaperScout and every client.

---

# Philosophy

DiaperScout is an API-first architecture.

The Progressive Web App is not a privileged client.

Every application communicates with the Atlas through the same published interface.

Examples include:

- Progressive Web App
- Native mobile applications
- Desktop applications
- Administrative tools
- Future integrations
- Automated testing

Business rules should exist exactly once within backend services.

Clients present information.

They do not implement business logic.

---

# Design Principles

The API should be:

- predictable;
- deterministic;
- discoverable;
- self-documenting;
- versioned;
- secure;
- stable.

The API should model the language of the Atlas rather than the structure of the database.

---

# Atlas-Centred Design

The API exposes canonical knowledge.

Examples include:

- Products
- Product Specifications
- Manufacturers
- Retailers
- Regional Variations

Most consumers interact with the Atlas rather than observations.

Observations become visible only where they contribute to transparency or workflow.

The Atlas remains the primary public representation of knowledge.

---

# Resource-Oriented Design

Resources should represent meaningful domain concepts.

Examples include:

- Products
- Observations
- Scout Tasks
- Manufacturers
- Retailers

Resources should never expose implementation-specific structures.

The database schema is an implementation detail.

---

# Observations

Community contributions are submitted as Observations.

Examples include:

- Product discoveries
- Retail observations
- Correction requests
- Manufacturer submissions

Submitting an Observation records evidence.

It does not immediately change the Atlas.

The API should make this distinction explicit.

---

# Workflow-Aware API

Some requests complete immediately.

Others begin workflows.

Examples include:

- discovery submission;
- image upload;
- barcode processing.

The API should distinguish between:

- work that has completed;
- work that has been accepted for processing.

This gives contributors confidence that their work has been safely received.

---

# Progress Reporting

Long-running workflows should expose meaningful progress.

Examples include:

- ✓ Observation received
- ✓ Evidence validated
- ✓ Images processed
- ✓ Ready for editorial review
- ✓ Published

Progress should describe meaningful workflow milestones rather than internal implementation details.

---

# Versioning

Breaking changes should be introduced through explicit API versions.

Supported versions should remain stable.

Deprecated versions should remain available for an appropriate migration period before retirement.

API evolution should be deliberate and predictable.

---

# Authentication

Authentication establishes identity.

Authorisation establishes responsibility.

Authentication should remain consistent regardless of client.

Permissions should be enforced by backend services rather than trusted clients.

---

# Error Handling

Errors should be:

- consistent;
- descriptive;
- actionable.

Responses should explain:

- what happened;
- why it happened;
- how the client can recover where appropriate.

Implementation details should remain hidden.

---

# Idempotency

Repeated requests should behave predictably.

Operations such as:

- observation submission;
- barcode scanning;
- media upload;

should avoid creating duplicate data where practical.

Clients should be able to safely retry interrupted requests.

---

# Search

The API exposes deterministic search.

Free-text search applies to Product names.

Structured filtering uses canonical Product Specification attributes.

Search should remain explainable and reproducible.

Artificial intelligence should not be required to interpret structured knowledge.

---

# Pagination

Large collections should support efficient retrieval.

Responses should encourage incremental loading without exposing implementation-specific pagination strategies.

The API should remain efficient regardless of Atlas size.

---

# Security

Every endpoint should apply appropriate authentication and authorisation.

Validation should occur at the API boundary.

Clients should never be trusted to enforce business rules.

Security exists to protect both the Atlas and the community.

---

# Evolution

The API should evolve alongside the Atlas.

New capabilities should extend existing concepts rather than introducing unnecessary complexity.

Where practical, the API should grow by adding new resources and behaviours rather than changing existing ones.

---

# Architectural Consequences

This architecture results in several important characteristics.

- Every client consumes the same API.
- Business rules exist only within backend services.
- The API exposes the Atlas rather than implementation details.
- Observations enter editorial workflows rather than modifying canonical knowledge directly.
- Long-running work exposes observable progress.
- Clients remain lightweight because behaviour is centralised.

---

# Relationship to Other Documents

This document defines how external clients interact with DiaperScout.

Related documents describe the architecture from complementary perspectives.

- **Backend Services** defines the services behind the API.
- **Workflow Architecture** explains how requests become completed work.
- **Knowledge Architecture** explains how observations become Atlas knowledge.
- **Authentication & Roles** defines identity and responsibility.

Together these documents define both the public interface and the behaviour behind it.