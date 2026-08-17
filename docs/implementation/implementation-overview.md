# Implementation Overview

## Purpose

This document defines the implementation strategy for the DiaperScout production application.

It translates the architectural decisions into a practical development approach.

The objective is to move from documented architecture to a maintainable production system without introducing unnecessary complexity.

The implementation should proceed as a series of complete, testable vertical slices rather than attempting to build every layer independently before anything works.

---

# Production Principle

DiaperScout is now being built as the production application.

The implementation must therefore favour:

* real domain behaviour;
* real persistence;
* real authentication;
* real API contracts;
* real editorial workflows;
* real testing;
* production-appropriate security.

Prototype shortcuts should not become permanent architecture.

Where a temporary implementation is unavoidable, it should be clearly isolated and recorded.

---

# Technology Baseline

The production application uses:

* .NET;
* ASP.NET Core;
* Blazor Web App;
* Progressive Web App capabilities;
* Entity Framework Core;
* PostgreSQL;
* .NET Aspire;
* Cloudflare R2 or compatible object storage for user/media assets;
* ASP.NET Core authentication and authorisation.

The exact package and framework versions are governed by the repository dependency baseline.

Dependencies should be kept current within supported release lifecycles.

---

# Architectural Shape

The production application is a modular monolith with explicit internal boundaries.

```text id="q5wq7a"
                    DiaperScout
                         │
          ┌──────────────┴──────────────┐
          │                             │
        Web                            API
          │                             │
          └──────────────┬──────────────┘
                         │
                   Application
                         │
                 ┌───────┴───────┐
                 │               │
              Domain       Infrastructure
                                 │
                       ┌─────────┴─────────┐
                       │                   │
                   PostgreSQL          Object Storage
```

The logical backend services remain visible within these boundaries.

They do not initially require independent deployment.

---

# Core Architectural Layers

## Web

The Web project provides the Explorer experience.

Responsibilities include:

* navigation;
* pages;
* components;
* forms;
* accessibility;
* responsive behaviour;
* PWA capabilities;
* authentication UX;
* Backpack experience;
* contribution UX.

The Web project does not access persistence directly.

---

## API

The API provides the HTTP application boundary.

Responsibilities include:

* endpoints;
* request validation;
* API contracts;
* authentication integration;
* authorisation;
* OpenAPI;
* HTTP concerns.

API endpoints remain thin.

---

## Application

The Application layer implements use cases.

It coordinates:

* commands;
* queries;
* validation;
* workflows;
* domain operations;
* persistence abstractions;
* external capability abstractions.

The Application layer contains application orchestration but should not become a dumping ground for domain rules.

---

## Domain

The Domain layer contains the core business model.

It owns:

* entities;
* value objects;
* invariants;
* domain rules;
* domain events.

The Domain has no dependency on:

* ASP.NET Core;
* Blazor;
* Entity Framework Core;
* PostgreSQL;
* Cloudflare R2;
* HTTP.

---

## Infrastructure

Infrastructure implements technical capabilities.

Examples include:

* EF Core;
* PostgreSQL;
* authentication persistence;
* object storage;
* email;
* search;
* background processing;
* external services.

Infrastructure-specific details remain outside the Domain.

---

# Logical Service Boundaries

The implementation preserves the following logical responsibilities:

```text id="q3y9t4"
Atlas
Observation
Editorial
Community
Discovery
Retail
Search
Media
Notification
Authentication
```

These should normally be represented through modules and services inside the modular monolith.

The application should not create a network boundary simply because two concepts have different names.

---

# Domain Modules

The initial Domain/Application organisation should broadly reflect:

```text id="l9qv6r"
Products
Retail
Observations
Editorial
Discovery
Community
Backpack
```

Additional modules may be introduced as the domain evolves.

Modules should be introduced when they represent meaningful boundaries rather than arbitrary code grouping.

---

# Authentication

Authentication establishes the technical User identity.

The initial authentication strategy is passwordless:

* Email Magic Links;
* Passkeys/WebAuthn.

Anonymous users can explore the published Atlas.

Authentication is required for protected actions such as:

* submitting Observations;
* submitting Evidence;
* saving personal information;
* participating in Discovery Tasks;
* privileged operations.

Authentication does not determine whether a User is a Contributor.

---

# Authorisation

Authorisation is policy-based and server-side.

The initial privileged responsibilities are:

* Moderator;
* Administrator;
* Verified Manufacturer.

Explorer is not an authorisation role.

Contributor is not an authorisation role.

Community Trust is not an authorisation mechanism.

The implementation must never derive privileged permissions automatically from contribution volume or Community Trust.

---

# Community Model

A User participates in the application as an Explorer.

An Explorer may become a Contributor through contribution.

```text id="g9t8pk"
User
  ↓
Explorer
  ↓
Contribution
  ↓
Contributor history
```

Community Trust is maintained as internal community information.

It may support:

* moderation recommendations;
* workflow prioritisation;
* contribution evaluation.

It does not grant privileges automatically.

---

# Product Model

The Product hierarchy is:

```text id="b2uv3z"
Product
└── Product Variant
    └── Size Variant
        └── Pack Type
            └── GTIN
```

The implementation must preserve these distinctions.

A GTIN is not automatically the identity of a Product.

Regional variations and product-specific specifications remain distinct domain concepts.

---

# Retail Model

Retail information is represented as:

```text id="d7v6f1"
Retailer
   │
   └── Location
          │
          └── Observation
                 │
                 └── Product
```

Retail availability is derived from observations.

The system does not maintain an authoritative live stock state.

An observation that a Product was seen at a Location does not guarantee that the Product remains in stock.

---

# Observation Model

Observations are the primary mechanism by which the community records real-world information.

Examples include:

* Product Discovery;
* Retail Observation;
* Correction Request;
* Manufacturer Submission.

An Observation contains or references the relevant information and supporting Evidence.

The original Observation remains historically attributable to its contributor.

---

# Evidence Model

Evidence supports Observations.

Evidence may include:

* photographs;
* packaging;
* barcodes;
* measurements;
* written notes;
* manufacturer documentation.

Original Evidence should be preserved.

Derived processing may create:

* thumbnails;
* extracted barcode values;
* searchable metadata;
* optimised media.

Derived data must not replace the original Evidence.

---

# Editorial Model

Community information does not directly become canonical Atlas knowledge.

The central production workflow is:

```text id="s3h2u8"
Explorer
   ↓
Observation
   ↓
Evidence
   ↓
Editorial Review
   ↓
Editorial Decision
   ↓
Atlas
```

The Editorial Service is the authoritative gateway into the published Atlas.

Verified Manufacturer submissions follow the same editorial principle.

---

# Discovery Model

Knowledge Gaps identify areas where the Atlas lacks sufficiently reliable or complete information.

Discovery Tasks provide actionable ways for Explorers to investigate those gaps.

```text id="9r4h4m"
Knowledge Gap
      ↓
Discovery Task
      ↓
Explorer
      ↓
Observation / Evidence
      ↓
Editorial Review
      ↓
Atlas
```

Discovery Tasks are voluntary.

They do not create:

* ranks;
* roles;
* permissions;
* public scores.

---

# Backpack

The Backpack represents the Explorer's personal journey.

It may contain:

* saved Products;
* saved Locations;
* Collections;
* discoveries;
* Scrapbook content;
* other personal journey information.

Backpack data belongs to the authenticated User.

It must be protected through resource ownership policies.

---

# Database

PostgreSQL is the authoritative relational store for structured application data.

Entity Framework Core provides persistence mapping and migrations.

The database preserves:

* canonical Atlas knowledge;
* Observations;
* Evidence metadata;
* Editorial Decisions;
* provenance;
* community contribution history;
* Discovery Tasks;
* Backpack data;
* authentication data;
* required audit information.

The database is not exposed directly to clients.

---

# Persistence Boundaries

EF Core belongs in Infrastructure.

Domain entities must not depend on EF Core-specific implementation details.

Entity configurations should remain separate from domain entities where practical.

Migrations must represent intentional changes to the persistent model.

---

# Media Storage

User-submitted and other large media assets should not be stored directly in PostgreSQL.

Object storage should be used.

The logical model is:

```text id="f3r9x7"
Observation
    ↓
Evidence
    ↓
Media Metadata
    ↓
Object Storage
```

The database retains metadata and provenance.

Cloudflare R2 is the initial preferred object-storage target where appropriate.

The Domain remains unaware of the provider.

---

# Search

Search operates against published or appropriately indexed information.

Search should not expose unreviewed community evidence as canonical knowledge.

Search indexing is a derived system.

The Atlas remains the source of truth.

A typical publication flow is:

```text id="b2s0oz"
Atlas Updated
      ↓
Search Index Update
```

Search failures should not corrupt the Atlas.

---

# Background Processing

Background processing handles work that does not need to block the Explorer.

Examples include:

* media processing;
* thumbnail generation;
* search indexing;
* notification delivery;
* Discovery Task generation;
* Community Trust evaluation;
* availability freshness calculations.

Background processing should use durable state and idempotent operations where practical.

The initial implementation does not require a separate Worker project.

---

# Events

Meaningful domain transitions may produce events.

Examples include:

* Observation Submitted;
* Evidence Added;
* Editorial Decision Recorded;
* Atlas Updated;
* Product Published;
* Discovery Task Generated;
* Discovery Task Completed;
* Contribution Accepted;
* Community Trust Updated.

Events describe what happened.

They do not prescribe implementation details to consuming components.

---

# API Contracts

API endpoints use explicit request and response contracts.

Persistence entities must never be exposed directly as API models.

The API should represent domain capabilities rather than database tables.

Initial API versioning uses:

```text id="x0wq88"
/api/v1/
```

---

# Web Application

The Web application consumes the API boundary and provides the Explorer experience.

The initial client is a Blazor Web App with PWA capabilities.

The Web application should support:

* desktop;
* tablet;
* mobile;
* offline-aware contribution drafting where appropriate.

Offline work must be clearly distinguished from server-confirmed submission.

---

# First Production Vertical Slice

The implementation should begin with a small but complete vertical slice.

The first slice should establish:

```text id="0u1u6b"
Product
  ↓
Application Query
  ↓
API
  ↓
Web
  ↓
PostgreSQL
```

This proves:

* project references;
* Domain;
* Application;
* Infrastructure;
* EF Core;
* PostgreSQL;
* API;
* Web;
* testing.

The first slice should use real persistence rather than an in-memory replacement that becomes permanent.

---

# First Contribution Slice

After the first read-only slice, the next complete vertical slice should establish:

```text id="z6o9m3"
Authenticated Explorer
       ↓
Observation
       ↓
Evidence
       ↓
Editorial Review
       ↓
Editorial Decision
       ↓
Atlas Update
```

This proves the central DiaperScout proposition.

It should include:

* authentication;
* authorisation;
* Observation persistence;
* Evidence handling;
* editorial workflow;
* Atlas update;
* audit/provenance;
* integration testing.

---

# Development Order

Implementation should proceed in vertical slices.

A practical sequence is:

## Phase 1 — Foundation

* Create solution;
* establish project references;
* configure Aspire;
* configure PostgreSQL;
* configure shared tooling;
* establish architecture tests.

## Phase 2 — Domain

* Product;
* Product Variant;
* Size Variant;
* Pack Type;
* GTIN;
* Product Specification.

## Phase 3 — Atlas Read Path

* persistence;
* Application queries;
* API;
* Web Product pages;
* search/filter foundations.

## Phase 4 — Authentication

* User;
* passwordless authentication;
* Passkeys;
* session management;
* authorisation policies.

## Phase 5 — Contributions

* Observation;
* Evidence;
* media;
* contributor attribution;
* validation.

## Phase 6 — Editorial

* editorial queue;
* review;
* decisions;
* Atlas publication;
* provenance.

## Phase 7 — Discovery

* Knowledge Gaps;
* Discovery Tasks;
* participation;
* contribution integration.

## Phase 8 — Community

* Backpack;
* Collections;
* Scrapbook;
* Community Trust.

## Phase 9 — Supporting Systems

* search indexing;
* notifications;
* background processing;
* caching;
* operational tooling.

Each phase should leave the application in a working state.

---

# Testing Strategy

Testing is part of implementation rather than a final phase.

The production solution uses:

* Unit Tests;
* Integration Tests;
* Architecture Tests;
* end-to-end tests where justified.

Critical workflows should be tested end-to-end.

The most important production path is:

```text id="w8x1j7"
Observation
  ↓
Evidence
  ↓
Editorial Review
  ↓
Atlas
```

---

# Security

Security must be implemented from the beginning.

The production application must include:

* HTTPS;
* secure authentication;
* policy-based authorisation;
* input validation;
* rate limiting;
* secure media handling;
* secret management;
* audit logging;
* least privilege;
* dependency updates;
* appropriate database protection.

Security cannot be deferred until deployment.

---

# Observability

The production application should provide:

* structured logging;
* metrics;
* distributed tracing where appropriate;
* health checks;
* background job visibility;
* authentication/security event visibility.

Observability should help answer:

> **What is happening, where, and why?**

Sensitive personal information must not be logged unnecessarily.

---

# Deployment

The deployment architecture should use the same application structure as development wherever practical.

The application should be deployable without changing domain behaviour between environments.

Environment-specific concerns include:

* connection strings;
* object storage credentials;
* authentication configuration;
* email configuration;
* telemetry;
* domain names;
* secrets.

These should be supplied through environment configuration rather than source code.

---

# Production Readiness

A feature should not be considered production-ready merely because its UI works.

A production feature requires:

* Domain behaviour;
* Application workflow;
* persistence;
* API contract where applicable;
* authorisation;
* validation;
* error handling;
* tests;
* observability;
* migration strategy;
* appropriate documentation.

---

# Temporary Implementations

Temporary implementations are permitted during development where they accelerate progress.

They must:

* be clearly identified;
* be isolated;
* not leak into the Domain;
* not become an undocumented dependency;
* have a defined replacement path.

Examples include:

* temporary mock external services;
* development-only authentication configuration;
* local object-storage substitutes.

Production behaviour must never silently depend on a development-only substitute.

---

# Architectural Discipline

The implementation must resist feature-driven architectural drift.

Do not introduce:

* arbitrary services;
* unnecessary abstractions;
* duplicate domain concepts;
* database-driven API models;
* client-side business rules;
* premature microservices;
* obsolete terminology.

When a new feature appears, first determine which existing domain concept it belongs to.

Only introduce a new concept when the domain genuinely requires it.

---

# Terminology

The production codebase uses the approved project terminology.

Canonical terms include:

* User;
* Explorer;
* Contributor;
* Product;
* Product Variant;
* Size Variant;
* Pack Type;
* Manufacturer;
* Brand;
* Retailer;
* Location;
* Observation;
* Evidence;
* Editorial Decision;
* Knowledge Gap;
* Discovery Task;
* Community Trust;
* Backpack;
* Scrapbook.

The following are prohibited in production architecture and code unless referring explicitly to historical documentation:

* Scout;
* Scout Service;
* Scout Task;
* Scout Points;
* Trusted Scout;
* Trusted Explorer as a role.

---

# Definition of Done

A production feature is complete when:

* its domain model is defined;
* its application behaviour is implemented;
* persistence is implemented;
* API contracts are implemented where required;
* UI is implemented where required;
* authentication/authorisation is correct;
* validation is implemented;
* errors are handled;
* tests pass;
* architecture tests pass;
* observability exists;
* migrations are reviewed;
* documentation is updated.

"Works on my machine" is not a production definition of done.

---

# Implementation Milestone

The documentation phase reaches implementation readiness when:

* the Domain Model is agreed;
* the Entity Reference is agreed;
* the Database Model is agreed;
* API Architecture is agreed;
* Backend Services are agreed;
* Workflow Architecture is agreed;
* Authentication is agreed;
* Authorisation is agreed;
* Solution Structure is agreed;
* Project Layout is agreed.

At that point the repository can become the authoritative implementation of the architecture.

---

# Relationship to Other Documents

This document translates the architecture into an implementation strategy.

Related documents include:

* **Solution Structure** — defines projects and dependency boundaries.
* **Project Layout** — defines the physical repository structure.
* **Domain Model** — defines business concepts.
* **Entity Reference** — defines production entities.
* **Database Model** — defines persistence.
* **API Architecture** — defines the HTTP boundary.
* **Backend Services** — defines logical service responsibilities.
* **Workflow Architecture** — defines process lifecycles.
* **Authentication Strategy** — defines identity implementation.
* **Authorization** — defines permission implementation.
* **Coding Standards** — defines implementation conventions.
* **Data Access Strategy** — defines persistence implementation.
* **Testing Strategy** — defines verification.

---

# Summary

DiaperScout should now move from architecture into production implementation.

The guiding sequence is:

```text id="q4q9mx"
Architecture
    ↓
Solution
    ↓
Domain
    ↓
Persistence
    ↓
Application
    ↓
API
    ↓
Web
    ↓
Authentication
    ↓
Contribution
    ↓
Editorial
    ↓
Atlas
```

The production build should proceed through complete vertical slices.

The first objective is not to build every feature.

It is to prove the architecture with a real, persisted, tested path through the application.

The central production workflow remains:

```text id="d3s6xw"
Explorer
   ↓
Observation
   ↓
Evidence
   ↓
Editorial Review
   ↓
Atlas
```

Everything else exists to make that workflow useful, trustworthy, secure and enjoyable to use.
