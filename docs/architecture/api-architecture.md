# API Architecture

## Purpose

This document defines the architecture and principles of the DiaperScout API.

The API provides a stable boundary between the DiaperScout application clients and the production domain.

It exposes domain capabilities rather than database structures.

The API must preserve the principles established by the Domain Model, Entity Reference, Authentication & Roles and Knowledge Architecture.

---

# API Philosophy

The API exists to allow clients to:

* explore the Atlas;
* discover Products;
* explore Locations;
* submit Observations;
* submit Evidence;
* participate in Discovery Tasks;
* manage personal Backpack information;
* authenticate and manage their account;
* perform authorised editorial and administrative operations.

The API should not expose internal implementation details unnecessarily.

The API is not a direct database interface.

---

# Architectural Principles

The API follows these principles:

* API-first;
* domain-oriented;
* versioned;
* explicit;
* secure by default;
* stateless where practical;
* backwards-compatible;
* predictable;
* observable;
* independently testable.

The API should expose meaningful domain operations rather than CRUD endpoints for every database table.

---

# API Boundary

The general architecture is:

```text
Blazor / PWA Client
        │
        ▼
   DiaperScout API
        │
        ▼
   Application Layer
        │
        ▼
      Domain
        │
        ▼
   Infrastructure
        │
        ▼
    PostgreSQL
```

The API should never allow clients to bypass the Application Layer and interact directly with persistence.

---

# API Versioning

The API is versioned explicitly.

The initial public API uses:

```text
/api/v1/
```

Versioning protects clients from breaking changes as the application evolves.

Breaking changes require a new API version.

Non-breaking additions may be introduced within an existing version where appropriate.

---

# Resource Naming

Resource names should use plural nouns where appropriate.

Examples:

```text
/api/v1/products
/api/v1/products/{productId}
/api/v1/locations
/api/v1/locations/{locationId}
/api/v1/observations
/api/v1/discovery-tasks
```

Resources should describe domain concepts rather than database table names.

---

# Products

Products are a primary Atlas resource.

Examples:

```text
GET /api/v1/products
GET /api/v1/products/{productId}
```

Product queries may support:

* search;
* filtering;
* sorting;
* pagination;
* regional context;
* product type;
* brand;
* manufacturer;
* size;
* availability evidence.

The Product endpoint should expose the canonical Product representation.

It should not expose internal database identifiers unnecessarily.

---

# Product Hierarchy

The Product API must preserve the conceptual hierarchy:

```text
Product
└── Product Variant
    └── Size Variant
        └── Pack Type
            └── GTIN
```

Clients should not have to reconstruct this hierarchy from unrelated database records.

Where appropriate, Product responses may include the relevant nested information.

---

# Brands

Brands are primarily a Product filtering and discovery concept.

Where a dedicated Brand resource is useful, it may be exposed through:

```text
GET /api/v1/brands
GET /api/v1/brands/{brandId}
```

Brands should not automatically become top-level navigation destinations merely because they exist as API resources.

---

# Manufacturers

Manufacturers may be exposed as Atlas resources.

Examples:

```text
GET /api/v1/manufacturers
GET /api/v1/manufacturers/{manufacturerId}
```

Public manufacturer information should be separated from private verification information.

---

# Locations

Locations are first-class Atlas resources.

Examples:

```text
GET /api/v1/locations
GET /api/v1/locations/{locationId}
```

Location queries may support:

* geographic area;
* distance;
* country;
* retailer;
* recent observations;
* Product observations.

Locations represent physical places.

---

# Retailers

Retailers provide context for Locations.

Examples:

```text
GET /api/v1/retailers
GET /api/v1/retailers/{retailerId}
```

The API should not model Retailers as permanent Product stock owners.

A Location may show Products observed there based on relevant observations.

---

# Availability

Availability is derived from observations.

The API may provide availability summaries such as:

```text
GET /api/v1/products/{productId}/availability
```

These responses must communicate that the information represents community observations rather than guaranteed live stock.

Availability responses should include appropriate:

* observation date;
* Location;
* evidence age;
* confidence/context;
* provenance where appropriate.

The API must never imply that an observation guarantees current stock.

---

# Observations

Observations are a core community contribution resource.

Examples:

```text
GET /api/v1/observations
GET /api/v1/observations/{observationId}
POST /api/v1/observations
```

Observation creation requires authentication.

An Observation should identify, where applicable:

* Product;
* Location;
* observation time;
* observation type;
* submitted information;
* contributor;
* supporting Evidence.

---

# Observation Lifecycle

The API should preserve the Observation lifecycle.

```text
Draft
  ↓
Submitted
  ↓
Under Review
  ↓
Accepted / Rejected / Deferred
```

The exact workflow state is controlled by the application.

Clients must not directly set editorial outcomes through ordinary Observation endpoints.

---

# Observation Immutability

The factual content of a submitted Observation should normally be immutable.

Workflow metadata may change.

Examples include:

* review status;
* editorial state;
* requests for additional Evidence.

Corrections should be represented through appropriate workflows rather than silently rewriting historical observations.

---

# Evidence

Evidence is associated with Observations.

Examples:

```text
GET /api/v1/observations/{observationId}/evidence
POST /api/v1/observations/{observationId}/evidence
```

Evidence may include:

* photographs;
* barcode images;
* packaging;
* measurements;
* written notes;
* manufacturer documentation.

Evidence submission requires authentication.

The API must preserve Evidence provenance.

---

# Media

Media should be uploaded through an appropriate controlled mechanism.

The API should not require large binary payloads to pass through application servers unnecessarily where object storage is available.

The general pattern may be:

```text
Client
  ↓
Upload authorisation
  ↓
Object Storage
  ↓
Media confirmation
  ↓
Observation Evidence
```

The database stores metadata and provenance.

Binary media may be stored in object storage.

---

# Editorial API

Editorial operations are privileged.

Examples include:

```text
GET /api/v1/editorial/reviews
GET /api/v1/editorial/observations/{observationId}
POST /api/v1/editorial/observations/{observationId}/decisions
```

Editorial endpoints require appropriate Moderator permissions.

Ordinary Explorers must not be able to:

* publish canonical knowledge;
* approve their own observations;
* alter editorial decisions;
* bypass Evidence requirements.

---

# Atlas Publication

The API should distinguish between:

**community contribution**

and

**published Atlas knowledge**.

A client requesting Product information normally receives the current canonical representation.

The API should not require clients to reconstruct canonical knowledge from raw Observations.

---

# Provenance

Where useful to the client, published information should expose provenance.

Examples include:

* observation date;
* source type;
* evidence availability;
* editorial status;
* last verified information.

Internal moderation details must not be exposed unnecessarily.

---

# Discovery Tasks

Discovery Tasks are authenticated community workflow resources.

Examples:

```text
GET /api/v1/discovery-tasks
GET /api/v1/discovery-tasks/{taskId}
POST /api/v1/discovery-tasks/{taskId}/accept
POST /api/v1/discovery-tasks/{taskId}/abandon
POST /api/v1/discovery-tasks/{taskId}/contribute
```

Discovery Tasks are voluntary.

They do not create a user role or permission level.

The API must not expose a `Scout` concept.

---

# Discovery Task Security

Discovery Task participation requires authentication.

The API should prevent:

* duplicate task claims;
* task manipulation;
* artificial trust inflation;
* repeated low-quality submissions;
* unauthorised task state changes.

Completing a Discovery Task may contribute to internal Community Trust through the normal contribution evaluation process.

---

# Community Trust

Community Trust is internal.

It must not be exposed as a public numeric reputation score.

Ordinary API responses should not expose:

* trust score;
* trust rank;
* trust percentile;
* moderation weighting;
* internal trust calculations.

Internal services may use Community Trust where authorised.

---

# Backpack

The Backpack is a personal authenticated resource.

Examples:

```text
GET /api/v1/me/backpack
GET /api/v1/me/backpack/saved-products
GET /api/v1/me/backpack/saved-locations
```

Backpack information belongs to the authenticated User.

Clients should normally access it through `/me` resources rather than passing arbitrary User IDs.

---

# Personal Data

Authenticated personal resources should prefer:

```text
/api/v1/me/...
```

rather than:

```text
/api/v1/users/{userId}/...
```

This reduces accidental exposure of other users' private information.

Administrative user endpoints may use explicit User identifiers where authorised.

---

# Explorer Profile

Public Explorer information may be exposed separately from private account information.

Example:

```text
GET /api/v1/explorers/{explorerId}
```

Public profile information may include:

* display name;
* public contribution summary;
* selected Backpack content.

It must not expose private authentication or Community Trust information.

---

# Contributor Information

Contributor is not an API role.

The API should not expose:

```text
GET /api/v1/contributors
```

merely to represent a security class.

Contribution information should instead be represented through:

* Observations;
* Evidence;
* public attribution;
* contribution history.

---

# Authentication

Authentication establishes User identity.

The API should support the authentication strategy defined by the authentication architecture.

Authentication concerns include:

* sign-in;
* account creation;
* session management;
* account recovery;
* verification;
* sign-out.

Authentication should remain separate from domain operations.

---

# Authorisation

Authorisation determines whether an authenticated User may perform a particular action.

The principal privileged responsibilities are:

* Moderator;
* Administrator;
* Verified Manufacturer.

Explorer and Contributor are not authorisation roles.

Community Trust does not automatically grant privileged access.

---

# Manufacturer Contributions

Verified Manufacturers may submit official information through authorised endpoints.

Examples may include:

```text
POST /api/v1/manufacturer-submissions
GET /api/v1/manufacturer-submissions/{submissionId}
```

Manufacturer submissions enter the appropriate editorial workflow.

Verification establishes identity.

It does not permit direct modification of canonical knowledge.

---

# Search

Search is an API capability rather than necessarily a single database resource.

Examples may include:

```text
GET /api/v1/search
```

Search may return:

* Products;
* Brands;
* Manufacturers;
* Locations;
* Observations where appropriate.

Search responses should provide enough information for clients to navigate to the underlying resource.

---

# Geographic Queries

Location-aware queries may support:

* latitude;
* longitude;
* radius;
* bounding box;
* country;
* region.

Geographic queries must respect privacy and security requirements.

Precise personal location information must not be exposed unnecessarily.

---

# Pagination

Collection endpoints should support pagination.

The API should use a consistent pagination strategy across resources.

Large collections must never require clients to download the complete dataset.

---

# Filtering

Filtering should use explicit query parameters.

Examples:

```text
/products?brandId=...
/products?productType=...
/locations?radius=...
/observations?productId=...
```

Filters should be documented and validated.

---

# Sorting

Sorting should be explicit and deterministic.

Examples include:

* relevance;
* distance;
* observation date;
* Product name.

The API must define default ordering for every paginated collection.

---

# Errors

API errors should use a consistent machine-readable format.

Errors should provide:

* HTTP status;
* stable error code;
* human-readable message;
* relevant validation information where appropriate;
* correlation information where useful.

Internal exception details must never be exposed to clients.

---

# Validation

Validation occurs at the API boundary and within the Application Layer.

The API should reject:

* malformed requests;
* missing required information;
* invalid identifiers;
* impossible values;
* unauthorised operations.

Domain rules remain the responsibility of the Domain/Application layers rather than being implemented exclusively at the HTTP boundary.

---

# Concurrency

The API should protect against conflicting updates.

Where appropriate, optimistic concurrency should be used.

This is particularly important for:

* editorial decisions;
* Product Specifications;
* Discovery Tasks;
* user-owned drafts;
* administrative configuration.

Clients should receive a clear response when an update conflicts with a newer version.

---

# Idempotency

Operations that may be safely retried should be designed to be idempotent where practical.

Particular care is required for:

* Observation submission;
* Evidence upload confirmation;
* editorial actions;
* Discovery Task state changes.

Duplicate community contributions should not be created simply because a client retries a request.

---

# Caching

Public read-heavy Atlas resources may be cached where appropriate.

Examples include:

* Product information;
* Brand information;
* Manufacturer information;
* Location information.

User-specific and privileged responses require appropriate cache controls.

---

# Rate Limiting

Rate limiting should protect:

* authentication endpoints;
* search;
* Observation creation;
* Evidence submission;
* Discovery Task actions;
* administrative endpoints.

Limits should be proportionate to the operation.

---

# Security

The API must:

* require authentication for protected operations;
* enforce authorisation server-side;
* validate all input;
* protect against injection;
* protect sensitive information;
* apply rate limiting;
* use secure transport;
* avoid leaking internal implementation details.

Clients must never be trusted to enforce security.

---

# Observability

API operations should produce structured telemetry sufficient to diagnose failures.

Telemetry should support:

* request tracing;
* performance monitoring;
* error diagnosis;
* authentication events;
* editorial actions;
* administrative actions.

Sensitive personal information should not be written to logs unnecessarily.

---

# OpenAPI

The API should publish an OpenAPI description for supported endpoints.

The OpenAPI contract should document:

* endpoints;
* request schemas;
* response schemas;
* authentication requirements;
* validation errors;
* versioning;
* relevant examples.

OpenAPI documentation is a contract for API consumers.

It must remain aligned with the actual implementation.

---

# DTOs

API contracts should use explicit request and response models.

Persistence entities must not be exposed directly as API contracts.

This prevents database implementation details from becoming public API commitments.

DTOs should represent the information required by a client rather than mirroring database tables.

---

# Domain Isolation

The API must not expose:

* EF Core entities;
* database navigation properties;
* internal identifiers unnecessarily;
* persistence-specific concepts;
* internal moderation data.

The Application Layer translates between API contracts and domain operations.

---

# API and Application Layer

The intended request flow is:

```text
HTTP Request
     ↓
API Endpoint
     ↓
Request Validation
     ↓
Application Command / Query
     ↓
Domain
     ↓
Infrastructure
     ↓
Response DTO
     ↓
HTTP Response
```

Endpoints should remain thin.

Business logic belongs in the Application and Domain layers.

---

# API and Events

Some operations may publish domain or integration events.

Examples include:

* Observation submitted;
* Observation accepted;
* Product discovered;
* Editorial Decision recorded;
* Discovery Task completed;
* Product Specification changed.

Events should represent meaningful domain changes rather than every database mutation.

---

# Backwards Compatibility

Once an API contract is published, changes should preserve compatibility wherever possible.

Breaking changes require:

* a new API version;
* migration guidance;
* a documented deprecation period where appropriate.

The implementation must not make undocumented breaking changes to public contracts.

---

# API Evolution

The API should evolve with the domain.

New functionality should normally follow:

```text
Domain Concept
      ↓
Application Capability
      ↓
API Contract
      ↓
Client Experience
```

The API should not be allowed to create domain concepts merely because they are convenient for a client.

---

# Anti-Patterns

The following should not be introduced without explicit architectural review.

## Database-as-API

Do not expose database tables directly as REST resources.

## Scout Endpoints

Do not create `Scout` endpoints, DTOs, roles or identifiers.

## Contributor Role Endpoints

Do not treat Contributor as a security role.

## Public Trust Endpoint

Do not expose internal Community Trust as a public reputation API.

## Direct Atlas Mutation

Do not allow community Observations to directly modify canonical Atlas data.

## Permanent Stock API

Do not expose observation-derived availability as guaranteed live inventory.

## Self-Approval

Do not allow Contributors or Verified Manufacturers to approve their own submissions.

## Client-Side Authorisation

Never rely on the client to enforce privileged operations.

---

# Summary

The DiaperScout API provides a stable, secure boundary between the application and the production domain.

Its central flow is:

```text
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

And its architectural responsibilities remain clearly separated:

```text
User
 ├── Explorer experience
 ├── Contributions
 └── Backpack

Moderator
 └── Editorial responsibility

Administrator
 └── Platform responsibility

Verified Manufacturer
 └── Official manufacturer submissions
```

The API exposes **capabilities and domain concepts**, not database structures.

It exists to make the Guide useful, trustworthy and evolvable while preserving the evidence and provenance behind the Atlas.
