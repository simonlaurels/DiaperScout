# Attribute Decision Log

## Overview

This document records significant decisions about which attributes DiaperScout should store, where they belong and why.

The current Product Specification is authoritative for the attributes that are actually stored.

Where an older decision conflicts with the current Product Specification, the current Product Specification wins.

This document preserves useful rationale and historical terminology so that superseded decisions are not accidentally reintroduced.

---

# Current Product Attributes

## Product

| Attribute | Decision | Reason |
|---|---|---|
| Manufacturer | Include | Core identity |
| Brand | Include | Consumer-facing identity |
| Product Name | Include | Manufacturer product identity |
| Product Family | Include | Useful manufacturer range grouping |
| Product Type | Include | Fundamental discovery/classification |
| Description | Include | Neutral factual context |
| Product Status | Include | Supports current and historical products |
| Official Website | Include | Primary information destination |

## Product Variant

| Attribute | Decision | Reason |
|---|---|---|
| Variant Name | Include | Meaningful manufacturer-defined version |
| Backing Type | Include | Useful objective construction distinction |
| Fastener Type | Include | Meaningful user-facing closure distinction |
| Fastener Count | Include | Two- vs four-fastener products can materially differ |
| Print Design | Include | Useful visual/product distinction |
| Primary Colour | Include | Objective and visually verifiable |
| Wetness Indicator | Include | Useful and commonly disclosed |
| Standing Leak Guards | Include | Visible, objective and functionally meaningful |
| Waistband Style | Include | Cleaner replacement for separate front/rear elastic fields |
| Fragrance | Include | Useful and commonly disclosed |
| Latex Free | Include | Important manufacturer-stated characteristic |
| Designed For | Include | Records explicit manufacturer-stated audience without inference |
| Construction Notes | Include | Factual home for useful unstructured construction details |

## Size Variant

| Attribute | Decision | Reason |
|---|---|---|
| Manufacturer Size | Include | Manufacturer-defined size |
| Waist Measurements | Include | Useful when explicitly stated |
| Hip Measurements | Include | Useful when explicitly stated |
| Fit Measurement Basis | Include | Preserves manufacturer-specific sizing semantics |
| Manufacturer-Stated Absorbency | Include | Useful, but requires source/method context |
| Product Length | Include | Objective physical reference |
| Product Width | Include | Objective physical reference |
| Product Weight | Include | Objective physical reference |
| GTIN / Barcode | Include | Size-specific trade-item identification |

## Pack / Retail

Pack Type, Quantity per Pack, Packaging Type, Case Quantity, Retail Packaging Image, Packaging Notes, retailer, retailer SKU, retail quantity, price, availability and affiliate destinations remain separate from the core product hierarchy.

---

# Consolidated Decisions

## Landing Strip / Landing Zone

**Decision: Remove as a standalone attribute.**

The useful information is represented through Fastener Type.

The terminology becomes less consistent across adhesive and hook-and-loop closure systems, so a separate yes/no engineering field is not justified.

## Front and Rear Elastic Waistbands

**Decision: Replace with Waistband Style.**

The previous separate Boolean fields are consolidated into a single construction attribute that can represent:

- no elastic
- front elastic
- rear elastic
- front + rear elastic
- all-around elastic
- other
- unknown

## Standing Leak Guards vs Inner Leak Guards

**Decision: Keep Standing Leak Guards; remove Inner Leak Guards as a separate field.**

Standing Leak Guards are visually meaningful raised barriers that can be seen on the product.

"Inner Leak Guards", "inner cuffs" and similar terminology remain useful source terminology but do not create a duplicate catalogue attribute.

## Appearance

**Decision: Remove generic Appearance.**

The useful objective information is represented through Primary Colour, Print Design, imagery and factual description.

Do not create subjective categories such as "medical-looking" or "underwear-like".

## Secondary Colours

**Decision: Remove.**

Primary Colour is sufficient as a structured colour field. Additional colours can be represented through Print Design, imagery and factual description.

## Target Gender

**Decision: Rename to Designed For.**

Record the manufacturer's explicit intended audience.

Do not infer it from colour, artwork, styling, anatomy assumptions or marketing imagery.

## Tape Count / Number of Fasteners

**Decision: Rename to Fastener Count.**

The number of fastening points is useful, but "Tape Count" is too narrow because not all closure systems are accurately described as tape.

## Manufacturer Rated Capacity / Capacity

**Decision: Reframe as Manufacturer-Stated Absorbency.**

Manufacturer-published absorbency figures are useful, but can use different test methods or bases.

The catalogue should preserve the stated figure together with methodology/basis and provenance where available.

Do not treat figures from different manufacturers as automatically comparable.

## Waist and Hip Sizing

**Decision: Keep at Size Variant level and preserve manufacturer semantics.**

Manufacturers may publish waist ranges, hip ranges, both, combined ranges or instructions such as "use the larger of waist or hip".

Never infer one measurement from another.

---

# Explicit Exclusions

## ADL / Acquisition Distribution Layer

Exclude from the core Product Specification.

The feature is real, but it is not consistently disclosed in a way that makes it a useful core consumer-facing catalogue attribute.

Explicit manufacturer references can remain in source material or factual descriptions.

## Chlorine Free

Exclude from the core Product Specification.

Chlorine-related claims can involve different processing terminology and definitions. Do not require moderators to classify bleaching processes as a standard product field.

Explicit manufacturer claims can remain in source information where useful.

---

# Historical / Superseded Terminology

| Historical term | Current treatment |
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
| Appearance | No generic field |
| Secondary Colours | No structured field |
| ADL | Excluded |
| Chlorine Free | Excluded |

---

# Decision Principles

Every proposed attribute should pass four tests:

1. **Useful** — Does it help someone understand, compare or find products?
2. **Available** — Can it realistically be obtained from manufacturers or reliable supporting sources?
3. **Objective** — Can it be recorded consistently without subjective judgement?
4. **Worth it** — Is the value worth the moderation and maintenance effort?

A fifth question determines its placement:

5. **Where does it belong?** — Product, Product Variant, Size Variant or Pack/Retail?

Unknown is better than incorrect.

The catalogue is not intended to become an engineering database.

---

# Current Canonical Hierarchy

```text
Product
└── Product Variant
    └── Size Variant
        └── Pack / Retail
```

The Product Specification describes objective product facts.

Community Observations describe experience, opinion and discovery.
