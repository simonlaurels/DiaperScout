# Product Model

## Overview

Every absorbent product represented within DiaperScout follows the same hierarchical model.

The model reflects how products exist in the real world rather than how they might be packaged, marketed or stored in software.

Separating information into distinct levels avoids duplication, improves consistency and allows the Guide to grow without becoming increasingly complex.

---

# Product Hierarchy

```
Product
└── Product Variant
    └── Size Variant
        └── Pack Type
```

Each level has a clearly defined purpose.

Information should be recorded at the highest appropriate level and only once.

---

# Product

The Product represents the identity of a product.

It answers the question:

> **"What product is this?"**

Examples include:

- BetterDry
- Crinklz Astronaut
- Trest Elite Briefs
- ABU Little Kings

The Product remains the same regardless of:

- size
- colour
- print
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
- Barcode prefixes (where appropriate)
- Official product information
- Product images
- Product status (current or discontinued)

The Product is the anchor for everything beneath it.

---

# Product Variant

A Product Variant represents a distinct version of a Product.

It answers the question:

> **"Which version of this product is this?"**

Variants exist when a manufacturer intentionally creates multiple versions of the same Product.

Examples include:

- Plain
- Printed
- Hook-and-loop
- Tape
- Day
- Night
- Plastic-backed
- Cloth-backed

A Product Variant has its own characteristics while still belonging to the same Product.

## Typical Product Variant Information

Examples include:

- Backing type
- Fastener type
- Colourway
- Print design
- Construction differences
- Feature differences
- Material differences

Variants should only exist when meaningful differences are present.

Minor packaging changes do not create new Product Variants.

---

# Size Variant

A Size Variant represents one physical size of a Product Variant.

It answers the question:

> **"Which size is this?"**

Every Size Variant has its own measurements.

Examples include:

- Small
- Medium
- Large
- XL
- 3XL

or

- Size 5
- Size 6
- Size 7

depending on the manufacturer's sizing system.

## Typical Size Variant Information

Examples include:

- Manufacturer size
- Waist range
- Hip range
- Capacity
- Product dimensions
- Weight range
- Individual product weight

Any characteristic that changes with size belongs here.

---

# Pack Type

A Pack Type represents how a Size Variant is sold.

It answers the question:

> **"How can this size be purchased?"**

Examples include:

- Pack of 10
- Pack of 12
- Pack of 14
- Case of 48
- Case of 60

Different retailers may sell different Pack Types without changing the underlying product.

## Typical Pack Type Information

Examples include:

- Quantity per pack
- Packaging format
- GTIN
- Retail packaging photographs
- Case quantity (where applicable)

Pack Types describe packaging, not products.

---

# Relationships

Each level has a one-to-many relationship with the level below it.

```
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

This hierarchy reflects how manufacturers organise products.

It also allows DiaperScout to avoid recording the same information repeatedly.

---

# Why This Model?

The Product Model exists for three reasons.

## 1. Accuracy

Information is recorded where it naturally belongs.

A manufacturer's name should not be repeated on every pack size.

A waist measurement should not be attached to the entire product.

Everything has a single authoritative home.

---

## 2. Maintainability

Changes are made once.

If a manufacturer changes its name, only the Product requires updating.

If a new size is introduced, only a new Size Variant is required.

If a retailer begins selling a larger pack, only a new Pack Type is added.

This keeps the Guide consistent over time.

---

## 3. Scalability

The model supports:

- International products
- Limited editions
- Seasonal releases
- Regional variants
- Discontinued products
- Future product categories

without requiring structural redesign.

---

# What Does *Not* Create a New Level?

Not every difference creates a new Product, Product Variant or Size Variant.

Examples that normally **do not** create new entities include:

- New retailer
- Temporary discount
- Promotional bundle
- Different shipping carton
- Warehouse labels
- Retail stickers

These belong to retail information rather than the Product Specification.

---

# Design Principles

The Product Model follows several simple principles.

## Record Information Once

Every fact should have one authoritative home.

Avoid duplication.

---

## Model Reality

The Guide should reflect how manufacturers produce products, not how software prefers to store them.

---

## Separate Product from Packaging

Products and packaging are different concepts.

Packaging changes more frequently than products.

Keeping them separate improves long-term stability.

---

## Separate Facts from Observations

The Product Model describes products.

Community Observations describe experiences with those products.

Neither should replace the other.

---

## Keep Relationships Predictable

Every Product follows the same hierarchy.

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

By separating Products, Product Variants, Size Variants and Pack Types, the Guide mirrors the real world while remaining simple, consistent and maintainable.

Every piece of information has a natural home.

Keeping information in that home is one of the foundations of a trustworthy explorer's guide.