# Glossary

## Overview

This glossary defines the terminology used throughout the DiaperScout Product Specification.

These definitions provide a shared vocabulary for contributors and help ensure documentation remains consistent across the project.

Where terms are defined here, they should be used consistently throughout the specification.

---

# A

## Appearance

The visual characteristics of a Product Variant, such as colours, prints and artwork.

Appearance should describe observable characteristics only and should not include subjective opinions.

---

# B

## Backing Type

The material used on the outer surface of a product.

Examples include:

- Plastic
- Cloth
- Hybrid

Backing Type is a Product Variant attribute.

## Brand

The consumer-facing identity under which a product is sold.

A Brand may manufacture products itself or be owned by another organisation.

---

# C

## Capacity

The published absorbency capacity of a Size Variant.

Capacity should always be recorded as an objective Product Specification where available.

## Community Observation

A contribution describing an Explorer's real-world experience, opinion or discovery relating to a product.

Community Observations complement Product Specifications by providing knowledge that cannot be represented through objective attributes alone.

Examples include:

- Reviews
- Ratings
- Fit notes
- Comparisons
- Photographs
- Availability discoveries

## Community Observations

The collection of Community Observation content associated with a product.

Community Observations are intentionally separate from Product Specifications.

## Controlled Vocabulary

A predefined set of permitted values for an attribute.

Controlled vocabularies improve consistency, simplify filtering and reduce ambiguity throughout the Guide.

Examples include:

- Product Type
- Backing Type
- Fastener Type
- Packaging Type
- Product Status

---

# E

## Explorer

A member of the DiaperScout community who discovers, documents and shares knowledge about absorbent products.

Explorers contribute through Product Specifications, Community Observations or both.

---

# F

## Fastener Type

The mechanism used to secure a product.

Examples include:

- Tape
- Hook & Loop
- Pull-up

Fastener Type is a Product Variant attribute.

---

# G

## Guide

The DiaperScout explorer's guide to absorbent products.

The Guide combines Product Specifications, Community Observations, retailer information and educational content to help Explorers discover and understand products.

## GTIN

Global Trade Item Number.

A unique identifier used to identify a retail product or pack.

GTINs normally belong to Pack Types rather than Products.

---

# L

## Landing Strip

The reinforced frontal area that allows repositionable tapes or hook-and-loop fasteners to attach securely.

Landing Strips are objective construction features.

## Latex Free

Indicates that a product is manufactured without natural latex.

This is an objective Product Specification attribute.

---

# O

## Objective

Information that can be independently verified and remains true regardless of who records it.

Objective information belongs in the Product Specification.

Examples include:

- Manufacturer
- Capacity
- Waist range
- Backing type

---

# P

## Pack Type

The lowest level of the Product Model.

A Pack Type describes how a Size Variant is sold.

Examples include:

- Pack of 10
- Pack of 14
- Case of 48

Pack Types describe packaging rather than products.

## Product

The highest level of the Product Model.

A Product represents the identity of a product regardless of its variants, sizes or packaging.

## Product Family

An optional grouping used by manufacturers to identify related products.

## Product Model

The hierarchical structure used by DiaperScout to organise Product Specifications.

```
Product
└── Product Variant
    └── Size Variant
        └── Pack Type
```

## Product Specification

The objective, authoritative description of a product maintained by DiaperScout.

A Product Specification records verifiable facts about a product and intentionally excludes subjective opinions and experiences.

## Product Status

Indicates whether a product is currently manufactured.

Examples include:

- Current
- Discontinued
- Prototype

## Product Type

The broad category describing how a product functions.

Examples include:

- Tape Brief
- Pull-up
- Pad
- Booster

## Product Variant

The second level of the Product Model.

A Product Variant represents a distinct version of a Product with meaningful differences in construction, appearance or features.

Examples include:

- Printed
- Plain
- Plastic-backed
- Cloth-backed

---

# S

## SAP

Super Absorbent Polymer.

The absorbent material responsible for retaining liquid within the product.

## Size Variant

The third level of the Product Model.

A Size Variant represents one physical size of a Product Variant.

Measurements such as waist range, capacity and dimensions belong here.

## Standing Leak Guards

Raised internal barriers designed to help reduce leakage.

Standing Leak Guards are objective construction features.

## Subjective

Information based on personal experience, interpretation or opinion.

Subjective information belongs in Community Observations rather than the Product Specification.

Examples include:

- Comfort
- Softness
- Quietness
- Confidence

---

# T

## Tape Count

The total number of fastening tapes fitted to a product.

For example:

- Two tapes
- Four tapes

Tape Count is an objective Product Variant attribute.

## Target Gender

The gender or genders identified by the manufacturer as the intended market for a product.

This attribute reflects manufacturer positioning rather than who may actually use the product.

---

# W

## Waist Range

The manufacturer-published waist measurement for a Size Variant.

Measurements should be recorded using centimetres.

## Wetness Indicator

A feature that changes appearance after becoming wet.

Wetness Indicators are objective Product Variant attributes.

---

# See Also

For further information, see:

- `docs/spec/data-model-principles.md`
- `docs/spec/product-model.md`
- `docs/spec/product-attributes.md`
- `docs/spec/community-observations.md`
- `docs/spec/attribute-decision-log.md`