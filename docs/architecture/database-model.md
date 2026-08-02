# Database Model

## Purpose

This document describes how the DiaperScout domain is represented within persistent storage.

It defines the principles governing how information is organised, preserved and related.

It intentionally avoids prescribing any particular database technology or schema implementation.

The database exists to preserve the Atlas, the evidence that supports it and the history of how knowledge evolves.

---

# Philosophy

The database is not the Atlas.

The Atlas is the published representation of knowledge.

The database contains considerably more information than is presented to users.

It preserves:

- canonical knowledge;
- observations;
- evidence;
- editorial decisions;
- provenance;
- historical records.

The database exists to support the Atlas rather than define it.

---

# Design Principles

The persistent model should:

- mirror the real world;
- preserve historical knowledge;
- maintain referential integrity;
- support continuous evolution;
- remain understandable;
- avoid unnecessary duplication.

The conceptual model should remain stable even if physical storage changes.

---

# Information Domains

Persistent information naturally separates into several distinct domains.

## Atlas

Stores published canonical knowledge.

Examples include:

- Products
- Product Specifications
- Manufacturers
- Canonical Images
- Regional Variations

This information represents the current published Atlas.

---

## Observations

Stores evidence contributed by the community.

Examples include:

- Product discoveries
- Retail observations
- Correction requests
- Manufacturer submissions

Observations describe reality.

They do not directly modify canonical knowledge.

---

## Evidence

Stores supporting material for observations.

Examples include:

- photographs;
- barcode images;
- manufacturer documentation;
- written notes;
- timestamps;
- locations.

Evidence always belongs to an Observation.

---

## Editorial

Stores editorial activity.

Examples include:

- review decisions;
- editorial history;
- provenance;
- reviewer comments.

Editorial records explain how the Atlas evolved.

---

## Community

Stores contributor information.

Examples include:

- users;
- roles;
- trust;
- Scout history.

Community information supports stewardship of the Atlas.

---

## Retail

Stores information about the retail world.

Examples include:

- retailers;
- countries;
- locations.

Retail availability itself is derived from observations rather than directly maintained.

---

# Identity

Every persistent entity should possess a permanent immutable identifier.

Identifiers should never change during the lifetime of an entity.

Where appropriate, human-readable identifiers should exist alongside immutable internal identifiers.

Examples include:

- product slugs;
- manufacturer slugs;
- retailer slugs.

Identifiers exist for computers.

Names exist for people.

---

# Relationships

Relationships should mirror reality.

Examples include:

- Manufacturers produce Products.
- Products possess Product Specifications.
- Products possess Barcodes.
- Retailers stock Products.
- Observations describe Products.
- Evidence supports Observations.
- Editorial Decisions evaluate Observations.

Relationships should remain explicit.

Hidden or implicit relationships should be avoided wherever practical.

---

# Ownership

Entities naturally own related information.

For example:

A Product owns:

- Product Specification;
- Canonical Images;
- Regional Variations;
- Barcodes.

An Observation owns:

- Evidence;
- Workflow State.

Ownership boundaries should remain clear throughout the implementation.

---

# Historical Preservation

Knowledge evolves.

The persistent model should preserve meaningful history.

Examples include:

- previous Product Specifications;
- editorial history;
- provenance;
- observation history.

Historical records explain how the Atlas reached its current understanding.

Operational mistakes corrected immediately should not unnecessarily clutter historical records.

---

# Immutability

Observations represent historical evidence.

Once recorded, they should normally remain unchanged.

Editorial decisions determine how observations influence the Atlas.

This preserves the integrity of the evidence while allowing the Atlas to evolve.

---

# Derived Information

Some information is calculated rather than entered directly.

Examples include:

- retailer confidence;
- Scout Task generation;
- stale observation detection;
- availability summaries.

Derived information should always be reproducible from underlying evidence.

Derived values should never become the authoritative source of truth.

---

# Referential Integrity

Relationships should remain valid throughout the lifetime of the Atlas.

The architecture should prevent accidental orphaning of important knowledge.

Deleting information should not compromise historical understanding.

Where removal is required, anonymisation should normally be preferred over destructive deletion.

---

# Schema Evolution

The persistent model should support continuous evolution.

Schema changes should favour:

- expansion before replacement;
- backwards compatibility where practical;
- gradual migration;
- eventual retirement of obsolete structures.

The objective is evolutionary change rather than disruptive redesign.

---

# Performance

Performance optimisation should never compromise the conceptual model.

Where optimisation is required, preference should normally be given to:

- indexing;
- caching;
- derived views;
- background processing.

Optimisations should remain implementation details rather than influencing the domain model.

---

# Resilience

The persistent model should support recovery from failure.

Backups should preserve:

- canonical knowledge;
- observations;
- evidence;
- editorial history;
- provenance;
- media.

The objective is preserving the Atlas rather than merely recovering infrastructure.

---

# Evolution

The persistent model should evolve alongside the Domain Model.

Future implementation choices should strengthen the conceptual architecture rather than introducing unnecessary complexity.

The objective is a persistent model that remains understandable, trustworthy and maintainable throughout the lifetime of DiaperScout.

---

# Relationship to Other Documents

This document explains how DiaperScout persists information.

Related documents describe the architecture from different perspectives.

- **Domain Model** defines the concepts being stored.
- **Knowledge Architecture** explains how those concepts evolve.
- **Backend Services** defines the services responsible for managing them.
- **Deployment & Operations** describes how persistent storage is protected and maintained.

Together these documents define how knowledge is preserved while remaining independent of any specific database technology.