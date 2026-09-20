# DiaperScout Product Model

## Purpose

This document defines the hierarchical structure used to represent absorbent products within DiaperScout.

The hierarchy separates information that belongs to an entire product from information that varies between product variants, physical sizes, packaging and retail arrangements.

The model is intended to reflect how products exist in the real world rather than how they might be simplified for software implementation.

---

# Product Hierarchy

```text
Product
└── Product Variant
    └── Size Variant
        └── Pack / Retail
```

Each level has a distinct purpose.

Information should be recorded at the highest appropriate level and only once.

---

# Product

The Product represents the overall identity of a product.

It answers:

> "What product is this?"

Product-level information includes:

- Manufacturer
- Brand
- Product Name
- Product Family
- Product Type
- Description
- Product Status
- Official Website

A Product may contain multiple Product Variants.

A difference in physical size, packaging or retailer does not create a new Product.

---

# Product Variant

A Product Variant represents a meaningful manufacturer-defined version of a Product.

It answers:

> "Which version of this product is this?"

A Product Variant may differ in construction, backing, fastening, print, colour, features or other objective characteristics.

Product Variant attributes are:

- Variant Name
- Backing Type
- Fastener Type
- Fastener Count
- Print Design
- Primary Colour
- Wetness Indicator
- Standing Leak Guards
- Waistband Style
- Fragrance
- Latex Free
- Designed For
- Construction Notes

A difference in physical size does not create a Product Variant.

A packaging or retailer difference does not create a Product Variant.

---

# Size Variant

A Size Variant represents one physical manufacturer-defined size of a Product Variant.

It answers:

> "Which physical size is this?"

Size Variant attributes are:

- Manufacturer Size
- Manufacturer Fit Measurements
- Fit Measurement Basis
- Manufacturer-Stated Absorbency
- Absorbency Basis / Method
- Product Length
- Product Width
- Product Weight
- GTIN / Barcode
- Manufacturer Pack Quantity

Manufacturers use different sizing conventions. DiaperScout must preserve the manufacturer's stated measurement basis and must not infer waist from hip or hip from waist.

If a manufacturer says to use the larger of waist or hip, record that method rather than copying the same range into both fields.

Manufacturer-stated absorbency must retain source/methodology context where available. Different manufacturers' figures must not be treated as automatically equivalent.

---

# Pack / Retail

Packaging and retail arrangements are separate from the core Product hierarchy.

They answer:

> "How is this size packaged, offered or sold?"

Pack/Retail information may include:

- Pack Type
- Retail Quantity
- Packaging Type
- Case Quantity
- Retail Packaging Image
- Packaging Notes
- Retailer
- Retailer SKU
- Retail Quantity
- Retail Price
- Retail Availability
- Retail Destination
- Affiliate Destination

A retail pack, case or retailer bundle does not automatically create a new Product, Product Variant or Size Variant.

Retail quantity is distinct from Manufacturer Pack Quantity.

---

# Record Information Once

Every fact should have one authoritative home.

If a fact remains true across all sizes of a Product Variant, it normally belongs at Product Variant level.

If it changes with physical size, it belongs at Size Variant level.

If it describes packaging or a retail offer, it belongs in Pack/Retail information.

Do not duplicate facts merely because they are relevant at more than one level.

---

# Separate Product Specification from Community Observations

The Product Specification records objective product facts.

Community Observations record experiences, opinions and discoveries.

Examples of Community Observations include:

- comfort
- softness
- noise
- perceived discretion
- personal fit
- real-world leakage experience
- reviews
- ratings
- comparisons

These should not be converted into objective Product Specification facts.

---

# Summary

The canonical hierarchy is:

```text
Product
└── Product Variant
    └── Size Variant
        └── Pack / Retail
```

A Product identifies what the product is.

A Product Variant identifies which meaningful version it is.

A Size Variant identifies which physical manufacturer-defined size it is.

Pack/Retail information identifies how that size is packaged or offered for sale.
