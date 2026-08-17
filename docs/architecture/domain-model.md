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

Not every useful concept needs to become a persistent entity.

Some concepts describe behaviour, responsibility or state rather than things that exist independently.

---

# Domain Boundaries

The architecture naturally separates into several related domains.

These are conceptual responsibilities rather than deployment boundaries.

## Atlas

Responsible for the published Guide.

Contains:

* Products
* Product Specifications
* Manufacturers
* Canonical Images
* Regional Variations

The Atlas represents the current published understanding of the world.

---

## Community

Responsible for people participating in DiaperScout.

Contains:

* Users
* Explorer identities
* Contributors
* Community Trust
* Moderators
* Administrators
* Verified Manufacturers

The Community contributes to and protects the Atlas.

Contributor is a description of participation rather than a separate account type or security role.

---

## Observations

Responsible for collecting evidence about the real world.

Contains:

* Product Discoveries
* Retail Observations
* Correction Requests
* Manufacturer Submissions
* Supporting Evidence

Observations describe reality.

They do not directly modify the Atlas.

---

## Editorial

Responsible for transforming evidence into trusted knowledge.

Contains:

* Editorial Decisions
* Review History
* Provenance
* Editorial Comments

Editorial determines how the Atlas evolves.

---

## Retail

Responsible for representing the physical retail world.

Contains:

* Retailers
* Locations
* Countries
* Regional markets

Retail availability is derived from observations rather than directly maintained as a stock state.

---

## Discovery

Responsible for identifying and resolving useful gaps in the Atlas.

Contains:

* Knowledge Gaps
* Discovery Tasks
* Task outcomes

Discovery Tasks direct voluntary community effort towards areas where new evidence could improve the Atlas.

---

# Core Entities

## Product

Represents a single identifiable absorbent product.

A Product exists independently of:

* retailers;
* locations;
* observations;
* pricing;
* availability.

The Product forms the centre of the Atlas.

---

## Product Specification

Represents the current canonical understanding of a Product.

Examples include:

* absorbency;
* backing;
* fastening system;
* wetness indicator;
* sizing.

The Product Specification is the published description of the Product rather than the evidence supporting it.

---

## Manufacturer

Represents the organisation responsible for producing Products.

Manufacturers may contribute official information through verified representatives.

They do not directly modify the Atlas.

---

## Barcode

Represents a recognised product identifier.

A barcode identifies a specific retail representation of a Product or Pack Type as defined by the Product Model.

Multiple barcodes may exist where appropriate.

---

## Retailer

Represents an organisation that sells Products.

A Retailer may operate one or more Locations.

Retailers remain independent of Products.

A Retailer does not directly establish that a Product is currently available.

Availability is evidenced through observations.

---

## Location

Represents a physical place where Products may be observed.

Examples include:

* Retail stores
* Pharmacies
* Supermarkets
* Specialist shops
* Other publicly accessible locations

A Location may belong to a Retailer.

A Location may also exist independently where no Retailer relationship is appropriate.

Observations are associated with Locations when the physical place is relevant to the observation.

---

## Country

Represents a geographical market.

Countries may influence:

* product presentation;
* regional variations;
* retailer availability;
* shipping destinations;
* packaging;
* product specifications.

---

## Observation

Represents a factual report submitted by an Explorer or other authorised contributor.

Examples include:

* Product Discovery
* Retail Observation
* Correction Request
* Manufacturer Submission

Observations preserve evidence.

They do not become canonical knowledge until accepted through editorial review.

---

## Evidence

Represents supporting material attached to an Observation.

Examples include:

* photographs;
* barcode images;
* written notes;
* measurements;
* manufacturer documentation;
* packaging;
* other independently verifiable material.

Evidence exists to support an Observation.

Evidence does not belong directly to a Product.

---

## Editorial Decision

Represents the outcome of editorial review.

Possible outcomes include:

* accepted;
* rejected;
* deferred;
* additional evidence requested.

Editorial Decisions determine whether and how the Atlas evolves.

---

## User

Represents the technical identity of an authenticated person using DiaperScout.

A User may participate in the community as an Explorer and may become a Contributor through submitting information.

User is an implementation and security concept rather than the preferred user-facing term.

---

## Explorer

Represents the user-facing identity of a person using DiaperScout.

Every authenticated User is an Explorer from the perspective of the experience.

Explorers may:

* browse the Atlas;
* discover Products;
* explore Locations;
* save Products and Locations;
* maintain a Backpack;
* submit Observations;
* contribute Evidence.

Explorer is not a security role.

---

## Contributor

Represents an Explorer who contributes information to DiaperScout.

Contributor is not:

* a security role;
* an account type;
* a rank;
* a level;
* a permission boundary.

A Contributor may submit:

* Observations;
* Evidence;
* corrections;
* Product Discoveries;
* Location information;
* other approved community contributions.

The Contributor concept describes activity and provenance rather than access control.

---

## Community Trust

Represents an internal assessment of an Explorer's demonstrated reliability as a Contributor.

Community Trust may consider:

* accuracy of previous contributions;
* evidence quality;
* consistency;
* correction history;
* successful resolution of Knowledge Gaps;
* malicious or abusive behaviour;
* long-term stewardship.

Community Trust is an internal signal.

It is not:

* a public score;
* XP;
* karma;
* a leaderboard position;
* a popularity metric;
* a security role.

Community Trust may support moderation and workflow decisions but never replaces evidence or editorial review.

---

## Moderator

Represents a person explicitly granted responsibility for editorial stewardship of the Atlas.

Moderators may:

* review Observations;
* evaluate Evidence;
* make Editorial Decisions;
* maintain editorial consistency;
* protect the integrity of the Atlas.

Moderator is a genuine security and responsibility boundary.

Community Trust may inform recommendations for moderation but does not automatically grant Moderator access.

---

## Administrator

Represents a person responsible for the operational management of the DiaperScout platform.

Administrators may manage:

* infrastructure;
* configuration;
* deployments;
* platform security;
* user access;
* operational monitoring.

Administrator responsibility is separate from editorial authority wherever practical.

---

## Verified Manufacturer

Represents a manufacturer or authorised representative whose identity has been verified.

Verified Manufacturers may submit official information and supporting material.

Verification establishes identity.

It does not grant authority to bypass editorial review.

---

## Knowledge Gap

Represents an identified area where the Atlas lacks useful or sufficiently reliable information.

Examples include:

* incomplete Product Specifications;
* conflicting Observations;
* stale availability evidence;
* regional uncertainty;
* missing Products;
* unverified historical information.

Knowledge Gaps may be identified by editorial workflows, community contributions or automated analysis.

---

## Discovery Task

Represents a specific, actionable Knowledge Gap that an Explorer may investigate.

Examples include:

* confirming Product availability at a Location;
* investigating conflicting Evidence;
* verifying a regional variation;
* completing missing Product Specifications;
* investigating a reported Product;
* resolving a correction request.

Discovery Tasks guide voluntary community effort.

They do not represent a user role or rank.

---

# Aggregate Ownership

Several entities naturally belong together.

For example:

A Product owns:

* Product Specification;
* Canonical Images;
* Regional Variations;
* Product identifiers and related Product Model information.

An Observation owns:

* Evidence;
* workflow state;
* contribution provenance.

A Retailer owns or operates:

* Locations.

A User owns or is associated with:

* Explorer identity;
* contribution history;
* Backpack;
* Community Trust information.

Separating these ownership boundaries reduces accidental complexity and keeps responsibilities clear.

---

# Relationships

The following relationships describe the structure of the Atlas.

* Manufacturers produce Products.
* Products have Product Specifications.
* Products may have multiple identifiers.
* Retailers operate Locations.
* Products may be observed at Locations.
* Observations describe Products, Locations or other real-world facts.
* Evidence supports Observations.
* Editorial Decisions evaluate Observations and Evidence.
* Accepted Editorial Decisions may update the Atlas.
* Users participate as Explorers.
* Explorers may become Contributors through contribution.
* Contributors create Observations.
* Moderators record Editorial Decisions.
* Verified Manufacturers submit official information.
* Knowledge Gaps identify areas requiring additional information.
* Discovery Tasks provide actionable ways to investigate Knowledge Gaps.
* Community Trust provides an internal signal about contribution reliability.

These relationships should mirror reality wherever practical.

---

# Architectural Constraints

Several important constraints apply throughout the model.

## Products are independent

Products exist regardless of where they are sold.

---

## Observations are evidence

Observations describe reality.

They do not directly modify the Atlas.

---

## Evidence belongs to Observations

Evidence should remain attached to the Observation that created or supplied it.

This preserves provenance.

---

## Availability is inferred

Retail availability is derived from observations rather than maintained directly.

An Observation that a Product was seen at a Location does not constitute a guarantee of current stock.

---

## Contributor is not a role

Becoming a Contributor does not grant additional security permissions.

Contribution status is derived from participation and provenance.

---

## Community Trust is internal

Community Trust may support operational decisions but is not a public reputation system.

Trust never replaces Evidence or Editorial Review.

---

## Roles represent genuine responsibility

Moderator, Administrator and Verified Manufacturer represent real security or operational boundaries.

They are explicitly granted.

They are not automatically earned through activity.

---

## Discovery Tasks are voluntary

Discovery Tasks invite contribution.

Explorers cannot be penalised simply for declining or abandoning a task.

---

## Editorial authority is explicit

No community contribution directly becomes canonical knowledge.

The normal path remains:

```text
Observation
     ↓
Evidence
     ↓
Editorial Review
     ↓
Atlas
```

---

# Evolution

The domain model should evolve alongside the Product Specification and wider DiaperScout architecture.

Future concepts should extend the existing language of the Atlas rather than introducing unnecessary abstraction.

New entities should only be introduced when they represent meaningful concepts within the domain.

The objective is to preserve a domain model that remains:

* understandable;
* maintainable;
* evidence-based;
* faithful to the real world;
* consistent with the Guide's principles.

---

# Relationship to Other Documents

This document defines the concepts represented within DiaperScout.

Related documents describe how those concepts behave.

* **Terminology** defines the canonical language used throughout the project.
* **Glossary** provides shared definitions for project terminology.
* **Knowledge Architecture** explains how knowledge evolves.
* **Community Contributions** explains how Explorers contribute to the Guide.
* **Discovery Task System** explains how Knowledge Gaps can be investigated.
* **Authentication & Roles** explains identity, permissions and responsibility.
* **Backend Services** explains which services own each responsibility.
* **Database Model** explains how the model is persisted.
* **API Architecture** explains how clients interact with these concepts.

Together these documents provide a complete conceptual description of the Atlas.
