# Product Attributes

## Overview

This document defines the canonical Product Specification used throughout DiaperScout.

Every approved attribute belongs to one level of the Product Model.

```text
Product
└── Product Variant
    └── Size Variant
```

Each attribute has one authoritative home and should only be recorded once.

For catalogue entry, shared details are entered once at the highest applicable level. Variants inherit shared values by default and store an explicit override only when a characteristic genuinely differs for that variant. Size-specific information belongs at Size Variant level where it can legitimately differ between sizes.

Retail selling arrangements are separate from the core Product Specification.

Where subjective information is required, it belongs in Community Observations rather than the Product Specification.

---

# Product Attributes

These attributes describe the identity of a product.

They remain constant regardless of variant or size.

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

Only attributes that genuinely differ between variants should be stored as variant overrides. A variant does not duplicate shared values simply because the field is also relevant to variants.

---

# Size Variant Attributes

These attributes describe information that may legitimately differ between physical sizes.

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
| Manufacturer Pack Quantity | Integer | Individual products in manufacturer's stated standard pack |
| GTIN / Barcode | Text | Global Trade Item Number |

Manufacturers occasionally publish only some measurements.

Unknown values should remain unknown.

### Manufacturer Pack Quantity

Manufacturer Pack Quantity records the number of individual products that the manufacturer states are supplied in the standard packaged product for that size, where published.

The value may be the same across several sizes or may differ between sizes.

This is product information associated with the Size Variant. It is not a Pack Type and does not create a fourth level in the Product Model.

Manufacturer Pack Quantity must not be confused with a retailer's selling quantity.

---

### GTIN / Barcode

A GTIN / Barcode identifies a specific trade item.

Within the normal DiaperScout catalogue model, manufacturer-issued identifiers are associated with the relevant Size Variant where that is the appropriate representation.

Different sizes commonly have different GTINs.

A GTIN should not be assumed to exist where no reliable identifier is published.

---

# Retail Information

Retail selling arrangements are not Product Specification attributes merely because they describe packaging or quantity.

Retail information may include:

- Quantity offered by a retailer
- Selling presentation
- Retailer SKU
- Individual samples
- Multiple manufacturer's packs sold together
- Retailer-created bundles or cases
- Retail availability
- Retail price and other time-sensitive commercial information

A retailer-created bundle or case does not create a new Product Variant or Size Variant.

Manufacturer-stated pack quantity remains Product Specification information because it describes the manufacturer's packaged product.

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

Temporary promotions, retailer descriptions and marketing claims should not be recorded as Product Specification attributes unless they meet the applicable evidence and attribute rules.

---

## Manufacturer First

Where manufacturer information is available, it should take precedence over retailer information for Product Specification facts.

Retailers frequently rewrite product descriptions.

The Product Specification should prefer primary sources.

Retail-specific information should remain Retail information.

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
- Product Status
- Fragrance

Retail packaging terminology should not be used to create a core Product hierarchy level.

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
| Manufacturer Pack Quantity | Individual products |

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
