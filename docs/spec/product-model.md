# DiaperScout Product Model

## Purpose

This document defines the hierarchical structure used to represent absorbent products within DiaperScout.

The hierarchy separates information that belongs to an entire product from information that varies between product variants and sizes.

```text
Product
└── Product Variant
    └── Size Variant
```

Each level has a distinct purpose.

Information should be recorded at the highest appropriate level and only once.

Retail selling arrangements are deliberately kept separate from the core Product Model.

---

# Product Hierarchy

```text
Product
└── Product Variant
    └── Size Variant
```

## Product

The Product represents the overall identity of a product.

It answers:

> "What product is this?"

A Product may contain multiple Product Variants.

The Product remains the same regardless of:

- size
- product variant
- retailer
- retail selling arrangement

Typical Product information includes:

- Manufacturer
- Brand
- Product name
- Product family
- Product type
- Description
- Official product information
- Product status
- Representative product images

---

# Product Variant

A Product Variant represents a materially different version of a Product.

It answers:

> "Which meaningful version of this product is this?"

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

- Plus
- Super
- Ultima
- Plastic-backed
- Cloth-backed
- Plain
- Printed
- Day
- Night

Variants should only exist when meaningful differences are present.

A difference in physical size does not create a new Product Variant.

A difference in retailer or retail selling arrangement does not create a new Product Variant.

---

# Size Variant

A Size Variant represents one manufacturer-defined physical size of a Product Variant.

It answers:

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

A Size Variant is the natural home for characteristics that may legitimately differ between sizes.

## Typical Size Variant Information

Examples include:

- Manufacturer size
- Manufacturer size code
- Waist range
- Hip range
- Capacity
- Product dimensions
- Product weight
- Manufacturer pack quantity
- GTIN / Barcode

Manufacturers do not necessarily publish every value for every size. Unknown values remain unknown.

### Manufacturer Pack Quantity

Where a manufacturer states how many individual products are supplied in its standard packaged product for a particular size, this may be recorded as **Manufacturer Pack Quantity**.

For example:

```text
TENA Slip Active Fit Maxi

Medium
└── Manufacturer pack quantity: 24

Large
└── Manufacturer pack quantity: 22
```

This is product information, not a separate Pack Type entity.

The quantity may legitimately differ between sizes and therefore belongs with the other size-specific information.

The field describes the manufacturer's stated packaged quantity. It does not describe how a retailer chooses to sell the product.

### GTIN / Barcode

A GTIN / Barcode identifies a specific trade item.

Within the normal DiaperScout catalogue model, manufacturer-issued identifiers are associated with the relevant Size Variant where that is the appropriate representation.

Different sizes commonly have different identifiers.

A GTIN should not be assumed to exist where no reliable identifier is published.

Unknown identifiers remain unknown.

---

# Retail Selling Arrangements

Retail selling arrangements are not part of the core Product Model.

A retailer may sell the same manufacturer product in different ways, including:

- one manufacturer's pack
- multiple manufacturer's packs together
- an individual sample taken from a manufacturer's pack
- a retailer-created bundle or case
- another retailer-defined quantity or presentation

These arrangements belong to Retail information and must not create new Products, Product Variants or Size Variants merely because the selling arrangement differs.

For example:

```text
Catalogue Product
└── TENA Slip Active Fit Maxi
    └── Large
        └── Manufacturer pack quantity: 22
```

A retailer may then offer that same Size Variant as:

```text
Retail Offer A
└── 1 manufacturer's pack

Retail Offer B
└── 3 manufacturer's packs bundled together

Retail Offer C
└── 1 individual sample
```

The retail offers do not create three catalogue variants.

A retailer-created bundle or case should not be confused with a manufacturer-defined product configuration.

---

# What Does Not Create a New Level?

Not every difference creates a new Product, Product Variant or Size Variant.

Examples that normally do not create new core catalogue entities include:

- New retailer
- Temporary discount
- Promotional bundle
- Retailer-created case or multipack
- Individual sample
- Different shipping carton
- Warehouse labels
- Retail stickers
- Changes to outer retail packaging that do not change the underlying manufactured product

A meaningful manufacturer-defined product difference may create a Product Variant.

A manufacturer-defined physical size creates a Size Variant.

Retail selling arrangements do not.

---

# Why This Model?

## 1. Accuracy

Information is recorded where it naturally belongs.

A manufacturer name should not be repeated on every size.

A waist measurement should not be attached to the entire Product.

A Product Variant characteristic should not be repeated on every Size Variant when it remains the same across the variant.

Manufacturer-stated pack quantity belongs with Size Variant information because it can vary by size.

Retailer-created quantities and selling presentations belong with Retail information because they describe the offer rather than the underlying product.

---

## 2. Maintainability

Changes are made once.

If a manufacturer changes its name, only the Product requires updating.

If a new Product Variant is introduced, only that Product Variant and its associated Size Variants need to be added.

If a new size is introduced, only a new Size Variant is required.

If a retailer changes how it sells the product, the Retail Offer can change without altering the core Product Specification.

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
- Manufacturer-stated packaged quantities
- Multiple retail selling arrangements

without treating retailer packaging as part of the core product hierarchy.

---

# Design Principles

## Record Information Once

Every fact should have one authoritative home.

Avoid duplication.

If a fact remains true across all Sizes of a Product Variant, it belongs at Product Variant level rather than being repeated on every Size Variant.

If a fact changes with physical size, it belongs at Size Variant level.

If a fact describes how a retailer is selling the product, it belongs in Retail information.

---

## Model Reality

The Guide should reflect how products exist in the real world rather than how software prefers to store them.

The core catalogue describes the manufactured product.

Retail describes how that product is offered for sale.

---

## Manufacturer Information vs Retail Information

The following distinction is fundamental:

**Manufacturer product information**

> "This size is supplied by the manufacturer with 24 individual products in its standard pack."

**Retail information**

> "This retailer sells one pack."

> "This retailer sells three packs together."

> "This retailer sells individual samples."

The first is part of the Product Specification.

The latter are Retail Offer information.

A retailer changing the quantity or presentation it offers must not silently change the underlying Product Specification.

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

Here, Super is the Product Variant and Medium, Large and XL are Size Variants.

---

## Separate Facts from Observations

The Product Model describes products.

Community Observations describe experiences with those products.

Neither should replace the other.

Objective product information belongs in the Product Specification.

Subjective experiences belong in Community Observations.

---

## Keep Retail Separate

Retailers may create bundles, cases, samples and other selling arrangements.

Those arrangements are useful catalogue-adjacent information but do not redefine the underlying product.

This separation prevents retailer-specific commercial arrangements from becoming accidental Product Variants or Size Variants.

---

# Future Evolution

The Product Model has been designed to accommodate future growth.

New attributes may be added to existing levels when justified.

New entity types should only be introduced when there is a clear modelling need that cannot be represented within the existing hierarchy.

Retail concepts should not be introduced into the core Product Model merely because retailers present products in different ways.

Stability should always be preferred over unnecessary complexity.

---

# Summary

The Product Model provides the structure that underpins every Product Specification in DiaperScout.

The core hierarchy is:

```text
Product
└── Product Variant
    └── Size Variant
```

A Product identifies **what the product is**.

A Product Variant identifies **which meaningful version it is**.

A Size Variant identifies **which manufacturer-defined physical size it is**.

Manufacturer-stated information such as pack quantity may be recorded at Size Variant level where it describes that size's standard packaged product.

Retailer quantities, samples, bundles, cases and selling presentations are Retail information and do not create new core catalogue entities.

Every piece of information has a natural home.

Keeping information in that home is one of the foundations of a trustworthy explorer's guide.
