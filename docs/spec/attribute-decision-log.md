# Attribute Decision Log

## Overview

The Product Specification has been developed through discussion, experimentation and careful evaluation.

This document records significant attribute decisions and the reasoning behind them.

Its purpose is to preserve design intent so that future contributors understand not only *what* was decided, but *why*.

The Decision Log should be updated whenever a significant modelling decision is made.

---

# Accepted Attributes

The following attributes have been accepted for inclusion within the Product Specification.

| Attribute | Level | Reason |
|----------|-------|--------|
| Manufacturer | Product | Identifies the organisation responsible for manufacture. |
| Brand | Product | Consumer-facing identity. |
| Product Name | Product | Official product name. |
| Product Type | Product | Essential for discovery and filtering. |
| Product Status | Product | Supports discontinued and historical products. |
| Backing Type | Product Variant | Significant construction difference. |
| Fastener Type | Product Variant | Fundamental product characteristic. |
| Wetness Indicator | Product Variant | Objective product feature. |
| Standing Leak Guards | Product Variant | Objective construction feature. |
| Inner Leak Guards | Product Variant | Objective construction feature. |
| Elastic Waistbands | Product Variant | Construction detail affecting comparison. |
| Print Design | Product Variant | Identifies visually distinct variants. |
| Number of Fasteners | Product Variant | Supports comparison between otherwise similar products. |
| Manufacturer Size | Size Variant | Manufacturer-defined sizing. |
| Waist Range | Size Variant | Essential sizing information. |
| Hip Range | Size Variant | Additional fit measurement where available. |
| Capacity | Size Variant | Published performance specification. |
| Product Dimensions | Size Variant | Objective physical measurements. |
| Product Weight | Size Variant | Objective reference information. |
| GTIN / Barcode | Size Variant | Identifies the relevant retail product for a specific size; different sizes commonly have different identifiers. |
| Quantity per Pack | Pack Type | Defines the packaging configuration. |
| Packaging Type | Pack Type | Describes the retail presentation. |

---

# Rejected Attributes

The following proposals have been intentionally excluded from the Product Specification.

These may still be valuable as Community Observations.

| Proposal | Reason |
|----------|--------|
| Comfort | Subjective experience. |
| Softness | Varies between individuals. |
| Quietness | Depends on environment and perception. |
| Discreteness | Interpretation rather than objective fact. |
| Confidence | Personal experience. |
| Value for Money | Depends on retailer and purchaser. |
| Best for Overnight | Usage recommendation rather than specification. |
| Good for Heavy Wetting | Interpretation of performance. |
| Popularity | Changes over time. |
| Community Rating | Community Observation. |
| Review Score | Community Observation. |
| Favourite Product | Community Observation. |

---

# Deferred Attributes

The following ideas remain under consideration.

They may become Product Specification attributes if future evidence demonstrates sufficient value.

| Proposal | Current Position |
|----------|------------------|
| SAP Percentage | Awaiting reliable manufacturer data. |
| Pulp Percentage | Awaiting consistent sources. |
| Core Construction Details | Requires further modelling. |
| Sustainability Metrics | Awaiting standardised definitions. |
| Manufacturing Facility | May become useful if reliable sources emerge. |

---

# Significant Design Decisions

## Product Specifications describe products.

Community Observations describe experiences.

This distinction is fundamental to the Guide.

---

## Unknown is better than incorrect.

Unknown values remain preferable to assumptions.

The Guide should never invent information simply to complete a Product Specification.

---

## Every fact has one home.

Each attribute belongs to one level of the Product Model.

Information should not be duplicated.

The Product hierarchy is:

```text
Product
└── Product Variant
    └── Size Variant
        └── Pack Type
```

---

## Product Variant is not Size Variant.

A Product Variant represents a meaningful version of a Product.

Examples may include manufacturer-defined versions such as:

- Plus
- Super
- Ultima
- Plastic-backed
- Cloth-backed
- Plain
- Printed
- Day
- Night

A Size Variant represents the physical size within that Product Variant.

For example:

```text
TENA Slip
├── Plus
│   ├── Small
│   ├── Medium
│   ├── Large
│   └── XL
├── Super
│   ├── Small
│   ├── Medium
│   ├── Large
│   └── XL
└── Ultima
    ├── Medium
    ├── Large
    └── XL
```

A difference in size does not create a new Product Variant.

---

## GTIN / Barcode belongs with Size Variant.

GTIN / Barcode is recorded at Size Variant level.

The reason is that the barcode commonly identifies the retail product for a particular physical size.

For example:

```text
TENA Slip Plus
├── Small  → GTIN A
├── Medium → GTIN B
├── Large  → GTIN C
└── XL     → GTIN D
```

A packaging configuration does not automatically create a new GTIN.

A case may consist of multiple retail packs, for example:

```text
Case
├── Bag of 10
├── Bag of 10
├── Bag of 10
└── Bag of 10
```

A case may not have its own GTIN.

Where a separate identifier genuinely exists for a packaging configuration, it should only be recorded when its meaning and scope can be reliably established.

---

## Pack Type describes packaging.

Pack Type describes how a Size Variant is packaged or presented for sale.

Examples include:

- Sample
- Pack
- Case

Pack Type should not be used to represent Product Variants or physical sizes.

Packaging changes should not create new Products or Product Variants merely because the outer packaging has changed.

---

## Model reality.

The Product Model reflects how products actually exist.

It does not simplify reality simply to make implementation easier.

---

## Objective information comes first.

Manufacturer information and verifiable specifications take priority over retailer descriptions and community interpretation.

---

## Community Observations complement the Product Specification.

Community knowledge strengthens the Guide without replacing objective facts.

---

# Evaluating Future Proposals

When considering a new attribute, ask:

- Is it objective?
- Can it be verified?
- Does it improve discovery?
- Does it improve comparison?
- Does it provide important reference information?
- Does it belong at the correct level of the Product Model?
- Could it instead be represented as a Community Observation?

Only attributes that provide lasting value should become part of the Product Specification.

---

# Living Document

The Decision Log is expected to evolve.

New decisions should be added rather than replacing historical reasoning.

Preserving previous discussions helps future contributors understand how the Product Specification reached its current form and avoids revisiting the same questions repeatedly.
