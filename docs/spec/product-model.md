# DiaperScout Product Model

## Purpose

This document defines the hierarchical structure used to represent absorbent products within DiaperScout.

The hierarchy separates information that belongs to an entire product from information that varies between product variants, sizes or packaging.

Correctly assigning data to the appropriate level avoids duplication and keeps the catalogue consistent.

The model is intended to reflect how products exist in the real world rather than how they might be simplified for software implementation.

---

# Product Hierarchy

```text
Product
└── Product Variant
    └── Size Variant
        └── Pack Type
```

Each level has a distinct purpose.

Information should be recorded at the highest appropriate level and only once.

---

# Product

The Product represents the overall identity of a product.

It answers the question:

> "What product is this?"

Examples include:

- BetterDry
- Crinklz Safari
- ABU LittlePawz
- NorthShore MegaMax
- TENA Slip

A Product may contain multiple Product Variants.

The Product remains the same regardless of:

- size
- product variant
- packaging
- retailer

## Typical Product Information

Examples include:

- Manufacturer
- Brand
- Product name
- Product family
- Product type
- Description
- Official product information
- Product images
- Product status

Information belongs at Product level when it remains true for the Product as a whole.

The Product is the anchor for everything beneath it.

---

# Product Variant

A Product Variant represents a materially different version of a Product.

It answers the question:

> "Which version of this product is this?"

A Product Variant exists when a manufacturer intentionally distinguishes multiple versions of the same Product.

A Product Variant may differ in:

- construction
- materials
- backing
- fastening system
- appearance
- print
- features
- performance characteristics

Examples include:

- Plain
- Printed
- Plastic-backed
- Cloth-backed
- Day
- Night
- Plus
- Super
- Ultima

A Product Variant may contain multiple Size Variants.

## Example: TENA Slip

A product such as TENA Slip may be offered in several materially distinct versions:

```text
TENA Slip
├── Plus
├── Super
└── Ultima
```

In this example:

- **TENA Slip** is the Product.
- **Plus**, **Super** and **Ultima** are Product Variants.
- The sizes available within each variant are represented as Size Variants.

The fact that two Product Variants share the same general product name does not make them the same Product Variant.

## Typical Product Variant Information

Examples include:

- Variant name
- Backing type
- Fastener type
- Colourway
- Print design
- Construction differences
- Feature differences
- Material differences

Variants should only exist when meaningful differences are present.

A difference in physical size does not create a new Product Variant.

A packaging difference does not create a new Product Variant.

---

# Size Variant

A Size Variant represents one physical size of a Product Variant.

It answers the question:

> "Which size is this?"

Examples include:

- Small
- Medium
- Large
- XL
- 3XL

or manufacturer-specific sizing systems such as:

- Size 5
- Size 6
- Size 7

A Size Variant represents the physical product at a particular manufacturer-defined size.

Every Size Variant may have its own size-specific characteristics.

## Typical Size Variant Information

Examples include:

- Manufacturer size
- Manufacturer size code
- Waist range
- Hip range
- Capacity
- Product dimensions
- Product weight
- GTIN / Barcode

Any characteristic that changes with physical size belongs at Size Variant level.

### GTIN / Barcode

A GTIN / Barcode identifies the relevant retail product represented by the Size Variant.

In the normal catalogue case, different sizes have different GTINs.

For example:

```text
Product Variant: TENA Slip Plus

Small
└── GTIN A

Medium
└── GTIN B

Large
└── GTIN C

XL
└── GTIN D
```

The GTIN belongs with the Size Variant rather than the Product as a whole because the barcode commonly changes between sizes.

A GTIN should not be assumed to exist where no reliable identifier is published.

Unknown identifiers remain unknown.

---

# Pack Type

A Pack Type represents how a Size Variant is packaged or sold.

It answers the question:

> "How is this size packaged or presented for sale?"

Examples include:

- Sample
- Retail pack
- Bag
- Case

A Pack Type does not represent a different Product, Product Variant or Size Variant.

The underlying Size Variant remains the same.

## Typical Pack Type Information

Examples include:

- Pack type
- Quantity of products contained
- Packaging format
- Case quantity, where applicable
- Retail packaging information
- Retail packaging photographs

Pack Types exist to represent packaging configurations without duplicating the underlying product specification.

### Retail Pack and Case

A retail pack may contain a number of individual products, for example:

```text
Medium
└── Retail bag
    └── 10 individual products
```

A case may contain multiple retail packs, for example:

```text
Medium
└── Case
    ├── Bag of 10
    ├── Bag of 10
    ├── Bag of 10
    └── Bag of 10
```

A case therefore does not necessarily represent a separate product or a separate Size Variant.

A case may also not have its own GTIN.

GTIN ownership follows the actual identifier associated with the relevant retail product and should not be inferred simply because a packaging configuration exists.

---

# Relationships

Each level has a one-to-many relationship with the level below it.

```text
Product
    ├── Product Variant
    │       ├── Size Variant
    │       │       ├── Pack Type
    │       │       └── Pack Type
    │       │
    │       └── Size Variant
    │
    └── Product Variant
```

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

Each Size Variant may then have one or more Pack Types representing how that size is packaged or sold.

This structure allows DiaperScout to represent real products without repeatedly storing the same information.

---

# Why This Model?

The Product Model exists for three reasons.

## 1. Accuracy

Information is recorded where it naturally belongs.

A manufacturer's name should not be repeated on every size.

A waist measurement should not be attached to the entire Product.

A Product Variant characteristic should not be repeated on every Size Variant when it remains the same across the variant.

Everything should have a single authoritative home.

---

## 2. Maintainability

Changes are made once.

If a manufacturer changes its name, only the Product requires updating.

If a new Product Variant is introduced, only the Product Variant and its associated Size Variants need to be added.

If a new size is introduced, only a new Size Variant is required.

If a new packaging configuration is introduced, only a new Pack Type is required.

This keeps the Guide consistent over time.

---

## 3. Scalability

The model supports:

- International products
- Limited editions
- Seasonal releases
- Regional variants
- Discontinued products
- Different manufacturer-defined product variants
- Different sizing systems
- Different packaging configurations
- Future product categories

without requiring structural redesign.

---

# What Does *Not* Create a New Level?

Not every difference creates a new Product, Product Variant or Size Variant.

Examples that normally do **not** create new entities include:

- New retailer
- Temporary discount
- Promotional bundle
- Different shipping carton
- Warehouse labels
- Retail stickers

These belong to retail information rather than the core Product Specification.

A packaging change should not create a new Product or Product Variant merely because the outer packaging has changed.

---

# Design Principles

The Product Model follows several simple principles.

## Record Information Once

Every fact should have one authoritative home.

Avoid duplication.

If a fact remains true across all Sizes of a Product Variant, it belongs at Product Variant level rather than being repeated on every Size Variant.

If a fact changes with size, it belongs at Size Variant level.

If a fact describes how that size is packaged, it belongs at Pack Type level.

---

## Model Reality

The Guide should reflect how manufacturers produce and identify products, not how software prefers to store them.

The hierarchy should therefore preserve meaningful distinctions between:

- Products
- Product Variants
- Sizes
- Packaging

without creating unnecessary entities.

---

## Separate Product from Packaging

Products and packaging are different concepts.

Packaging changes more frequently than products.

A retail pack or case does not automatically represent a different product.

Keeping packaging separate improves long-term stability.

---

## Separate Product Variant from Size

A Product Variant represents a meaningful version of a Product.

A Size Variant represents a physical size of that Product Variant.

For example:

```text
TENA Slip
└── Super
    ├── Medium
    ├── Large
    └── XL
```

Here, **Super** is the Product Variant and **Medium, Large and XL** are Size Variants.

A size must not be recorded as a Product Variant simply because it is the most obvious variation encountered when researching a product.

---

## Separate Facts from Observations

The Product Model describes products.

Community Observations describe experiences with those products.

Neither should replace the other.

Objective product information belongs in the Product Specification.

Subjective experiences belong in Community Observations.

---

## Keep Relationships Predictable

Every Product follows the same hierarchy:

```text
Product
└── Product Variant
    └── Size Variant
        └── Pack Type
```

Explorers and contributors should never need to guess where information belongs.

Consistency makes the Guide easier to navigate and easier to maintain.

---

# Future Evolution

The Product Model has been designed to accommodate future growth.

New attributes may be added to existing levels when justified.

New entity types should only be introduced when there is a clear modelling need that cannot be represented within the existing hierarchy.

Stability should always be preferred over unnecessary complexity.

---

# Summary

The Product Model provides the structure that underpins every Product Specification in DiaperScout.

By separating:

- Products
- Product Variants
- Size Variants
- Pack Types

the Guide mirrors the real world while remaining simple, consistent and maintainable.

A Product identifies **what the product is**.

A Product Variant identifies **which meaningful version it is**.

A Size Variant identifies **which physical size it is**.

A Pack Type identifies **how that size is packaged or presented for sale**.

GTIN / Barcode information belongs with the relevant Size Variant where it identifies the retail product represented by that size.

Every piece of information has a natural home.

Keeping information in that home is one of the foundations of a trustworthy explorer's guide.
