# Domain Model

## Purpose

This document defines the conceptual model of DiaperScout.

The domain model describes the real-world concepts represented by the Atlas and the relationships between them.

It is intentionally independent of any particular programming language, database technology or implementation framework.

The objective is to provide a shared language that both technical and non-technical contributors can use when discussing the architecture.

---

# Philosophy

The domain model should describe the real world.

Entities exist because they represent meaningful concepts within the Atlas, not because they simplify implementation.

The architecture should favour concepts that contributors naturally understand.

The software should adapt to the domain rather than forcing the domain to adapt to the software.

---

# Domain Boundaries

The architecture naturally separates into several related domains.

These are conceptual responsibilities rather than deployment boundaries.

## Atlas

Responsible for the published Guide.

Contains:

- Products
- Product Specifications
- Manufacturers
- Canonical Images
- Regional Variations

The Atlas represents the current published understanding of the world.

---

## Community

Responsible for contributors.

Contains:

- Users
- Scouts
- Moderators
- Administrators
- Trust
- Roles

The Community improves and protects the Atlas.

---

## Observations

Responsible for collecting evidence.

Contains:

- Discoveries
- Retail Observations
- Correction Requests
- Manufacturer Submissions
- Supporting Evidence

Observations describe reality.

They do not directly modify the Atlas.

---

## Editorial

Responsible for transforming evidence into trusted knowledge.

Contains:

- Editorial Decisions
- Review History
- Provenance
- Editorial Comments

Editorial determines how the Atlas evolves.

---

## Retail

Responsible for representing the retail world.

Contains:

- Retailers
- Countries
- Locations

Retail availability is derived from observations rather than directly maintained.

---

# Core Entities

## Product

Represents a single identifiable diaper product.

A Product exists independently of:

- retailers;
- observations;
- pricing;
- availability.

The Product forms the centre of the Atlas.

---

## Product Specification

Represents the current canonical understanding of a Product.

Examples include:

- absorbency;
- backing;
- fastening system;
- wetness indicator;
- sizing.

The Product Specification is the published description of the Product rather than the evidence supporting it.

---

## Manufacturer

Represents the organisation responsible for producing Products.

Manufacturers contribute official observations.

They do not directly modify the Atlas.

---

## Barcode

Represents a recognised product identifier.

A barcode identifies a Product.

Multiple barcodes may exist where appropriate.

---

## Retailer

Represents an organisation that sells Products.

Retailers remain independent of Products.

Availability emerges from observations rather than direct editing.

---

## Country

Represents a geographical market.

Countries influence:

- product presentation;
- retailer availability;
- shipping destinations.

---

## Observation

Represents a factual report submitted by a contributor.

Examples include:

- product discovery;
- retailer observation;
- correction request;
- manufacturer submission.

Observations preserve evidence.

They do not become canonical knowledge until accepted through editorial review.

---

## Evidence

Represents supporting material attached to an Observation.

Examples include:

- photographs;
- barcode images;
- written notes;
- manufacturer documentation.

Evidence exists to support an Observation.

It never belongs directly to a Product.

---

## Editorial Decision

Represents the outcome of editorial review.

Possible outcomes include:

- accepted;
- rejected;
- deferred;
- additional evidence requested.

Editorial Decisions determine how the Atlas evolves.

---

## User

Represents a community contributor.

Users earn increasing trust through valuable contributions.

Users may assume different responsibilities over time.

---

## Role

Represents a contributor's responsibility within DiaperScout.

Examples include:

- Visitor
- Scout
- Moderator
- Administrator
- Verified Manufacturer

Roles describe responsibility rather than status.

---

## Scout Task

Represents an opportunity to improve the Atlas.

Examples include:

- confirming retailer availability;
- investigating conflicting evidence;
- revisiting stale observations;
- verifying correction requests.

Scout Tasks guide community effort towards areas where it provides the greatest benefit.

---

# Aggregate Ownership

Several entities naturally belong together.

For example:

A Product owns:

- Product Specification
- Canonical Images
- Regional Variations
- Barcodes

An Observation owns:

- Evidence
- Workflow State

Separating these ownership boundaries reduces accidental complexity and keeps responsibilities clear.

---

# Relationships

The following relationships describe the structure of the Atlas.

- Manufacturers produce Products.
- Products have Product Specifications.
- Products possess Barcodes.
- Retailers stock Products.
- Observations describe Products.
- Evidence supports Observations.
- Editorial Decisions evaluate Observations.
- Accepted Editorial Decisions update the Atlas.
- Users create Observations.
- Moderators record Editorial Decisions.
- Scout Tasks encourage new Observations.

These relationships should mirror reality wherever practical.

---

# Architectural Constraints

Several important constraints apply throughout the model.

## Products are independent.

Products exist regardless of where they are sold.

---

## Observations are evidence.

Observations describe reality.

They do not directly modify the Atlas.

---

## Evidence belongs to Observations.

Evidence should remain attached to the Observation that created it.

This preserves provenance.

---

## Availability is inferred.

Retail availability is derived from observations rather than maintained directly.

This ensures that availability reflects evidence rather than assumption.

---

## Responsibilities are earned.

Roles represent stewardship.

Greater responsibility is earned through trust and contribution.

---

# Evolution

The domain model should evolve alongside the Product Specification.

Future concepts should extend the existing language of the Atlas rather than introducing unnecessary abstraction.

The objective is to preserve a domain model that remains understandable, maintainable and faithful to the real world.

---

# Relationship to Other Documents

This document defines the concepts represented within DiaperScout.

Related documents describe how those concepts behave.

- **Knowledge Architecture** explains how knowledge evolves.
- **Backend Services** explains which services own each responsibility.
- **Database Model** explains how the model is persisted.
- **API Architecture** explains how clients interact with these concepts.

Together these documents provide a complete conceptual description of the Atlas.