# Product Attributes

## Overview

This document defines the canonical Product Specification used throughout DiaperScout.

Every approved attribute belongs to one level of the Product Model.

```
Product
└── Product Variant
    └── Size Variant
        └── Pack Type
```

Each attribute has one authoritative home and should only be recorded once.

Where subjective information is required, it belongs in Community Observations rather than the Product Specification.

---

# Product Attributes

These attributes describe the identity of a product.

They remain constant regardless of variant, size or packaging.

| Attribute | Type | Notes |
|-----------|------|------|
| Manufacturer | Reference | Company responsible for manufacture |
| Brand | Text | Consumer-facing brand |
| Product Name | Text | Official product name |
| Product Family | Text | Optional product range |
| Product Type | Enum | Tape, Pull-up, Pad, Booster, etc. |
| Description | Markdown | Neutral factual description |
| Product Status | Enum | Current, Discontinued, Prototype |
| Official Website | URL | Manufacturer product page |
| Primary Product Image | Image | Representative product image |

---

# Product Variant Attributes

These attributes describe meaningful variations within a Product.

Variants exist only where the manufacturer intentionally produces different versions.

| Attribute | Type | Notes |
|-----------|------|------|
| Variant Name | Text | Plain, Printed, Night, etc. |
| Backing Type | Enum | Plastic, Cloth, Hybrid |
| Fastener Type | Enum | Tape, Hook & Loop, Pull-up |
| Print Design | Text | Description of artwork |
| Primary Colour | Text | Dominant colour |
| Secondary Colours | List | Optional |
| Wetness Indicator | Boolean |
| Standing Leak Guards | Boolean |
| Inner Leak Guards | Boolean |
| Elastic Waistband Front | Boolean |
| Elastic Waistband Rear | Boolean |
| Waistband Style | Enum |
| Fragrance | Enum |
| Latex Free | Boolean |
| Chlorine Free | Boolean |
| Number of Fasteners | Integer |
| Construction Notes | Markdown | Objective only |

Only attributes that genuinely differ between variants should appear here.

---

# Size Variant Attributes

These attributes change with physical size.

| Attribute | Type | Unit |
|-----------|------|------|
| Manufacturer Size | Text | — |
| Waist Range Minimum | Integer | cm |
| Waist Range Maximum | Integer | cm |
| Hip Range Minimum | Integer | cm |
| Hip Range Maximum | Integer | cm |
| Capacity | Integer | ml |
| Product Length | Integer | mm |
| Product Width | Integer | mm |
| Product Weight | Integer | g |

Manufacturers occasionally publish only some measurements.

Unknown values should remain unknown.

---

# Pack Type Attributes

Pack Types describe how a Size Variant is sold.

| Attribute | Type | Notes |
|-----------|------|------|
| Quantity per Pack | Integer |
| Packaging Type | Enum | Bag, Box, Case |
| GTIN / Barcode | Text |
| Case Quantity | Integer | Optional |
| Retail Packaging Image | Image |
| Packaging Notes | Markdown | Objective only |

Packaging changes should not create new Products or Product Variants.

---

# Attribute Design Rules

## One Home

Every attribute belongs to one level of the Product Model.

Do not duplicate information.

---

## Objective Only

Product Specifications record facts.

Subjective information belongs in Community Observations.

---

## Stable Information

Attributes should represent information that remains true for that Product Specification.

Temporary promotions, retailer descriptions and marketing claims should not be recorded as Product Specification attributes.

---

## Manufacturer First

Where manufacturer information is available, it should take precedence over retailer information.

Retailers frequently rewrite product descriptions.

The Product Specification should always prefer primary sources.

---

## Unknown Is Acceptable

If information cannot be verified, leave it blank.

An incomplete Product Specification is preferable to an incorrect one.

---

# Standard Data Types

| Type | Description |
|------|-------------|
| Text | Short free-form text |
| Markdown | Longer formatted text |
| Integer | Whole number |
| Decimal | Numeric value |
| Boolean | True / False |
| Enum | Controlled vocabulary |
| Reference | Link to another entity |
| URL | External web address |
| Image | Image asset |
| List | Multiple values |

---

# Controlled Vocabularies

Where possible, Product Specifications should use controlled vocabularies rather than free text.

Examples include:

- Product Type
- Backing Type
- Fastener Type
- Packaging Type
- Product Status
- Fragrance

Controlled vocabularies improve consistency and simplify filtering.

---

# Units

Unless otherwise stated:

| Measurement | Unit |
|------------|------|
| Length | mm |
| Width | mm |
| Weight | g |
| Capacity | ml |
| Waist / Hip | cm |

The Guide may display alternative units for Explorers, but Product Specifications should use a single canonical unit internally.

---

# Adding New Attributes

New attributes should only be added when they:

- Improve product discovery.
- Improve product comparison.
- Provide important reference information.
- Are objective.
- Can be recorded consistently.
- Have a clear home within the Product Model.

Every accepted attribute should also be recorded in the Attribute Decision Log.

---

# Relationship to Community Observations

The Product Specification answers:

> **"What is this product?"**

Community Observations answer:

> **"What is it like to use?"**

This distinction should always be maintained.

A Product Specification should never attempt to summarise community opinion.

Likewise, Community Observations should never replace objective product information.

Together they provide a complete picture of a product.
