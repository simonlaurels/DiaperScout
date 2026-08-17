# Data Access Strategy

## Purpose

This document defines how DiaperScout accesses persistent data.

The strategy establishes:

* ownership of persistence;
* Entity Framework Core usage;
* PostgreSQL integration;
* query and command patterns;
* transaction boundaries;
* concurrency;
* migrations;
* projections;
* pagination;
* testing;
* performance considerations.

The objective is a data-access architecture that is explicit, testable and maintainable without introducing unnecessary abstraction.

---

# Core Principle

PostgreSQL is the authoritative persistent store for structured application data.

Entity Framework Core is the primary data-access technology.

EF Core belongs exclusively to the Infrastructure layer.

```text
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
EF Core
 ↓
PostgreSQL
```

The Web and API projects must never access `DbContext` directly.

---

# Infrastructure Ownership

The Infrastructure project owns:

* `DbContext`;
* EF Core configuration;
* migrations;
* database connection configuration;
* persistence implementations;
* database-specific optimisations;
* transaction infrastructure.

The Domain must remain unaware that EF Core exists.

---

# DbContext

The application uses an explicit DiaperScout `DbContext`.

For example:

```text
DiaperScoutDbContext
```

The context represents the application's persistence boundary.

It contains:

* `DbSet`s for persisted aggregate roots where appropriate;
* model configuration;
* persistence-specific behaviour.

The `DbContext` should not become a general-purpose application service.

---

# DbContext Lifetime

The normal web request should use a scoped `DbContext`.

A typical lifetime is:

```text
HTTP Request
     ↓
Application Operation
     ↓
DbContext
     ↓
Database
```

The context should normally be disposed at the end of the operation/request scope.

Long-lived `DbContext` instances must be avoided.

---

# No DbContext in Domain

Domain entities must never depend on:

* `DbContext`;
* `DbSet`;
* EF Core attributes where avoidable;
* EF Core change tracking;
* database connections.

The Domain represents business behaviour.

Infrastructure represents persistence.

---

# No DbContext in Web

The Web application must never directly access:

```text
DbContext
DbSet<T>
NpgsqlConnection
```

or other persistence infrastructure.

All data access must pass through the appropriate application/API boundary.

---

# No DbContext in API Feature Logic

API endpoints should not contain feature-level persistence queries.

Avoid:

```text
Endpoint
   ↓
DbContext
   ↓
PostgreSQL
```

Prefer:

```text
Endpoint
   ↓
Application Query / Command
   ↓
Persistence Abstraction
   ↓
DbContext
   ↓
PostgreSQL
```

This keeps HTTP concerns separate from application behaviour.

---

# Repository Strategy

DiaperScout does **not** use a generic repository abstraction.

Do not introduce:

```text
IRepository<T>
GenericRepository<T>
RepositoryBase<T>
UnitOfWork<T>
```

merely to wrap EF Core.

EF Core already provides a unit-of-work and repository-like abstraction through:

* `DbContext`;
* `DbSet<T>`;
* change tracking;
* transactions.

An additional generic abstraction would normally add indirection without providing meaningful domain value.

---

# Specific Repositories

A specific persistence abstraction may be introduced when it represents a genuine boundary.

For example:

```text
IProductReadStore
IObservationStore
```

may be appropriate where:

* the query is complex;
* multiple persistence strategies may exist;
* a read model is involved;
* Infrastructure implementation details would otherwise leak into Application;
* a boundary materially improves testing.

A repository should not exist merely because an entity exists.

---

# Application Data Access

Application handlers should consume the smallest useful abstraction.

For simple operations, an application-specific query or persistence interface may be sufficient.

For example:

```text
IProductQueries
IObservationRepository
```

The interface should describe the capability required by the application rather than exposing the entire database.

---

# Queries

Queries retrieve information without changing domain state.

Examples include:

* Product lookup;
* Product search;
* Location search;
* Observation history;
* Discovery Task listing;
* Backpack retrieval.

Queries should be optimised for the shape of data the caller requires.

They do not need to materialise complete domain aggregates when a projection is sufficient.

---

# Query Projections

Read-heavy operations should use projections where appropriate.

For example:

```text
SELECT
    Product.Id,
    Product.Name,
    Product.Brand,
    Product.ProductType
```

rather than loading an entire aggregate unnecessarily.

EF Core projections should normally execute in the database.

Avoid:

```text
ToList()
```

followed by expensive in-memory filtering when the operation can be performed by PostgreSQL.

---

# Domain Loading

Domain entities should be loaded when domain behaviour needs to operate on them.

A query should not automatically materialise a full domain aggregate simply because EF Core can do so.

The required representation depends on the operation.

This produces two legitimate patterns:

```text
Read
 ↓
Projection
 ↓
DTO
```

and:

```text
Command
 ↓
Aggregate
 ↓
Domain Behaviour
 ↓
Persistence
```

---

# Tracking

EF Core tracking should be used deliberately.

For read-only queries:

```text
AsNoTracking()
```

should normally be preferred.

Tracking is appropriate when:

* an entity will be modified;
* change detection is useful;
* the entity participates in a persistence operation.

Avoid tracking large read-only result sets unnecessarily.

---

# Queries and Commands

The implementation should distinguish between:

### Queries

Read information without changing state.

### Commands

Request a state change.

Conceptually:

```text
Query
 ↓
Read
```

and:

```text
Command
 ↓
Domain Behaviour
 ↓
Persist
```

This distinction improves clarity without requiring a heavyweight CQRS framework.

---

# CQRS

DiaperScout may use CQRS principles without requiring separate databases or services.

CQRS should be interpreted as:

> **Queries and commands have different responsibilities.**

It does not require:

* event sourcing;
* separate read databases;
* separate microservices;
* a message bus for every operation.

The initial implementation should remain straightforward.

---

# Commands

Commands should:

1. validate application-level input;
2. load the required domain state;
3. invoke domain behaviour;
4. persist the resulting state;
5. publish relevant events where required.

Business invariants belong to the Domain.

Application validation should not replace domain validation.

---

# Transactions

A command that changes multiple related pieces of state should use an appropriate database transaction.

For example:

```text
Observation
   +
Evidence Metadata
   +
Workflow State
```

should be committed consistently where the domain requires atomicity.

The transaction boundary should normally correspond to the application operation.

---

# Transaction Size

Transactions should be as short as practical.

Do not hold database transactions open while performing:

* network calls;
* object-storage uploads;
* email delivery;
* long-running processing;
* external API calls.

Prefer:

```text
Persist local state
       ↓
Commit
       ↓
Perform asynchronous external work
```

where the workflow permits.

---

# External Side Effects

Database transactions and external side effects must not be assumed to be atomic together.

For example:

```text
Database
    +
Cloudflare R2
```

cannot normally be treated as one transaction.

Where consistency matters, use durable state and appropriate asynchronous processing.

---

# Outbox Pattern

An Outbox mechanism should be introduced where reliable event publication is required.

Conceptually:

```text
Database Transaction
 ├── Domain State
 └── Outbox Event
          ↓
       Commit
          ↓
   Background Processor
          ↓
   External Consumer
```

This prevents the system from committing domain state while losing the corresponding event because of a transient messaging failure.

The Outbox should be introduced when event-driven workflows require guaranteed delivery.

It should not be implemented merely for architectural fashion.

---

# Domain Events

Domain events describe meaningful changes in domain state.

Examples include:

* Observation Submitted;
* Editorial Decision Recorded;
* Product Published;
* Discovery Task Completed.

Domain events may be persisted through the Outbox where reliable external processing is required.

---

# Persistence Events vs Domain Events

A database row changing does not automatically constitute a domain event.

Avoid creating events such as:

```text
ProductRowUpdated
```

unless the event represents meaningful domain behaviour.

Prefer:

```text
ProductPublished
```

when that is the actual business event.

---

# Concurrency

The application should use optimistic concurrency where simultaneous updates are possible.

Important examples include:

* editorial review;
* Discovery Task state;
* Observation workflow;
* administrative configuration.

A concurrency token should be used where appropriate.

---

# Optimistic Concurrency

The preferred model is:

```text
Load Version 5
     ↓
User edits
     ↓
Attempt update
     ↓
Database contains Version 6
     ↓
Conflict
```

The application must not silently overwrite another user's changes.

The conflict should be surfaced and handled appropriately.

---

# Editorial Concurrency

Editorial operations are particularly sensitive.

Two Moderators must not be able to independently make conflicting decisions that silently overwrite each other.

The editorial workflow should use concurrency protection around the relevant state.

---

# Discovery Task Concurrency

Discovery Tasks may have multiple Users attempting to participate.

Where a Task has exclusive state transitions, those transitions must be concurrency-safe.

The system must prevent:

* duplicate exclusive claims;
* invalid state transitions;
* conflicting completion;
* stale client updates.

---

# Pagination

All potentially large collections must be paginated.

Examples include:

* Products;
* Locations;
* Observations;
* Discovery Tasks;
* editorial queues;
* contribution history.

The API must never require clients to retrieve an unbounded collection.

---

# Pagination Strategy

The initial implementation may use offset pagination where appropriate.

For very large or frequently changing datasets, keyset/cursor pagination should be preferred where it provides meaningful performance or consistency benefits.

The chosen strategy should be explicit in the API contract.

---

# Sorting

Paginated queries must use deterministic ordering.

For example:

```text
ORDER BY CreatedAt DESC, Id DESC
```

A unique secondary key should be used where necessary to ensure stable ordering.

---

# Filtering

Filtering should occur in PostgreSQL whenever practical.

Examples include:

```text
ProductType
Brand
Manufacturer
Location
Country
ObservationDate
WorkflowState
```

Do not retrieve a large unfiltered dataset and perform ordinary filtering in application memory unless there is a deliberate reason.

---

# PostgreSQL

PostgreSQL is the production relational database.

The implementation should take advantage of PostgreSQL where doing so provides meaningful value.

Examples include:

* appropriate indexes;
* constraints;
* JSONB where justified;
* full-text capabilities where appropriate;
* geographic capabilities where required;
* PostgreSQL-native data types where they improve correctness.

Provider-specific features should remain inside Infrastructure.

---

# Database Constraints

Important invariants should be enforced at both:

* Domain/application level;
* database level where appropriate.

Examples include:

* unique identifiers;
* foreign-key relationships;
* required fields;
* uniqueness constraints;
* valid relationship cardinality.

Database constraints provide a final defence against invalid persistence.

They do not replace domain rules.

---

# Indexes

Indexes should be introduced based on real query patterns.

Important candidates may include:

* GTIN;
* Product identifiers;
* Location identifiers;
* Observation timestamps;
* workflow states;
* foreign keys;
* search/filter fields.

Indexes should be reviewed as part of schema changes.

Avoid indexing every column automatically.

---

# Unique Constraints

Business identifiers that must be unique should have database-level uniqueness protection.

For example:

```text
GTIN
```

where the domain specification establishes that uniqueness.

The exact uniqueness semantics must follow the Product model.

---

# Foreign Keys

Relationships between persisted entities should use appropriate database foreign keys.

Foreign keys protect referential integrity.

Where the Domain permits historical retention after deletion, the database relationship should reflect that requirement rather than blindly cascading deletes.

---

# Deletion

Deletion must be deliberate.

Canonical Atlas information and historical Observations may require retention for provenance.

Do not use cascading deletion as the default strategy for historically significant information.

Where appropriate, use:

* soft deletion;
* archival state;
* anonymisation;
* immutable historical records.

The correct approach depends on the domain concept.

---

# Authentication Data

Authentication persistence is Infrastructure-owned.

Authentication-related tables should not be treated as ordinary domain aggregates.

Identity data must remain separated from community contribution data.

Deleting a User account must not accidentally destroy required historical provenance.

---

# Media Metadata

The database stores media metadata rather than large binary assets.

Metadata may include:

* media identifier;
* object-storage key;
* content type;
* dimensions;
* size;
* checksum;
* creation time;
* provenance;
* processing state.

The binary object remains in object storage.

---

# Object Storage Consistency

Object storage operations should be treated as external side effects.

For example:

```text
Create Evidence Metadata
       ↓
Commit
       ↓
Upload / process media
       ↓
Update processing state
```

The exact sequence should be selected to ensure abandoned or failed uploads can be detected and cleaned up.

---

# Background Data Operations

Long-running data operations should not run inside ordinary HTTP requests when they can reasonably be asynchronous.

Examples include:

* bulk media processing;
* reindexing;
* availability recalculation;
* large imports;
* cleanup;
* Community Trust recalculation.

These operations should use durable job state where appropriate.

---

# Bulk Operations

Bulk operations must be designed deliberately.

Avoid loading hundreds of thousands of entities into memory simply to update them one by one.

Where safe, use:

* database-side operations;
* batched processing;
* bulk SQL;
* EF Core bulk capabilities where appropriate.

Any bulk operation must preserve domain invariants.

If bypassing normal domain behaviour is required, the operation needs explicit architectural review.

---

# Migrations

EF Core migrations are the authoritative mechanism for evolving the application schema.

Migrations live in:

```text
DiaperScout.Infrastructure/Persistence/Migrations/
```

Every schema change should:

1. change the persistence model;
2. generate a migration;
3. review the generated migration;
4. test the migration;
5. verify upgrade behaviour;
6. deploy through the normal release process.

---

# Migration Safety

Production migrations must be reviewed for:

* destructive operations;
* locking behaviour;
* table rewrites;
* index creation;
* data migration requirements;
* backward compatibility.

Large production migrations may need to be split into multiple releases.

---

# Seed Data

Seed data should be limited to information required for the application to function.

Examples may include:

* required system configuration;
* development data;
* carefully defined reference data.

Production Atlas data should not be treated as ordinary EF Core seed data.

---

# Development Database

Developers should be able to create a clean local PostgreSQL environment through the development infrastructure.

The development environment should use the same relational technology as production.

An in-memory database must not be treated as a substitute for PostgreSQL integration testing.

---

# Testing

Persistence tests should use real PostgreSQL where database behaviour matters.

Important areas include:

* EF Core mappings;
* constraints;
* indexes;
* migrations;
* transactions;
* concurrency;
* PostgreSQL-specific queries.

SQLite or in-memory substitutes may be used for narrowly scoped tests where provider-specific behaviour is irrelevant, but they must not be the primary persistence test environment.

---

# Test Database Isolation

Integration tests must use isolated database state.

Suitable approaches include:

* disposable PostgreSQL containers;
* dedicated test databases;
* transaction isolation where appropriate.

Tests must not depend on developer machine state.

---

# Query Testing

Important read queries should be tested against realistic data volumes where performance matters.

Tests should verify:

* filtering;
* pagination;
* sorting;
* projection;
* expected query behaviour.

Performance testing should be introduced for genuinely important query paths rather than every query.

---

# N+1 Prevention

The implementation must avoid accidental N+1 query patterns.

Particular care is required when loading:

* Product hierarchies;
* Locations and Observations;
* editorial queues;
* Backpack collections.

Use appropriate projections or explicit loading strategies.

Do not enable broad lazy loading merely to hide data-access design problems.

---

# Lazy Loading

Lazy loading should not be enabled by default.

Explicit data requirements are preferred because they make:

* query behaviour;
* performance;
* transaction boundaries;

easier to understand.

---

# Connection Management

Database connections are managed by EF Core/Npgsql.

The application should rely on connection pooling rather than manually maintaining long-lived connections.

Connections should be released promptly after operations complete.

---

# Resilience

Transient database failures should be handled using appropriate provider/framework resilience mechanisms where safe.

Retries must not be applied blindly to operations with non-idempotent side effects.

Database retry behaviour should be configured centrally rather than duplicated across features.

---

# Read/Write Separation

A separate read database is not required initially.

The initial architecture uses:

```text
PostgreSQL
   ↑
Reads + Writes
```

Read models may be introduced later where scale or query complexity justifies them.

---

# Caching

Caching is not a substitute for correct persistence.

Suitable read-heavy Atlas data may be cached.

Cached data must have an explicit invalidation or freshness strategy.

User-specific data must use appropriate isolation.

---

# Data Integrity

The application must preserve:

* provenance;
* Observation history;
* Editorial history;
* User attribution;
* Product relationships;
* Location relationships.

Data-access shortcuts must not compromise these requirements.

---

# Privacy

Persistence must minimise unnecessary personal information.

Personal data should have:

* defined ownership;
* appropriate access controls;
* retention requirements;
* deletion/anonymisation behaviour.

Private User information must not appear in public query projections.

---

# Security

Database access credentials must:

* come from environment/configuration;
* never be committed to source control;
* use least privilege;
* be rotated appropriately.

Application database accounts should not have unnecessary administrative privileges.

---

# Performance Principles

Optimise based on evidence.

The preferred order is:

```text
Correct query
   ↓
Correct index
   ↓
Correct projection
   ↓
Correct caching
   ↓
More advanced optimisation
```

Do not introduce a distributed database architecture before the application demonstrates a real requirement.

---

# Data Access Anti-Patterns

The following should not be introduced without explicit architectural review.

## Generic Repository

Do not wrap every `DbSet` in a generic repository.

## Generic Unit of Work

Do not create a second unit-of-work abstraction around `DbContext`.

## DbContext Everywhere

Do not inject `DbContext` throughout Web, API and Domain code.

## Lazy Loading Everywhere

Do not rely on lazy loading to discover data requirements.

## In-Memory Production Logic

Do not load large datasets into memory merely to perform database work.

## Database-as-Domain

Do not allow database schema structure to dictate the Domain model.

## Domain-Aware EF Core

Do not make Domain entities responsible for persistence.

## In-Memory Database as Truth

Do not use an in-memory database as evidence that PostgreSQL behaviour is correct.

---

# Implementation Summary

The DiaperScout data-access model is intentionally straightforward:

```text
Application
    │
    ▼
Persistence Abstractions
    │
    ▼
Infrastructure
    │
    ▼
EF Core / Npgsql
    │
    ▼
PostgreSQL
```

Queries favour efficient projections.

Commands load domain state where domain behaviour is required.

Transactions remain short and explicit.

Concurrency is protected where multiple actors can update the same state.

Events use an Outbox where reliable delivery is genuinely required.

PostgreSQL remains the authoritative relational store.

---

# Relationship to Other Documents

This document defines how application data is persisted and retrieved.

Related documents include:

* **Database Model** — defines the persistent domain model.
* **Solution Structure** — defines project boundaries.
* **Project Layout** — defines physical source layout.
* **Domain Model** — defines business behaviour.
* **Backend Services** — defines logical service ownership.
* **Workflow Architecture** — defines state transitions.
* **Implementation Overview** — defines the implementation strategy.
* **Testing Strategy** — defines persistence verification.
* **Security** — defines database and data protection requirements.

---

# Final Principle

Data access should be **boring, explicit and correct**.

The application should not hide PostgreSQL behind layers of abstractions simply because architecture diagrams look prettier that way.

The important boundary is:

> **Domain behaviour belongs in Domain.
> Application orchestration belongs in Application.
> Persistence belongs in Infrastructure.
> PostgreSQL stores the truth.**

Everything else should serve those boundaries.
