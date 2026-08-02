# Backend Services

## Purpose

This document describes the logical backend services that collectively implement DiaperScout.

These services define architectural responsibilities rather than deployment units.

A service may initially be implemented within a single application while remaining logically independent.

As DiaperScout evolves, services may be separated into independent deployments without changing the architectural model.

Every service exists to bring the Atlas to life.

---

# Philosophy

Backend services exist to protect clear architectural boundaries.

Each service owns a specific responsibility.

Services collaborate through well-defined interfaces rather than sharing implementation details.

No service should assume responsibility that belongs to another.

Maintaining these boundaries keeps the architecture understandable, testable and maintainable.

---

# Design Principles

Every service should:

- own a single responsibility;
- expose a stable interface;
- communicate through published contracts;
- remain independent of presentation technologies;
- support event-driven workflows;
- preserve the integrity of the Atlas.

Service boundaries should remain stable even if deployment technologies evolve.

---

# Service Overview

The architecture consists of the following logical services.

| Service | Primary Responsibility |
|----------|------------------------|
| Atlas Service | Publishes canonical knowledge |
| Observation Service | Records evidence |
| Editorial Service | Curates knowledge |
| Scout Service | Supports community stewardship |
| Retail Service | Represents the retail world |
| Search Service | Provides deterministic discovery |
| Media Service | Stores and processes media |
| Notification Service | Communicates meaningful events |
| Authentication Service | Identity and responsibility |
| API Service | Public interface |
| Background Processing Service | Executes asynchronous work |

---

# Atlas Service

## Responsibility

The Atlas Service owns the published Atlas.

It is responsible for:

- Products
- Product Specifications
- Manufacturers
- Canonical Product Images
- Regional Variations

The Atlas Service exposes DiaperScout's current understanding of the world.

It never accepts direct community edits.

Changes originate only through accepted editorial decisions.

---

# Observation Service

## Responsibility

The Observation Service records community evidence.

Responsibilities include:

- Product discoveries
- Retail observations
- Correction requests
- Manufacturer submissions
- Barcode submissions

Observations are historical records.

They are never responsible for directly modifying the Atlas.

---

# Editorial Service

## Responsibility

The Editorial Service transforms evidence into trusted knowledge.

Responsibilities include:

- moderation;
- review;
- approval;
- rejection;
- provenance;
- editorial history.

The Editorial Service is the only service capable of publishing changes into the Atlas.

Regardless of where evidence originates, every contribution follows the same editorial pathway.

---

# Scout Service

## Responsibility

The Scout Service supports community stewardship.

Responsibilities include:

- Scout Tasks;
- trust calculation;
- contribution history;
- promotion recommendations.

The Scout Service strengthens the Atlas indirectly by encouraging valuable exploration.

---

# Retail Service

## Responsibility

The Retail Service represents the retail world.

Responsibilities include:

- retailers;
- countries;
- locations;
- retailer metadata.

Retail availability itself is derived from observations rather than maintained directly.

---

# Search Service

## Responsibility

The Search Service enables exploration of the Atlas.

Responsibilities include:

- product search;
- deterministic filtering;
- barcode lookup;
- search indexing.

Search indexes canonical Atlas knowledge rather than raw observations.

Search behaviour should remain deterministic and explainable.

---

# Media Service

## Responsibility

The Media Service manages media assets.

Responsibilities include:

- canonical product imagery;
- observation evidence;
- optimisation;
- thumbnail generation;
- media integrity.

Atlas Media and Observation Media remain separate throughout their lifecycle.

---

# Notification Service

## Responsibility

The Notification Service communicates meaningful events.

Examples include:

- discovery accepted;
- correction resolved;
- moderator invitation;
- additional evidence requested.

Routine community activity should remain discoverable within the application rather than generating notifications.

---

# Authentication Service

## Responsibility

The Authentication Service establishes identity.

Responsibilities include:

- authentication;
- sessions;
- roles;
- permissions;
- verified manufacturers.

Authentication answers:

> Who is this contributor?

Authorisation answers:

> What responsibilities do they possess?

---

# API Service

## Responsibility

The API Service exposes the public interface of DiaperScout.

Every client, including the Progressive Web App, consumes the same API.

Business rules remain within backend services rather than client applications.

The API represents the Atlas rather than the underlying database.

---

# Background Processing Service

## Responsibility

The Background Processing Service performs asynchronous work.

Examples include:

- image processing;
- thumbnail generation;
- search indexing;
- Scout Task generation;
- trust recalculation;
- retailer confidence updates;
- follow-up reminders.

Background work should be event-driven rather than dependent upon maintenance windows.

Long-running operations should expose observable progress where appropriate.

---

# Service Interaction

Services collaborate through published interfaces.

Typical interactions include:

```text
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

Each service remains responsible only for its own domain.

No service should directly manipulate another service's internal state.

---

# Event Ownership

Services communicate primarily through events.

Examples include:

- Observation Submitted
- Editorial Decision Recorded
- Atlas Updated
- Product Published
- Scout Task Generated

Events describe what has happened.

They do not instruct other services how to respond.

Each service determines how those events affect its own responsibilities.

---

# Service Boundaries

Maintaining service boundaries is essential.

For example:

- The Observation Service records evidence.
- The Editorial Service evaluates evidence.
- The Atlas Service publishes knowledge.
- The Search Service indexes published knowledge.
- The Scout Service identifies opportunities to improve the Atlas.

These responsibilities should remain distinct regardless of deployment strategy.

---

# Evolution

Logical service boundaries should remain stable throughout the lifetime of DiaperScout.

Deployment strategies may evolve.

Infrastructure may evolve.

Programming languages may evolve.

The responsibilities of the services should remain recognisable.

---

# Relationship to Other Documents

This document defines the responsibilities of each backend service.

Related documents describe the architecture from different perspectives.

- **Domain Model** defines the concepts the services operate upon.
- **Workflow Architecture** explains how work moves between services.
- **API Architecture** describes how clients interact with the services.
- **Deployment & Operations** explains how services are operated.

Together these documents describe how the Atlas is implemented while preserving clear architectural responsibilities.