# DiaperScout Product Specification

## Overview

The DiaperScout Product Specification defines how the Guide represents absorbent products.

It provides a stable, consistent foundation for product discovery by describing objective product characteristics, their relationships, and the principles that govern the data model.

The specification is intentionally independent of implementation details. It describes **what** the Guide records and **why**, not **how** it is stored.

The core Product Model is:

```text
Product
└── Product Variant
    └── Size Variant
```

Retail selling arrangements are represented separately from the core Product Model.

---

# Relationship to the World Documentation

The Product Specification implements the philosophy established by the DiaperScout World documentation.

Where implementation decisions and project philosophy ever conflict, the World documentation takes precedence.

In practice this means the Product Specification should always reinforce DiaperScout's core values:

- Discovery over shopping
- Facts over assumptions
- Respect over categorisation
- Objective information supported by Community Observations

---

# Purpose

DiaperScout is an explorer's guide to absorbent products.

Its purpose is to help Explorers discover, compare and learn about products from around the world through a combination of:

- Objective Product Specifications
- Community Observations
- Retail information
- Educational content

The Product Specification exists to ensure every product is documented consistently, accurately and objectively.

---

# Document Guide

## `data-model-principles.md`

Defines the philosophy behind the Product Specification.

Topics include:

- Objective versus subjective information
- Product-first modelling
- Manufacturer product information versus Retail information
- Discovery, comparison and reference
- Avoiding duplication
- Long-term maintainability

Read this document first.

---

## `product-model.md`

Defines the hierarchy used throughout the Guide.

```text
Product
└── Product Variant
    └── Size Variant
```

This document explains what information belongs at each level of the core model and how Retail information remains separate.

---

## `product-attributes.md`

The canonical Product Specification.

For every approved attribute it defines:

- Entity
- Data type
- Purpose
- Allowed values

This is the authoritative reference for the Product Specification.

---

## `community-observations.md`

Defines how subjective community knowledge complements the Product Specification.

Examples include:

- Reviews
- Ratings
- Photographs
- Fit notes
- Comparisons
- Real-world experiences

Community Observations intentionally remain separate from Product Specifications.

---

## `attribute-decision-log.md`

Records the reasoning behind accepted, rejected and deferred attribute proposals.

It preserves important design decisions and provides context for future discussions.

---

# Document Authority

The specification should generally be read in the following order.

1. `data-model-principles.md`
2. `product-model.md`
3. `product-attributes.md`
4. `community-observations.md`
5. `attribute-decision-log.md`

Where documents overlap, authority follows the same order.

---

# Guiding Philosophy

The Guide records objective facts about products.

Retail information records how those products are offered for sale.

Explorers contribute observations about using those products.

Neither replaces the other.

Together they create a richer understanding than any one layer could provide alone.

This distinction is fundamental to DiaperScout and should be preserved throughout the project.

---

# Contributing

When extending the Product Specification:

1. Read the Data Model Principles.
2. Verify the proposal aligns with the World documentation.
3. Ensure the information is objective.
4. Confirm the attribute belongs at the correct level of the Product Model.
5. Consider whether the information is better represented as Retail information or a Community Observation.
6. Record significant decisions in the Attribute Decision Log.

The goal is not to create the largest product catalogue.

The goal is to create the most trustworthy explorer's guide to absorbent products.

---

# Core Model

The core catalogue hierarchy is deliberately simple:

```text
Product
└── Product Variant
    └── Size Variant
```

Manufacturer-stated information such as the number of individual products supplied in a standard pack may be recorded at Size Variant level where applicable.

Retailers may sell the same product as individual samples, one manufacturer's pack, multiple packs together, or other retailer-defined presentations. These are Retail Offer arrangements and do not create new core catalogue entities.
