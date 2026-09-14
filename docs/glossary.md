# DiaperScout Glossary

## Purpose

This glossary defines the terminology used throughout the DiaperScout project.

Unless otherwise stated, these definitions should be considered authoritative across all specifications, documentation and code.

The terms in this glossary should be used consistently when describing the Product Model, Product Specifications, Community Observations and related catalogue data.

---

# A

## ADL (Acquisition Distribution Layer)

An additional material layer placed beneath the topsheet that rapidly distributes liquid throughout the absorbent core.

An ADL improves acquisition speed and helps utilise the full absorbent capacity of the product.

ADL is an objective Product Variant attribute where manufacturer information is available.

---

## Appearance

The visual presentation of a Product Variant.

Examples include:

- Plain
- Printed
- Transparent

Appearance is distinct from Colour.

Appearance describes observable characteristics and should not include subjective opinions.

---

# B

## Backing Type

The material used for the outer surface of a product.

Typical values include:

- Plastic
- Cloth-like
- Hybrid

Backing Type is a Product Variant attribute.

---

## Brand

The consumer-facing identity under which a product is sold.

A Brand may manufacture products itself or be owned by another organisation.

Brand is a Product attribute.

---

# C

## Capacity

The manufacturer's published absorbent capacity of a Size Variant, measured in millilitres (mL).

Capacity is an objective Product Specification and should not be confused with real-world performance reported by users.

---

## Community Data

Information contributed by users based on personal experience, opinion or discovery.

Examples include:

- Comfort
- Softness
- Quietness
- Reviews
- Ratings
- Fit notes
- Comparisons
- Photographs
- Availability discoveries

Community Data is intentionally separate from Product Specifications.

---

## Community Observation

A contribution describing an Explorer's real-world experience, opinion or discovery relating to a product.

Community Observations complement Product Specifications by providing knowledge that cannot be represented through objective attributes alone.

---

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

Explorers may contribute Community Observations and, where permitted by the applicable workflow, product information.

---

# F

## Fastener Type

The mechanism used to secure a product.

Examples include:

- Adhesive Tape
- Hook & Loop
- Pull-up

Fastener Type is a Product Variant attribute.

---

## Fragrance

Whether a product includes a manufacturer-applied fragrance.

Typical values include:

- Unscented
- Scented

Fragrance is a Product Variant attribute.

---

# G

## GTIN

Global Trade Item Number.

A GTIN is a globally recognised identifier used to identify a specific trade item.

Within the DiaperScout Product Model, GTIN / Barcode is normally associated with the relevant Size Variant because different physical sizes of the same Product Variant commonly have different identifiers.

For example:

```text
TENA Slip Plus
├── Small  → GTIN A
├── Medium → GTIN B
├── Large  → GTIN C
└── XL     → GTIN D
```

A GTIN should only be recorded when the identifier can be reliably established.

A packaging configuration or case does not automatically have its own GTIN.

---

## Guide

The DiaperScout explorer's guide to absorbent products.

The Guide combines Product Specifications, Community Observations, retailer information and educational content to help Explorers discover and understand products.

---

# L

## Landing Strip

A reinforced frontal area designed to receive repositionable fastening tapes or hook-and-loop fasteners.

Landing Strips are objective construction features and are recorded at Product Variant level.

---

## Latex Free

Indicates whether a product is manufactured without natural rubber latex.

This is an objective Product Specification attribute.

Where the manufacturer does not provide reliable information, the value should remain Unknown.

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
- Fastener type
- GTIN / Barcode

---

# P

## Pack Type

The lowest level of the Product Model.

A Pack Type describes how a Size Variant is packaged or presented for sale.

Examples include:

- Sample
- Pack
- Case

A Pack Type describes packaging rather than creating a different Product or Product Variant.

A case may contain multiple retail packs and does not necessarily have its own GTIN / Barcode.

---

## Product

The highest level of the Product Model.

A Product represents the identity of an absorbent product regardless of its Product Variants, Size Variants or packaging.

Examples include:

- BetterDry
- NorthShore MegaMax
- TENA Slip

A Product may contain one or more Product Variants.

---

## Product Family

An optional grouping used by manufacturers to identify related products.

A Product Family may contain multiple Products.

---

## Product Model

The hierarchical structure used by DiaperScout to organise Product Specifications.

```text
Product
└── Product Variant
    └── Size Variant
        └── Pack Type
```

Each level has a distinct purpose.

---

## Product Specification

The objective, authoritative description of a product maintained by DiaperScout.

A Product Specification records verifiable facts about a product and intentionally excludes subjective opinions and experiences.

---

## Product Status

Indicates the lifecycle state of a Product.

Examples include:

- Current
- Discontinued
- Prototype

---

## Product Type

The broad functional classification of an absorbent product.

Examples include:

- Tape Brief
- Pull-Up
- Pad
- Booster
- Insert
- Belted Brief

Product Type is a Product attribute.

---

## Product Variant

A distinct, materially meaningful version of a Product.

A Product Variant answers:

> "Which version of this product is this?"

Examples include:

- Plus
- Super
- Ultima
- Plain
- Printed
- Plastic-backed
- Cloth-backed
- Day
- Night

For example:

```text
TENA Slip
├── Plus
├── Super
└── Ultima
```

Product Variants contain one or more Size Variants.

A difference in physical size does not create a new Product Variant.

A packaging difference does not create a new Product Variant.

---

# S

## SAP (Super Absorbent Polymer)

The absorbent polymer used within disposable absorbent products.

SAP absorbs and retains liquid within the absorbent core.

---

## Size Variant

A specific physical size of a Product Variant.

A Size Variant answers:

> "Which size is this?"

Examples include:

- Small
- Medium
- Large
- XL
- 3XL

A manufacturer may use its own sizing system, such as:

- Size 5
- Size 6
- Size 7

Each Size Variant may have its own:

- sizing information
- measurements
- capacity
- dimensions
- product weight
- GTIN / Barcode

A Size Variant belongs to one Product Variant.

---

## Standing Leak Guards

Raised internal barriers designed to help reduce leakage.

Standing Leak Guards are objective construction features and are recorded at Product Variant level.

---

## Subjective

Information based on personal experience, interpretation or opinion.

Subjective information belongs in Community Observations rather than the Product Specification.

Examples include:

- Comfort
- Softness
- Quietness
- Confidence
- Discreteness

---

# T

## Tape Count

The total number of fastening tapes fitted to a product.

Tape Count is an objective Product Variant attribute.

---

## Target Gender

The gender or genders identified by the manufacturer as the intended market for a product, where applicable.

This describes manufacturer positioning rather than who may actually use the product.

---

# W

## Waist Range

The manufacturer-published body measurement range for a Size Variant.

Unless otherwise specified, measurements are recorded in centimetres (cm).

---

## Wetness Indicator

A visual indicator that changes appearance after the product becomes wet.

Wetness Indicators are objective Product Variant attributes.

---

# Core Concepts

## Objective Data

Information that can be independently verified and remains true regardless of who records it.

Examples include:

- Manufacturer
- Product Type
- Backing Type
- Capacity
- Waist Range
- GTIN / Barcode

Objective data belongs within the Product Specification.

---

## Subjective Data

Information based on user experience or opinion.

Examples include:

- Quietness
- Comfort
- Softness
- Confidence

Subjective Data belongs within Community Observations rather than the Product Specification.

---

# Product Hierarchy

The DiaperScout data model is organised into four levels.

```text
Product
└── Product Variant
    └── Size Variant
        └── Pack Type
```

Each level stores information appropriate to that level.

For example:

```text
TENA Slip
│
├── Plus
│   ├── Small
│   ├── Medium
│   ├── Large
│   └── XL
│
├── Super
│   ├── Small
│   ├── Medium
│   ├── Large
│   └── XL
│
└── Ultima
    ├── Medium
    ├── Large
    └── XL
```

A Size Variant may then have one or more Pack Types describing how that size is packaged or sold.

---

# See Also

- `docs/spec/data-model-principles.md`
- `docs/spec/product-model.md`
- `docs/spec/product-attributes.md`
- `docs/spec/community-data.md`
- `docs/spec/attribute-decision-log.md`
