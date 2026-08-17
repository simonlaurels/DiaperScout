# Entity Reference

## Purpose

This document provides the authoritative reference for the entities represented within the DiaperScout production domain.

It translates the conceptual Domain Model into an implementation-oriented entity inventory without prescribing a particular database schema or persistence technology.

The Entity Reference should remain consistent with:

* `docs/world/04_Terminology.md`
* `docs/glossary.md`
* `docs/architecture/domain-model.md`
* `docs/architecture/authentication-and-roles.md`
* `docs/architecture/discovery-task-system.md`

Where an entity is described here, the description defines its architectural purpose.

---

# Entity Principles

Not every concept in the domain becomes an independent entity.

An entity should exist when it has one or more of the following characteristics:

* independent identity;
* persistent lifecycle;
* relationships with multiple other entities;
* meaningful provenance;
* independent authorisation requirements;
* independent editorial state.

Concepts that describe behaviour, classification or derived information should not automatically become entities.

In particular:

* **Explorer** is a user-facing identity over a User.
* **Contributor** describes contribution activity and is not a separate account entity.
* **Community Trust** is persisted as internal user/community data but is not a role.
* **Moderator**, **Administrator** and **Verified Manufacturer** represent security responsibilities rather than ordinary domain entities.
* **Discovery Task** is a persistent workflow entity because it has its own lifecycle and state.

---

# Identity & Community Entities

## User

**Purpose:** Technical identity of an authenticated person.

A User represents the account and security identity used by the application.

### Responsibilities

* Authentication
* Account state
* Session/security relationships
* Explorer identity
* Contribution ownership
* Backpack ownership
* Community Trust data

### Key Relationships

```text
User
 ├── Explorer Profile
 ├── Observations
 ├── Evidence
 ├── Backpack
 └── Community Trust
```

### Notes

A User does not need to have contributed anything.

A User is the technical representation of an Explorer.

---

## Explorer Profile

**Purpose:** User-facing identity and profile information.

The Explorer Profile contains information intended to support the Explorer experience without exposing unnecessary personal information.

### Examples

* Display name
* Profile image where supported
* Public biography where supported
* Journey information
* Public contribution summary

### Key Relationships

```text
User
  │
  └── Explorer Profile
```

Explorer Profile is not an authentication identity.

---

## Community Trust

**Purpose:** Internal signal describing demonstrated contributor reliability.

### Examples of contributing factors

* Accepted contributions
* Evidence quality
* Accuracy
* Correction history
* Successful Discovery Tasks
* Repeated low-quality submissions
* Malicious behaviour

### Key Relationships

```text
User
  │
  └── Community Trust
```

### Notes

Community Trust is:

* internal;
* non-public;
* not XP;
* not karma;
* not a leaderboard score;
* not a role;
* not authority.

It may support workflow and moderation decisions.

It never replaces evidence or editorial review.

---

# Product Entities

## Manufacturer

**Purpose:** Organisation responsible for producing Products.

### Key Relationships

```text
Manufacturer
  │
  └── Products
```

A Manufacturer may have one or more verified representatives.

Manufacturer identity is separate from Product ownership and editorial authority.

---

## Brand

**Purpose:** Consumer-facing identity under which Products are sold.

### Key Relationships

```text
Manufacturer
  │
  └── Brand
       │
       └── Products
```

A Brand may belong to a Manufacturer.

---

## Product

**Purpose:** Canonical identity of an individual absorbent product.

A Product is the central entity within the Atlas.

### Key Relationships

```text
Brand
  │
  └── Product
       ├── Product Variants
       ├── Regional Variations
       ├── Observations
       └── Product Specifications
```

A Product exists independently of:

* retailers;
* locations;
* current availability;
* pricing;
* community observations.

---

## Product Variant

**Purpose:** Distinct version of a Product with meaningful construction, appearance or feature differences.

Examples:

* Cloth-backed
* Plastic-backed
* Printed
* Plain

### Relationship

```text
Product
  │
  └── Product Variant
       └── Size Variants
```

---

## Size Variant

**Purpose:** Physical size of a Product Variant.

### Examples

* Small
* Medium
* Large
* Extra Large

Size-specific information belongs here.

Examples include:

* Waist Range
* Dimensions
* Capacity
* Size-specific identifiers

---

## Pack Type

**Purpose:** Describes how a Size Variant is packaged for sale.

Examples:

* Pack of 10
* Pack of 14
* Case of 48

### Relationship

```text
Size Variant
  │
  └── Pack Type
       └── GTIN
```

Pack Type describes packaging rather than the underlying Product.

---

## Product Specification

**Purpose:** Canonical, objective information describing a Product and its variants.

Product Specifications represent the current published understanding of a Product.

They are distinct from the evidence used to establish them.

---

## Regional Variation

**Purpose:** Represents a recognised difference between a Product's presentation or specification in different regions.

Examples include:

* Regional packaging
* Regional formulation
* Regional Product Variant
* Regional specification differences

Regional Variations should not be inferred merely from country-specific observations.

---

## GTIN

**Purpose:** Identifies a specific retail representation of a Product or Pack Type.

GTINs should be associated with the appropriate level of the Product Model.

A GTIN is not itself a Product.

---

# Retail Entities

## Retailer

**Purpose:** Organisation operating one or more retail locations.

### Relationship

```text
Retailer
  │
  └── Locations
```

A Retailer does not directly establish Product availability.

---

## Location

**Purpose:** Physical place represented within the Atlas.

A Location may be:

* a retail store;
* pharmacy;
* supermarket;
* specialist shop;
* another publicly accessible place where Products may be observed.

### Key Relationships

```text
Retailer
  │
  └── Location
       │
       ├── Observations
       └── Products Observed
```

A Location may exist without a known Retailer.

### Important Principle

The Atlas documents **places**, not businesses.

Retailer information provides context about a Location.

---

## Country

**Purpose:** Geographic market used for regional classification.

Countries may be associated with:

* Products;
* Regional Variations;
* Locations;
* Observations;
* availability evidence.

---

# Observation Entities

## Observation

**Purpose:** Records a real-world finding or report.

An Observation has independent provenance and lifecycle.

### Examples

* Product Discovery
* Retail Observation
* Correction Request
* Manufacturer Submission

### Key Relationships

```text
User
  │
  └── Observation
       ├── Product
       ├── Location
       ├── Evidence
       └── Editorial Decisions
```

An Observation does not directly modify canonical Product information.

---

## Evidence

**Purpose:** Supporting material associated with an Observation.

### Examples

* Photographs
* Barcode photographs
* Packaging
* Measurements
* Manufacturer documentation
* Written notes

### Relationship

```text
Observation
  │
  └── Evidence
```

Evidence retains provenance.

Evidence belongs to the Observation through which it was supplied rather than directly to the Product.

---

## Observation Media

**Purpose:** Media submitted as part of an Observation.

Examples:

* Product photographs
* Shelf photographs
* Barcode photographs
* Supporting documents

Observation Media is a type of Evidence where appropriate.

---

# Editorial Entities

## Editorial Decision

**Purpose:** Records the outcome of editorial review.

### Possible outcomes

* Accepted
* Rejected
* Deferred
* Additional Evidence Requested

### Relationship

```text
Observation
  │
  └── Editorial Decision
```

An Editorial Decision explains how an Observation was handled.

---

## Editorial Review

**Purpose:** Represents the process of evaluating an Observation and its Evidence.

An Editorial Review may involve:

* one or more Moderators;
* multiple pieces of Evidence;
* multiple Editorial Decisions;
* requests for additional Evidence.

Editorial Review is primarily a workflow concept.

It should not become a persistent entity unless the implementation requires an independent review lifecycle.

---

## Provenance

**Purpose:** Describes the origin and supporting history of canonical knowledge.

Provenance may reference:

* Observations;
* Evidence;
* Editorial Decisions;
* Manufacturer information;
* other authoritative sources.

Provenance is essential to understanding why the Atlas contains a particular fact.

---

# Discovery Entities

## Knowledge Gap

**Purpose:** Represents an identified area where the Atlas lacks sufficiently reliable or complete information.

### Examples

* Missing Product
* Missing specification
* Conflicting evidence
* Stale availability information
* Regional uncertainty

A Knowledge Gap may be created by:

* editorial workflows;
* community contributions;
* automated analysis.

---

## Discovery Task

**Purpose:** Provides a specific, actionable way to investigate a Knowledge Gap.

### Relationship

```text
Knowledge Gap
      │
      └── Discovery Task
             │
             └── Observation / Evidence
```

A Discovery Task has its own lifecycle and therefore exists as a persistent workflow entity.

### Important Principle

A Discovery Task does not represent a User role.

It does not create:

* Scout accounts;
* Explorer ranks;
* task-based permissions;
* public scores.

---

# Backpack Entities

## Backpack

**Purpose:** Represents an Explorer's personal journey through DiaperScout.

A Backpack may contain:

* saved Products;
* saved Locations;
* Collections;
* discoveries;
* drafts;
* Scrapbook content;
* Discovery Journal information.

### Relationship

```text
User
  │
  └── Backpack
```

The Backpack is personal to the Explorer.

---

## Collection

**Purpose:** A personal or curated grouping of Products, Locations or discoveries.

Collections may be:

* private;
* shared;
* editorial;
* system-generated.

Collection behaviour is defined by the relevant product specification.

---

## Scrapbook Item

**Purpose:** A record of meaningful recognition or a memorable part of an Explorer's journey.

Scrapbook Items are not points or ranks.

They should preserve the story of contribution rather than create competitive progression.

### Relationship

```text
Backpack
  │
  └── Scrapbook Items
```

---

# Security Responsibilities

The following concepts represent security responsibilities rather than ordinary domain entities.

## Moderator

A User explicitly granted moderation permissions.

Moderator status should be represented through the application's authorisation system.

It should not be inferred solely from Community Trust.

---

## Administrator

A User explicitly granted administrative permissions.

Administrator permissions belong to the platform security model.

---

## Verified Manufacturer

A verified organisation or representative authorised to submit official manufacturer information.

Verification should be represented through explicit account/organisation state.

Verification does not bypass editorial review.

---

# Entity Relationship Overview

The principal relationships can be represented as:

```text
User
 │
 ├── Explorer Profile
 ├── Backpack
 ├── Community Trust
 │
 └── Observations
       │
       ├── Evidence
       ├── Product ────────────┐
       ├── Location             │
       └── Editorial Decisions  │
                                │
Brand ── Manufacturer           │
 │                              │
 └── Product ◄──────────────────┘
      │
      ├── Product Variants
      │     └── Size Variants
      │            └── Pack Types
      │                   └── GTIN
      │
      └── Product Specification

Retailer
 │
 └── Location
       │
       └── Observations

Knowledge Gap
 │
 └── Discovery Task
       │
       └── Observation / Evidence
```

---

# Identity and Ownership Rules

## User Ownership

User owns or is associated with:

* authentication identity;
* Explorer Profile;
* Backpack;
* Observations;
* contribution provenance;
* Community Trust.

---

## Product Ownership

Product owns or is associated with:

* Product Specification;
* Product Variants;
* Regional Variations;
* Product identifiers;
* canonical Product Media.

---

## Observation Ownership

Observation owns or is associated with:

* Evidence;
* Observation Media;
* contribution provenance;
* editorial workflow state.

---

## Location Ownership

Retailers may operate Locations.

Locations remain independently identifiable because the Atlas is concerned with physical places rather than only the organisations operating them.

---

# Derived Concepts

The following should normally be derived rather than stored as independent authoritative entities.

## Contributor

Derived from an Explorer/User having submitted one or more contributions.

Contributor status does not require a separate account record.

---

## Products Observed at a Location

Derived from accepted or otherwise eligible Observations.

This should not be treated as a manually maintained Product-to-Location inventory.

---

## Retail Availability

Derived from relevant Observations.

An availability result must include appropriate evidence age and provenance.

It does not represent guaranteed live stock.

---

## Community Confidence

Where presented, confidence should be derived from Evidence, Observations and editorial state.

It should not simply represent a User's Community Trust.

---

# Anti-Patterns

The following structures should not be introduced into the production model without explicit architectural review.

### Scout

No `Scout` entity, role or identity.

### Trusted Scout

No replacement role such as `Trusted Explorer`.

Community Trust is an internal signal, not a rank.

### Contributor Role

Contributor is not an authorisation role.

### Product Stock Entity

Do not create a permanent stock state merely because an Observation exists.

### Public Trust Score

Do not expose Community Trust as a public reputation score.

### Automatic Moderator Promotion

Do not convert Community Trust into automatic privileged access.

### Direct Community-to-Atlas Updates

Do not allow Observations to directly mutate canonical knowledge.

---

# Evolution

New entities should be introduced only when they represent a genuine domain concept or an independently managed lifecycle.

Before introducing an entity, ask:

1. Does it have independent identity?
2. Does it have its own lifecycle?
3. Does it need independent provenance?
4. Does it participate in meaningful relationships?
5. Does it require independent authorisation?
6. Could it instead be represented as state, value data or a relationship?

If the answer to these questions is generally no, an independent entity may not be appropriate.

---

# Summary

The core production domain is built around:

```text
User
Explorer Profile
Backpack
Community Trust

Manufacturer
Brand
Product
Product Variant
Size Variant
Pack Type
Product Specification
Regional Variation
GTIN

Retailer
Location

Observation
Evidence
Editorial Decision
Provenance

Knowledge Gap
Discovery Task
```

The central flow remains:

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

And the central principle remains:

> **DiaperScout records discoveries about the real world, preserves the evidence behind them, and publishes trustworthy knowledge through editorial review.**
