# Database Model

## Purpose

This document describes how the DiaperScout domain is represented within persistent storage.

It defines the principles governing how information is organised, preserved and related.

It intentionally avoids prescribing any particular database technology or physical schema implementation.

The database exists to preserve the Atlas, the evidence that supports it, the people contributing to it and the history of how knowledge evolves.

---

# Philosophy

The database is not the Atlas.

The Atlas is the published representation of knowledge.

The database contains considerably more information than is presented to users.

It preserves:

* canonical knowledge;
* observations;
* evidence;
* editorial decisions;
* provenance;
* community contribution history;
* discovery workflows;
* user and Explorer information;
* meaningful historical records.

The database exists to support the Atlas rather than define it.

---

# Design Principles

The persistent model should:

* mirror the real world;
* preserve historical knowledge;
* maintain referential integrity;
* support continuous evolution;
* remain understandable;
* avoid unnecessary duplication;
* preserve provenance;
* distinguish evidence from derived information;
* distinguish internal workflow data from public information.

The conceptual model should remain stable even if the physical storage technology changes.

---

# Information Domains

Persistent information naturally separates into several related domains.

## Atlas

Stores published canonical knowledge.

Examples include:

* Products;
* Product Specifications;
* Manufacturers;
* Brands;
* Product Variants;
* Size Variants;
* Pack Types;
* Canonical Media;
* Regional Variations;
* Product identifiers.

This information represents the current published Atlas.

---

## Community

Stores information about people participating in DiaperScout.

Examples include:

* Users;
* Explorer Profiles;
* Backpack information;
* contribution history;
* Community Trust;
* explicitly granted security responsibilities.

Community information supports contribution and stewardship of the Atlas.

Contributor is not a separate account role.

An Explorer becomes a Contributor through participation.

---

## Observations

Stores reports of real-world findings.

Examples include:

* Product Discoveries;
* Retail Observations;
* Correction Requests;
* Manufacturer Submissions.

Observations describe reality.

They do not directly modify canonical knowledge.

---

## Evidence

Stores material supporting Observations.

Examples include:

* photographs;
* barcode images;
* manufacturer documentation;
* written notes;
* measurements;
* packaging;
* timestamps;
* location information.

Evidence belongs to an Observation.

Evidence preserves the provenance of the information supplied by a Contributor.

---

## Editorial

Stores editorial activity.

Examples include:

* editorial reviews;
* Editorial Decisions;
* editorial history;
* provenance;
* review comments;
* requests for additional evidence.

Editorial records explain how the Atlas evolved.

---

## Discovery

Stores structured investigation of identified Knowledge Gaps.

Examples include:

* Knowledge Gaps;
* Discovery Tasks;
* task state;
* task outcomes;
* task provenance.

Discovery Tasks direct voluntary community effort towards useful gaps in the Atlas.

They do not represent a user role or rank.

---

## Retail

Stores information about the physical retail world.

Examples include:

* Retailers;
* Locations;
* Countries;
* regional markets.

Retail availability itself is derived from relevant Observations rather than directly maintained as a permanent stock state.

---

# Identity

Every persistent entity should possess a permanent immutable identifier where independent identity is required.

Identifiers should never change during the lifetime of an entity.

Where appropriate, human-readable identifiers should exist alongside immutable internal identifiers.

Examples include:

* product slugs;
* manufacturer slugs;
* brand slugs;
* retailer slugs;
* location identifiers.

Identifiers exist for systems.

Names exist for people.

---

# Core Relationships

Relationships should mirror reality.

Examples include:

* Manufacturers produce Products.
* Manufacturers may own Brands.
* Brands contain Products.
* Products contain Product Variants.
* Product Variants contain Size Variants.
* Size Variants are sold through Pack Types.
* Pack Types may have GTINs.
* Retailers operate Locations.
* Observations describe Products and, where relevant, Locations.
* Evidence supports Observations.
* Editorial Decisions evaluate Observations and Evidence.
* Accepted editorial outcomes may update the Atlas.
* Users have Explorer identities.
* Explorers may submit Observations.
* Knowledge Gaps may produce Discovery Tasks.
* Discovery Tasks may result in Observations and Evidence.

Relationships should remain explicit.

Hidden or implicit relationships should be avoided wherever practical.

---

# Product Model

The persistent Product Model follows the conceptual hierarchy:

```text id="e0c2qk"
Product
└── Product Variant
    └── Size Variant
        └── Pack Type
            └── GTIN
```

The database should preserve these distinctions.

A GTIN should not automatically be treated as the identity of the underlying Product.

A Product may have multiple:

* Product Variants;
* Size Variants;
* Pack Types;
* regional representations;
* identifiers.

---

# Retail Model

Retail information follows the principle:

```text id="l8n1w8"
Retailer
    │
    └── Location
           │
           └── Observation
                  │
                  └── Product
```

A Retailer does not directly own a permanent Product availability relationship.

Instead, DiaperScout records observations of Products at Locations.

This distinction is important because:

> **An Observation that a Product was seen at a Location is not a guarantee that the Product is currently in stock.**

Availability summaries are therefore derived information.

---

# Ownership

Entities naturally own related information.

For example:

A User owns or is associated with:

* Explorer Profile;
* Backpack;
* contribution history;
* Community Trust data.

A Product owns or is associated with:

* Product Specification;
* Product Variants;
* Regional Variations;
* canonical Product Media.

A Product Variant owns or is associated with:

* Size Variants;
* variant-specific attributes.

A Size Variant owns or is associated with:

* Pack Types.

A Pack Type may own or be associated with:

* GTINs.

An Observation owns:

* Evidence;
* Observation Media;
* contribution provenance;
* its editorial workflow state.

A Retailer operates:

* Locations.

A Knowledge Gap may produce:

* Discovery Tasks.

Ownership boundaries should remain clear throughout the implementation.

---

# Community Data

## User

The User represents the technical identity of an authenticated person.

User data may include:

* authentication state;
* account state;
* security information;
* Explorer Profile;
* Backpack;
* contribution relationships.

---

## Explorer

Explorer is the user-facing identity of the User.

It should not be implemented as a separate security role.

---

## Contributor

Contributor describes a User who has made one or more contributions.

Contributor status should normally be derived from contribution history rather than represented as an independent role.

---

## Community Trust

Community Trust is internal community data associated with a User.

It may support:

* contribution review;
* workflow prioritisation;
* moderation recommendations;
* Discovery Task recommendations.

Community Trust must not become a public reputation score.

It must not replace evidence or editorial review.

---

# Security Responsibilities

Security responsibilities such as:

* Moderator;
* Administrator;
* Verified Manufacturer;

are represented through the authentication and authorisation architecture.

They should not be confused with ordinary domain participation.

A User may have one or more explicitly granted security responsibilities where required.

Community Trust does not automatically grant those responsibilities.

---

# Historical Preservation

Knowledge evolves.

The persistent model should preserve meaningful history.

Examples include:

* previous Product Specifications;
* Editorial Decisions;
* provenance;
* Observation history;
* contribution history;
* Discovery Task outcomes.

Historical records explain how the Atlas reached its current understanding.

Operational mistakes corrected immediately should not unnecessarily clutter historical records.

---

# Observation Immutability

Observations represent historical evidence.

The factual content of an Observation should normally be treated as immutable after submission.

This protects the integrity of the evidence.

However, associated workflow metadata may change during its lifecycle.

Examples include:

* editorial state;
* review status;
* requests for additional evidence;
* moderation state.

Changes to workflow state must not silently rewrite the original observation or its evidence.

Where an Observation is found to be incorrect, the correction should normally be represented through an appropriate editorial or correction workflow rather than rewriting history.

---

# Evidence Integrity

Evidence must preserve provenance.

Evidence should identify:

* the Observation it supports;
* its contributor or source;
* when it was submitted;
* its relevant metadata;
* any subsequent editorial treatment.

Evidence should not be silently replaced in a way that destroys the historical record.

Where a correction is necessary, the system should preserve sufficient history to understand what happened.

---

# Derived Information

Some information is calculated rather than entered directly.

Examples include:

* Product availability summaries;
* observation freshness;
* Knowledge Gap detection;
* Discovery Task generation;
* community confidence;
* contribution summaries;
* location-level product discovery summaries.

Derived information should always be reproducible from underlying data wherever practical.

Derived values should never become the authoritative source of truth.

In particular:

> **Derived availability is not authoritative stock information.**

---

# Community Trust and Derived Data

Community Trust may be calculated or adjusted from underlying contribution and moderation information.

The authoritative inputs should remain available.

The system should avoid making an opaque numerical score the sole source of truth for contributor reliability.

Where practical, meaningful trust decisions should retain sufficient provenance to explain why the internal signal changed.

---

# Referential Integrity

Relationships should remain valid throughout the lifetime of the Atlas.

The architecture should prevent accidental orphaning of important knowledge.

Deleting information should not compromise historical understanding.

Where removal is required, anonymisation or controlled archival should normally be preferred over destructive deletion.

Special care is required for:

* Users;
* Observations;
* Evidence;
* Editorial history;
* Provenance;
* Product history.

---

# Deletion and Anonymisation

Privacy requirements may require information to be removed.

Where a User requests deletion, the system should distinguish between:

* personal information that must be removed;
* historical contribution information that may need to remain for Atlas integrity;
* information that can be anonymised rather than destroyed.

Anonymisation should preserve the usefulness and provenance of the Atlas without unnecessarily retaining personal information.

Deletion must not silently rewrite historical knowledge.

---

# Media Storage

Media may be stored separately from structured domain records.

The database should retain sufficient metadata to identify:

* ownership;
* source;
* Observation;
* Product where appropriate;
* media type;
* submission time;
* editorial state;
* storage location.

The binary media itself does not necessarily need to reside within the relational database.

The persistent model must nevertheless preserve its provenance and lifecycle.

---

# Schema Evolution

The persistent model should support continuous evolution.

Schema changes should favour:

* expansion before replacement;
* backwards compatibility where practical;
* gradual migration;
* explicit migration history;
* eventual retirement of obsolete structures.

The objective is evolutionary change rather than disruptive redesign.

Changes to the database model should follow changes to the Domain Model and relevant architecture decisions.

---

# Performance

Performance optimisation should never compromise the conceptual model.

Where optimisation is required, preference should normally be given to:

* indexing;
* caching;
* derived views;
* query optimisation;
* background processing;
* appropriate denormalisation where justified.

Optimisations should remain implementation details rather than changing the meaning of domain entities.

---

# Resilience

The persistent model should support recovery from failure.

Backups should preserve:

* canonical knowledge;
* Products;
* Product Specifications;
* Observations;
* Evidence metadata;
* editorial history;
* provenance;
* community contribution history;
* Discovery Task history;
* required media.

The objective is preserving the Atlas and the evidence behind it rather than merely recovering infrastructure.

---

# Auditability

Significant changes to canonical knowledge should be traceable.

The system should be able to answer:

* What changed?
* When did it change?
* Why did it change?
* Which evidence supported the change?
* Which editorial decision authorised it?
* What was the previous published state?

Auditability is particularly important for:

* Product Specifications;
* Product status;
* regional variations;
* editorial decisions;
* community trust changes;
* privileged account changes.

---

# Evolution

The persistent model should evolve alongside the Domain Model.

Future implementation choices should strengthen the conceptual architecture rather than introduce unnecessary complexity.

New persistent entities should only be introduced where they represent:

* an independently identifiable concept;
* an independently managed lifecycle;
* meaningful provenance;
* significant relationships;
* or a genuine persistence requirement.

The objective is a persistent model that remains understandable, trustworthy and maintainable throughout the lifetime of DiaperScout.

---

# Anti-Patterns

The following structures should not be introduced without explicit architectural review.

## Scout Data

No `Scout` table, identity or historical role.

---

## Trusted Explorer Role

Community Trust must not become an automatic privileged role.

---

## Contributor Role Table

Contributor should not become an authorisation role merely because someone has contributed.

---

## Permanent Stock State

Do not create an authoritative Product-to-Location stock state from observations.

---

## Public Trust Score

Do not expose internal Community Trust as a public reputation metric.

---

## Direct Observation-to-Atlas Updates

Observations must not directly overwrite canonical knowledge.

---

## Destructive History Rewrites

Do not silently rewrite historical evidence to make the current state appear cleaner.

---

# Relationship to Other Documents

This document explains how DiaperScout persists information.

Related documents describe the architecture from different perspectives.

* **Domain Model** defines the concepts being stored.
* **Entity Reference** defines the persistent entity inventory.
* **Knowledge Architecture** explains how knowledge evolves.
* **Authentication & Roles** defines identity and security responsibilities.
* **Community Contributions** explains how Explorers contribute.
* **Discovery Task System** explains how Knowledge Gaps are investigated.
* **Editorial Architecture** defines editorial processing.
* **Backend Services** defines the services responsible for managing persistent information.
* **Deployment & Operations** describes how persistent storage is protected and maintained.

Together these documents define how DiaperScout preserves knowledge, evidence and provenance while allowing the Atlas to evolve safely.
