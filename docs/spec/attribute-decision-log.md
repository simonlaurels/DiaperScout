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
| Manufacturer Pack Quantity | Size Variant | Manufacturer-stated quantity in the standard packaged product; may differ between sizes. |
| GTIN / Barcode | Size Variant | Identifies the relevant manufacturer trade item where this is the appropriate catalogue representation. |

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

The core Product hierarchy is:

```text
Product
└── Product Variant
    └── Size Variant
```

Retail selling arrangements are deliberately outside this hierarchy.

---

## Product Variant is not Size Variant.

A Product Variant represents a meaningful version of a Product.

A Size Variant represents the physical size within that Product Variant.

---

## Manufacturer Pack Quantity belongs with Size Variant.

Manufacturers commonly state the number of individual products supplied in the standard packaged product for a particular size.

This information is useful Product Specification data and may differ between sizes.

For example:

```text
TENA Slip Active Fit Maxi

Medium
└── Manufacturer Pack Quantity: 24

Large
└── Manufacturer Pack Quantity: 22
```

The value therefore belongs with other size-specific information.

It does not create a separate Pack Type entity.

---

## Pack Type is not part of the core Product Model.

The earlier Product Model included Pack Type beneath Size Variant.

That model is intentionally superseded.

Retailers may:

- sell one manufacturer's pack
- sell several manufacturer's packs together
- break a manufacturer's pack down into individual samples
- create bundles or cases
- choose other selling quantities or presentations

These are Retail Offer arrangements, not additional Product Variants or Size Variants.

A manufacturer-stated packaged quantity remains catalogue information, but the packaging hierarchy itself is not modelled as a core entity.

This distinction prevents retailer-specific commercial arrangements from becoming accidental catalogue entities.

---

## GTIN / Barcode identifies a trade item.

A GTIN is associated with a specific trade item.

Within the normal DiaperScout catalogue model, GTIN / Barcode is recorded with the relevant Size Variant where that is the appropriate representation.

A GTIN should only be recorded when its meaning and scope can be reliably established.

A retailer-created bundle or case must not be assumed to be a new catalogue item simply because the retailer sells it as a distinct offer.

---

## Model reality.

The Product Model reflects how products actually exist.

It does not flatten meaningful manufacturer differences.

It also does not turn every retailer selling arrangement into a catalogue entity.

---

## Objective information comes first.

Manufacturer information and verifiable specifications take priority over retailer descriptions and community interpretation for Product Specification facts.

Retail-specific information remains Retail information.

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
- Is it manufacturer product information or retailer selling information?
- Could it instead be represented as a Community Observation?
- Would introducing a new entity unnecessarily mix retail concerns into the core catalogue?

Only attributes that provide lasting value should become part of the Product Specification.

---

# Living Document

The Decision Log is expected to evolve.

New decisions should be added rather than replacing historical reasoning.

Preserving previous discussions helps future contributors understand how the Product Specification reached its current form and avoids revisiting the same questions repeatedly.
