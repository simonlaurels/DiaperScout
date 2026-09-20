# DiaperScout Glossary

## Purpose

This glossary defines the current terminology used throughout the DiaperScout project.

Where an older document uses superseded terminology, use the current term below.

---

# A

## Absorbency — Manufacturer-Stated

A numerical absorbency figure explicitly published by a manufacturer for a Size Variant.

It must not be treated as a universal real-world capacity.

Where available, the stated methodology or basis and source provenance should be retained.

## ADL

**Superseded / excluded attribute.**

ADL means Acquisition Distribution Layer.

It describes an internal absorbent-product construction component, but is not a current core Product Specification attribute.

---

# B

## Backing Type

The material or construction of the outer backing of a product.

Typical values may include Plastic, Cloth-like/Textile, Hybrid, Other and Unknown.

Backing Type is a Product Variant attribute.

## Brand

The consumer-facing identity under which a product is sold.

Brand is a Product attribute.

---

# C

## Capacity

**Superseded terminology.**

Use Manufacturer-Stated Absorbency instead.

## Community Observation

A contribution describing an Explorer's real-world experience, opinion or discovery relating to a product.

## Construction Notes

Factual notes describing useful product construction details that do not warrant a separate structured attribute.

Construction Notes are a Product Variant attribute and must not contain subjective reviews or recommendations.

## Controlled Vocabulary

A predefined set of permitted values for an attribute.

---

# D

## Designed For

The manufacturer-stated intended audience for a Product Variant.

Typical values may include Men, Women, Unisex, Manufacturer-defined and Unknown.

Designed For replaces the superseded Target Gender terminology.

It must not be inferred from colour, artwork, styling, anatomy assumptions or marketing imagery.

---

# E

## Explorer

A member of the DiaperScout community who discovers, documents or shares knowledge about absorbent products.

---

# F

## Fastener Count

The number of fastening points used to secure a product, where clearly defined.

Fastener Count replaces Tape Count and Number of Fasteners.

## Fastener Type

The mechanism used to secure a product.

Initial values include:

- Adhesive Tape
- Hook & Loop
- Other
- Unknown

Landing Zone / Landing Strip is not a separate catalogue attribute.

## Fit Measurement Basis

The manufacturer's stated method for applying a Size Variant's fit measurements.

Examples:

- Waist
- Hip
- Waist and Hip
- Larger of Waist or Hip
- Other manufacturer-defined basis
- Unknown

Fit Measurement Basis is a Size Variant attribute.

## Fragrance

Whether the manufacturer states that a product is fragranced or fragrance-free.

Values:

- Fragranced
- Fragrance-Free
- Unknown

Fragrance is a Product Variant attribute.

---

# G

## GTIN

Global Trade Item Number.

A globally recognised identifier used to identify a specific trade item.

Within DiaperScout, GTIN / Barcode normally belongs with the relevant Size Variant.

---

# H

## Hook & Loop

A mechanical fastening system in which hooks engage with a loop or compatible fastening surface.

Hook & Loop is a Fastener Type.

---

# L

## Latex Free

A manufacturer-stated indication that a product is latex-free.

Values:

- Yes
- No
- Unknown

Latex Free is a Product Variant attribute.

## Landing Zone / Landing Strip

**Superseded / excluded attribute.**

A landing zone may form part of a fastening system, but DiaperScout does not store it as a standalone structured field.

Its useful user-facing information is represented through Fastener Type.

---

# M

## Manufacturer

The organisation responsible for manufacture of the product.

Manufacturer is a Product attribute.

## Manufacturer Size

The size label or code assigned by the manufacturer.

Manufacturer Size is a Size Variant attribute.

## Manufacturer-Stated Absorbency

A manufacturer-published absorbency figure associated with a Size Variant.

It replaces the older Capacity and Manufacturer Rated Capacity terminology.

The value should retain source and methodology/basis where available.

---

# P

## Pack / Retail

Information describing how a Size Variant is packaged, offered or sold.

Pack/Retail information is separate from the core Product hierarchy.

## Primary Colour

The dominant colour of a Product Variant.

A separate Secondary Colours attribute is not part of the core Product Specification.

## Print Design

Objective manufacturer-defined artwork or printed design on a Product Variant.

## Product

The overall catalogue identity of an absorbent product.

## Product Family

A manufacturer-defined broader range or family to which a Product belongs.

## Product Type

The fundamental category of the product.

## Product Variant

A meaningful manufacturer-defined version of a Product.

---

# S

## Size Variant

A physical manufacturer-defined size of a Product Variant.

A Size Variant may have its own manufacturer size, fit measurements, manufacturer-stated absorbency, dimensions, weight and GTIN.

## Standing Leak Guards

Raised barriers around the leg openings intended to help contain leakage.

Standing Leak Guards are a Product Variant attribute.

"Inner Leak Guards", "inner cuffs" and similar terminology do not create a separate structured attribute.

---

# W

## Waistband Style

The elastic waistband construction of a Product Variant.

Initial values may include:

- No Elastic Waistband
- Front Elastic
- Rear Elastic
- Front + Rear Elastic
- All-Around Elastic
- Other
- Unknown

Waistband Style replaces the separate Front Elastic Waistband and Rear Elastic Waistband fields.

## Wetness Indicator

A manufacturer-provided visual indicator intended to show that a product may need checking or changing.

Values:

- Yes
- No
- Unknown

Wetness Indicator is a Product Variant attribute.

---

# Superseded Terms

| Superseded term | Current term |
|---|---|
| Target Gender | Designed For |
| Tape Count | Fastener Count |
| Number of Fasteners | Fastener Count |
| Elastic Waistband Front | Waistband Style |
| Elastic Waistband Rear | Waistband Style |
| Capacity | Manufacturer-Stated Absorbency |
| Manufacturer Rated Capacity | Manufacturer-Stated Absorbency |
| Landing Strip | No standalone field; represented through Fastener Type |
| Landing Zone | No standalone field; represented through Fastener Type |
| Inner Leak Guards | No separate field; Standing Leak Guards retained |
| Appearance | No generic structured field |
| Secondary Colours | No structured field |
| ADL | Excluded |
| Chlorine Free | Excluded |

---

# Core Hierarchy

```text
Product
└── Product Variant
    └── Size Variant
        └── Pack / Retail
```

Every catalogue fact should have one clear home within this hierarchy.

The Product Specification describes objective product facts.

Community Observations describe experience, opinion and discovery.
