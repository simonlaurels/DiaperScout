# Backend Services

## Purpose

This document describes the logical backend services that collectively implement DiaperScout.

These services define architectural responsibilities rather than deployment units.

A service may initially be implemented within a single application while remaining logically independent.

As DiaperScout evolves, services may be separated into independent deployments without changing the architectural model.

Every service exists to support the Atlas and the Explorer experience.

---

# Philosophy

Backend services exist to protect clear architectural boundaries.

Each service owns a specific responsibility.

Services collaborate through well-defined interfaces rather than sharing implementation details.

No service should assume responsibility that belongs to another.

Maintaining these boundaries keeps the architecture understandable, testable and maintainable.

The logical service model should not be interpreted as a requirement to build a distributed microservice architecture.

The initial production implementation should favour simplicity while preserving these boundaries within the application architecture.

---

# Design Principles

Every service should:

* own a coherent responsibility;
* expose a stable interface;
* communicate through published contracts;
* remain independent of presentation technologies;
* support event-driven workflows where appropriate;
* preserve the integrity of the Atlas;
* avoid unnecessary coupling to other services.

Service boundaries should remain recognisable even if deployment technologies evolve.

---

# Service Overview

The architecture consists of the following logical services.

| Service                       | Primary Responsibility                                         |
| ----------------------------- | -------------------------------------------------------------- |
| Atlas Service                 | Publishes canonical knowledge                                  |
| Observation Service           | Records real-world observations and evidence                   |
| Editorial Service             | Curates and publishes knowledge                                |
| Community Service             | Supports Explorers, contributions and internal Community Trust |
| Discovery Service             | Identifies Knowledge Gaps and manages Discovery Tasks          |
| Retail Service                | Represents the retail world                                    |
| Search Service                | Provides deterministic discovery                               |
| Media Service                 | Stores and processes media                                     |
| Notification Service          | Communicates meaningful events                                 |
| Authentication Service        | Establishes identity and authorisation                         |
| API Service                   | Provides the application interface                             |
| Background Processing Service | Executes asynchronous work                                     |

These are logical boundaries.

They do not imply that each service must initially be deployed independently.

---

# Atlas Service

## Responsibility

The Atlas Service owns the published Atlas.

It is responsible for:

* Products;
* Product Specifications;
* Manufacturers;
* Brands;
* Product Variants;
* Size Variants;
* Pack Types;
* canonical Product Media;
* Regional Variations;
* published knowledge.

The Atlas Service exposes DiaperScout's current understanding of the world.

It never accepts direct community edits.

Changes originate only through accepted editorial decisions.

---

# Observation Service

## Responsibility

The Observation Service records real-world observations and the evidence supporting them.

Responsibilities include:

* Product Discoveries;
* Retail Observations;
* Correction Requests;
* Manufacturer Submissions;
* barcode observations;
* supporting Evidence;
* Observation provenance.

Observations are historical records.

They are never responsible for directly modifying the Atlas.

The Observation Service preserves what was observed.

The Editorial Service determines whether that information should affect the published Atlas.

---

# Editorial Service

## Responsibility

The Editorial Service transforms evidence into trusted knowledge.

Responsibilities include:

* editorial review;
* moderation;
* approval;
* rejection;
* requests for additional Evidence;
* provenance;
* editorial history;
* publication decisions.

The Editorial Service is the only logical service capable of publishing changes into the Atlas.

Regardless of where evidence originates, every contribution follows the appropriate editorial pathway.

Manufacturer submissions do not bypass editorial review.

---

# Community Service

## Responsibility

The Community Service supports people participating in DiaperScout and manages community contribution context.

Responsibilities include:

* Explorer Profiles;
* contribution history;
* contributor attribution;
* Backpack data;
* Collections;
* Scrapbook data;
* internal Community Trust;
* community participation state.

The Community Service supports the community without creating artificial hierarchy.

---

# Community Trust

Community Trust is an internal signal representing an Explorer's demonstrated reliability as a Contributor.

The Community Service may maintain the underlying information required to calculate or evaluate Community Trust.

Inputs may include:

* accepted contributions;
* evidence quality;
* correction history;
* successful Knowledge Gap resolution;
* consistency;
* abuse or misinformation history.

Community Trust may support workflow and moderation decisions.

It is not:

* a public reputation score;
* XP;
* karma;
* a leaderboard;
* a security role.

Community Trust does not automatically grant Moderator or other privileged permissions.

---

# Contributor

Contributor is not a security role.

An Explorer becomes a Contributor through submitting information to DiaperScout.

The Community Service may derive contribution status from contribution history rather than maintaining a separate Contributor role.

The service should preserve sufficient provenance to identify who contributed information and when.

---

# Discovery Service

## Responsibility

The Discovery Service identifies useful gaps in the Atlas and manages structured investigations into those gaps.

Responsibilities include:

* Knowledge Gaps;
* Discovery Tasks;
* task prioritisation;
* task state;
* task assignment or participation;
* task outcomes;
* investigation provenance.

Discovery Tasks provide voluntary opportunities for Explorers to improve the Atlas.

They do not represent ranks, roles or permissions.

---

# Discovery Task Generation

Discovery Tasks may originate from:

* incomplete Product Specifications;
* conflicting Evidence;
* stale availability information;
* regional uncertainty;
* missing Products;
* community correction requests;
* editorial analysis;
* automated gap detection.

The Discovery Service may generate tasks automatically where appropriate.

Automated task generation must never directly modify canonical Atlas data.

---

# Retail Service

## Responsibility

The Retail Service represents the physical retail world.

Responsibilities include:

* Retailers;
* Locations;
* Countries;
* geographic relationships;
* retailer metadata.

Retail availability itself is derived from relevant Observations rather than maintained directly.

A Retailer does not directly establish that a Product is currently available.

A Location represents the physical place where a Product may be observed.

---

# Search Service

## Responsibility

The Search Service enables deterministic exploration of the Atlas.

Responsibilities include:

* Product search;
* deterministic filtering;
* barcode lookup;
* Brand search;
* Manufacturer search;
* Location search;
* search indexing.

Search indexes canonical Atlas knowledge rather than raw unreviewed Observations.

Search behaviour should remain deterministic and explainable.

---

# Media Service

## Responsibility

The Media Service manages media assets.

Responsibilities include:

* canonical Product imagery;
* Observation Evidence media;
* upload handling;
* optimisation;
* thumbnail generation;
* media integrity;
* storage metadata.

Atlas Media and Observation Media remain separate throughout their lifecycle.

The Media Service preserves media provenance and ownership.

---

# Notification Service

## Responsibility

The Notification Service communicates meaningful events to Explorers and privileged users.

Examples include:

* Observation accepted;
* correction resolved;
* additional Evidence requested;
* Discovery Task update;
* Moderator invitation;
* important editorial outcome.

Routine community activity should remain discoverable within the application rather than generating unnecessary notifications.

Notifications should be meaningful rather than engagement-driven.

---

# Authentication Service

## Responsibility

The Authentication Service establishes and protects User identity.

Responsibilities include:

* authentication;
* sessions;
* account recovery;
* identity verification;
* authorisation integration;
* privileged access;
* Verified Manufacturer identity.

Authentication answers:

> **Who is this User?**

Authorisation answers:

> **What actions is this User permitted to perform?**

The Authentication Service should not own domain contribution history or Community Trust.

Those responsibilities belong to the Community Service.

---

# Authorisation

Authorisation represents genuine security boundaries.

Initial privileged responsibilities include:

* Moderator;
* Administrator;
* Verified Manufacturer.

Explorer and Contributor are not authorisation roles.

Community Trust does not automatically grant privileged access.

Role assignment must be explicit and follow the principle of least privilege.

---

# API Service

## Responsibility

The API Service exposes the public application interface of DiaperScout.

Every client, including the Progressive Web App, consumes the same API.

The API Service is responsible for:

* HTTP endpoints;
* API versioning;
* request validation;
* authentication integration;
* authorisation enforcement;
* DTOs;
* response formatting;
* API documentation;
* rate limiting;
* API-level observability.

Business rules remain within the Application and Domain layers rather than client applications or API endpoints.

The API represents domain capabilities rather than underlying database tables.

---

# Background Processing Service

## Responsibility

The Background Processing Service performs asynchronous work that should not block ordinary application requests.

Examples include:

* image processing;
* thumbnail generation;
* search indexing;
* Discovery Task generation;
* Community Trust recalculation;
* availability freshness calculations;
* notification delivery;
* media processing;
* other scheduled or event-driven work.

Background processing should be event-driven where practical.

Long-running operations should expose observable progress where appropriate.

---

# Service Interaction

Services collaborate through published interfaces and application-level contracts.

A typical Atlas contribution flow is:

```text id="lq5r0n"
Observation Service
        ↓
Editorial Service
        ↓
Atlas Service
        ↓
Search Service
        ↓
Notification Service
```

A Discovery flow may be:

```text id="4bjh9r"
Atlas / Editorial Analysis
        ↓
Discovery Service
        ↓
Explorer
        ↓
Observation Service
        ↓
Editorial Service
        ↓
Atlas Service
```

Community Trust may be updated as a consequence of accepted contribution outcomes:

```text id="af3c4n"
Observation
    ↓
Editorial Outcome
    ↓
Community Service
    ↓
Community Trust
```

Each service remains responsible only for its own domain.

---

# Event Ownership

Services communicate primarily through meaningful domain or integration events.

Examples include:

* Observation Submitted;
* Evidence Added;
* Editorial Decision Recorded;
* Atlas Updated;
* Product Published;
* Discovery Task Generated;
* Discovery Task Completed;
* Community Contribution Accepted;
* Community Trust Updated.

Events describe what has happened.

They do not instruct another service how to implement its response.

Each service determines how events affect its own responsibilities.

---

# Event Principles

Events should:

* represent meaningful domain changes;
* contain sufficient information for consumers;
* avoid exposing internal implementation details;
* be versionable;
* be idempotently consumable where appropriate.

Not every database mutation requires an event.

Events should exist where another responsibility genuinely needs to react.

---

# Service Boundaries

Maintaining service boundaries is essential.

For example:

* The Observation Service records evidence.
* The Editorial Service evaluates evidence.
* The Atlas Service publishes knowledge.
* The Search Service indexes published knowledge.
* The Community Service maintains contribution context and internal trust.
* The Discovery Service identifies and manages Knowledge Gaps.
* The Retail Service represents physical locations.
* The Media Service manages media assets.
* The Authentication Service protects identity and access.

No service should directly manipulate another service's internal state.

---

# Data Ownership

Each logical service should have clear ownership of the data it manages.

Examples:

| Data                    | Owner                  |
| ----------------------- | ---------------------- |
| Canonical Product       | Atlas Service          |
| Product Specification   | Atlas Service          |
| Observation             | Observation Service    |
| Evidence                | Observation Service    |
| Editorial Decision      | Editorial Service      |
| Explorer Profile        | Community Service      |
| Backpack                | Community Service      |
| Community Trust         | Community Service      |
| Knowledge Gap           | Discovery Service      |
| Discovery Task          | Discovery Service      |
| Retailer                | Retail Service         |
| Location                | Retail Service         |
| Search Index            | Search Service         |
| Media Metadata          | Media Service          |
| Authentication Identity | Authentication Service |

Ownership does not necessarily mean separate databases.

It means that one service is the authoritative owner of the concept.

---

# Shared Data

Services should not create competing authoritative copies of the same domain concept.

Where another service requires information owned elsewhere, it should use:

* an application interface;
* a published contract;
* a domain event;
* a read model;
* an appropriately cached representation.

Duplicated data used for performance must remain clearly identified as derived data.

---

# Atlas Integrity

The Atlas Service is the final owner of canonical published knowledge.

No other service may directly mutate canonical Product information.

The normal path remains:

```text id="x5yr24"
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

This applies regardless of whether the original information came from:

* an Explorer;
* a Contributor;
* a Verified Manufacturer;
* an automated Discovery Task.

---

# Service and Deployment

Logical services do not require immediate physical separation.

The initial production architecture should favour a modular application with clear internal service boundaries.

A future deployment may evolve towards:

```text id="5t7p9j"
                    ┌── Atlas
                    ├── Community
                    ├── Discovery
                    ├── Search
                    ├── Media
                    └── Notifications
                         ...
```

without requiring the domain model to change.

The architecture should therefore optimise first for **clear ownership and maintainability**, not premature distribution.

---

# Failure Isolation

Where practical, asynchronous service interactions should tolerate temporary failure.

Examples include:

* Search indexing delayed after Atlas publication;
* notification delivery delayed after an editorial decision;
* thumbnail processing delayed after media upload;
* Community Trust recalculation delayed after an accepted contribution.

The core Atlas transaction should not depend on every secondary service being immediately available.

---

# Observability

Services should provide sufficient telemetry to understand:

* successful operations;
* failures;
* processing latency;
* event processing;
* background jobs;
* external dependencies.

Telemetry should identify the logical service responsible for an operation.

Sensitive personal information should not be logged unnecessarily.

---

# Testing

Each logical service should be independently testable.

Tests should cover:

* domain rules;
* application behaviour;
* service boundaries;
* event handling;
* authorisation;
* failure handling;
* data ownership.

Integration tests should verify important cross-service workflows.

---

# Evolution

Logical service boundaries should remain stable throughout the lifetime of DiaperScout.

Deployment strategies may evolve.

Infrastructure may evolve.

Programming languages may evolve.

The responsibilities of the services should remain recognisable.

New services should only be introduced when they represent a genuinely distinct responsibility.

A new service should not be created merely because a new class or database table has appeared.

---

# Anti-Patterns

The following should not be introduced without explicit architectural review.

## Scout Service

There is no `Scout Service`.

Community participation belongs to the Community Service.

Discovery Tasks belong to the Discovery Service.

---

## Microservice-by-Noun

Do not create a separate deployed service for every domain entity.

Logical responsibility is more important than deployment granularity.

---

## Shared Database Ownership

Do not allow multiple services to independently modify the same authoritative data.

---

## Direct Atlas Mutation

No service other than the Atlas Service may directly modify canonical Atlas data.

---

## Trust as Authorisation

Community Trust must not become an automatic permission system.

---

## Client-Owned Business Logic

Clients must not implement authoritative business rules.

---

## Database-as-Service

Services must not expose raw persistence structures as their public contracts.

---

# Relationship to Other Documents

This document defines the responsibilities of each logical backend service.

Related documents describe the architecture from complementary perspectives.

* **Domain Model** defines the concepts the services operate upon.
* **Entity Reference** defines the entity inventory.
* **Database Model** defines persistence principles.
* **Authentication & Roles** defines identity and security responsibilities.
* **Discovery Task System** defines Knowledge Gaps and Discovery Tasks.
* **API Architecture** describes how clients interact with backend capabilities.
* **Workflow Architecture** explains how work moves between services.
* **Deployment & Operations** explains how services are operated.

Together these documents describe how DiaperScout is implemented while preserving clear ownership, evidence, editorial authority and community stewardship.
