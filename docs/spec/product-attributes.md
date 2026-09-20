# DiaperScout Product Attributes

## Purpose

This document is the authoritative reference for the Product Specification.

It defines which attributes are stored, which level they belong to, and how they should be recorded.

The Product Specification is intentionally conservative.

An attribute should provide meaningful product discovery, comparison or reference value, be objective enough to record consistently, and have a clear home in the Product Model.

Unknown is preferable to invented information.

---

# Product Attributes

| Attribute | Type | Notes |
|---|---|---|
| Manufacturer | Reference | Organisation responsible for the product |
| Brand | Reference | Consumer-facing brand |
| Product Name | Text | Manufacturer's product name |
| Product Family | Text / Reference | Manufacturer-defined broader range where applicable |
| Product Type | Enum | Fundamental product classification |
| Description | Markdown | Neutral, factual product description |
| Product Status | Enum | Current, discontinued, prototype, etc. |
| Official Website | URL | Official manufacturer/product information destination |

---

# Product Variant Attributes

| Attribute | Type | Notes |
|---|---|---|
| Variant Name | Text | Manufacturer-defined variant name where applicable |
| Backing Type | Enum | Plastic, cloth-like/textile, hybrid, other, unknown |
| Fastener Type | Enum | Adhesive Tape, Hook & Loop, Other, Unknown |
| Fastener Count | Integer | Number of fastening points where clearly defined |
| Print Design | Text | Objective description of manufacturer-defined artwork |
| Primary Colour | Text / Enum | Dominant product colour |
| Wetness Indicator | Boolean / Unknown | Manufacturer-provided visual indicator |
| Standing Leak Guards | Boolean / Unknown | Raised standing barriers around leg openings |
| Waistband Style | Enum | No Elastic, Front, Rear, Front + Rear, All-Around, Other, Unknown |
| Fragrance | Enum | Fragranced, Fragrance-Free, Unknown |
| Latex Free | Boolean / Unknown | Manufacturer-stated latex-free status |
| Designed For | Enum / Text | Manufacturer-stated intended audience |
| Construction Notes | Markdown | Objective construction information |

Only values that genuinely differ between variants should be stored as variant-specific information.

## Fastener Type

Fastener Type describes the mechanism used to secure the product.

Initial controlled values:

- Adhesive Tape
- Hook & Loop
- Other
- Unknown

Landing Strip / Landing Zone is not a separate attribute. Its useful information is represented through Fastener Type and factual product description.

## Fastener Count

Fastener Count replaces the older "Tape Count" and "Number of Fasteners" terminology.

The term is deliberately broader because not all closure systems are accurately described as tape.

## Standing Leak Guards

Standing Leak Guards record whether raised barriers around the leg openings are present.

"Inner Leak Guards", "inner cuffs" and similar terminology do not create a second structured field.

## Waistband Style

Waistband Style replaces the separate Front Elastic Waistband and Rear Elastic Waistband fields.

Initial values:

- No Elastic Waistband
- Front Elastic
- Rear Elastic
- Front + Rear Elastic
- All-Around Elastic
- Other
- Unknown

## Designed For

Designed For replaces the older Target Gender terminology.

Record the manufacturer's explicit intended audience only.

Initial values may include:

- Men
- Women
- Unisex
- Manufacturer-defined
- Unknown

Do not infer this from colour, artwork, styling, anatomy assumptions or marketing imagery.

## Construction Notes

Construction Notes are factual only.

They may contain useful construction information that does not justify another structured field.

They must not become a place for comfort opinions, recommendations, reviews or subjective performance judgements.

---

# Size Variant Attributes

| Attribute | Type | Unit / Notes |
|---|---|---|
| Manufacturer Size | Text | Manufacturer's size label/code |
| Waist Minimum | Integer | cm; only when explicitly stated |
| Waist Maximum | Integer | cm; only when explicitly stated |
| Hip Minimum | Integer | cm; only when explicitly stated |
| Hip Maximum | Integer | cm; only when explicitly stated |
| Fit Measurement Basis | Enum / Text | Manufacturer's stated sizing method |
| Manufacturer-Stated Absorbency | Integer / Decimal | ml where a numerical figure is published |
| Absorbency Basis / Method | Text / Enum | Test method or stated basis where available |
| Product Length | Integer | mm |
| Product Width | Integer | mm |
| Product Weight | Integer | g |
| GTIN / Barcode | Text | Global Trade Item Number where reliably established |
| Manufacturer Pack Quantity | Integer | Number of individual products the manufacturer states are supplied in the standard pack for this size |

## Manufacturer Fit Measurements

Manufacturers may publish:

- waist range
- hip range
- both waist and hip ranges
- a combined waist-or-hip range
- another explicitly defined measurement basis

Capture the manufacturer's stated information faithfully.

Do not infer a waist range from a hip range or a hip range from a waist range.

If the manufacturer says to use the larger of waist or hip, record that basis explicitly.

Retailer sizing information may support verification, but manufacturer information takes precedence where available.

## Manufacturer-Stated Absorbency

Manufacturer-Stated Absorbency replaces the older "Capacity" and "Manufacturer Rated Capacity" terminology.

The catalogue records the manufacturer's published figure without implying that it is a universal real-world capacity.

Where available, retain:

- numerical value
- unit
- test method or methodology
- stated basis
- source/provenance

Do not normalise different manufacturers' figures into a common performance score.

Community reports of real-world performance belong in Community Observations.

## GTIN / Barcode

GTIN / Barcode normally belongs at Size Variant level because different sizes commonly have different identifiers.

Only record a GTIN when its value and scope can be reliably established.

Do not copy GTINs when propagating size data between variants.

---

# Pack / Retail Attributes

| Attribute | Type | Notes |
|---|---|---|
| Pack Type | Enum | Sample, Pack, Case, etc. |
| Retail Quantity | Integer | Number of manufacturer packs or individual units offered in a specific retail arrangement |
| Packaging Type | Enum | Bag, Box, Case, etc. |
| Case Quantity | Integer | Number of retail packs in a case |
| Retail Packaging Image | Image | Packaging image |
| Packaging Notes | Markdown | Objective packaging information |

Retail information may additionally include retailer, retailer SKU, quantity offered, price, availability, retailer destination and affiliate destination.

Retail quantity is not the same thing as Manufacturer Pack Quantity.

---

# Explicitly Excluded Core Attributes

| Attribute | Treatment |
|---|---|
| ADL / Acquisition Distribution Layer | Exclude from core Product Specification |
| Landing Strip / Landing Zone | No standalone field; represented through Fastener Type |
| Inner Leak Guards | No separate field; Standing Leak Guards retained |
| Appearance | No generic structured field |
| Secondary Colours | No structured field |
| Chlorine Free | Exclude from core Product Specification |
| Target Gender | Replaced by Designed For |
| Tape Count | Replaced by Fastener Count |
| Number of Fasteners | Replaced by Fastener Count |
| Elastic Waistband Front | Replaced by Waistband Style |
| Elastic Waistband Rear | Replaced by Waistband Style |
| Capacity | Replaced by Manufacturer-Stated Absorbency |
| Manufacturer Rated Capacity | Replaced by Manufacturer-Stated Absorbency |

---

# Attribute Design Rules

## One Home

Every attribute belongs to one level of the Product Model.

Do not duplicate information.

## Objective Only

Product Specifications record facts.

Subjective information belongs in Community Observations.

## Manufacturer First

Where manufacturer information is available, it should normally take precedence over retailer information.

## Unknown Is Acceptable

If information cannot be verified, leave it unknown.

## Do Not Over-model

A technically interesting fact is not automatically worth a dedicated field.

A structured attribute should be useful, realistically obtainable, objective enough to record consistently, and worth the moderation and maintenance effort.

---

# Community Observations

The following remain outside the Product Specification:

- Comfort
- Softness
- Quietness
- Discretion
- Confidence
- Value for Money
- Best for Overnight
- Good for Heavy Wetting
- Popularity
- Community Rating
- Review Score
- Favourite Product
- Real-world fit experiences
- Real-world leakage experiences

The Product Specification describes what the product is.

Community Observations describe what it is like to use.
