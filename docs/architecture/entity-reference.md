# Entity Reference

## Purpose

This document provides the canonical reference for every entity within the DiaperScout domain model.

Unlike the Domain Model, which explains the conceptual relationships between entities, this document describes each entity individually.

It acts as the bridge between the conceptual architecture and the eventual implementation.

Every entity should have a clearly defined purpose, ownership and lifecycle before implementation begins.

---

# Using this Document

Each entity is described using a consistent structure.

- Purpose
- Ownership
- Lifecycle
- Relationships
- Constraints
- Notes

This keeps the model understandable as the Atlas grows.

---

# Product

## Purpose

Represents a single identifiable diaper product within the Atlas.

Products are the primary published entity of DiaperScout.

## Ownership

Owned by:

- Atlas

Owns:

- Product Specification
- Atlas Media
- Regional Variations
- Barcodes

## Lifecycle

Created through:

- Accepted Product Discovery

Updated through:

- Editorial Decisions

Retired through:

- Editorial Decision

## Relationships

- Manufacturer
- Product Specification
- Barcode(s)
- Retail Availability
- Observation History

## Constraints

- Exists independently of retailers.
- Exists independently of observations.
- Never edited directly by contributors.

---

# Product Specification

## Purpose

Represents the current canonical description of a Product.

## Ownership

Owned by:

- Product

## Lifecycle

Created with Product.

Updated through editorial review.

Historical revisions may be retained.

## Relationships

- Product

## Constraints

- Only one current specification exists.
- Historical specifications remain traceable.

---

# Manufacturer

## Purpose

Represents the organisation responsible for producing Products.

## Ownership

Owned by:

- Atlas

## Relationships

- Products
- Manufacturer Observations

## Constraints

- Verification confirms identity.
- Verification does not grant editorial authority.

---

# Barcode

## Purpose

Represents a recognised product identifier.

## Ownership

Owned by:

- Product

## Relationships

- Product

## Constraints

- Identifies exactly one Product.
- Products may possess multiple Barcodes.

---

# Retailer

## Purpose

Represents an organisation that sells Products.

## Ownership

Owned by:

- Retail Domain

## Relationships

- Countries
- Retail Observations

## Constraints

- Does not directly own Products.
- Availability is evidence-driven.

---

# Observation

## Purpose

Represents a factual report submitted by a contributor.

## Ownership

Owned by:

- Observation Domain

Owns:

- Evidence
- Workflow State

## Lifecycle

Created by contributor.

Reviewed editorially.

Never becomes canonical directly.

## Relationships

- Product
- User
- Evidence
- Editorial Decision

## Constraints

- Immutable once submitted.
- Represents historical evidence.

---

# Evidence

## Purpose

Supports an Observation.

## Ownership

Owned by:

- Observation

## Relationships

- Observation

## Constraints

- Never belongs directly to Products.
- Preserved for provenance.

---

# Editorial Decision

## Purpose

Represents the outcome of editorial review.

## Ownership

Owned by:

- Editorial Domain

## Relationships

- Observation
- Moderator

## Constraints

- Required before Atlas changes.

---

# User

## Purpose

Represents a contributor.

## Ownership

Owned by:

- Community

## Relationships

- Observations
- Roles
- Trust

---

# Role

## Purpose

Represents responsibility within DiaperScout.

## Relationships

- User

## Constraints

- Responsibility rather than status.

---

# Scout Task

## Purpose

Represents an opportunity to improve the Atlas.

## Relationships

- Observation
- Product
- Retailer

## Constraints

- Never changes the Atlas directly.
- Completing a Scout Task always creates an Observation.

---

# Relationship to Other Documents

This document provides detailed reference information for each entity.

Related documents include:

- Domain Model
- Database Model
- Backend Services
- API Architecture

Together these documents describe what exists within DiaperScout and how those concepts are implemented.