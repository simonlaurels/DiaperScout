# Glossary

## Purpose

This glossary defines the shared vocabulary used throughout the DiaperScout documentation.

These definitions provide a common language for contributors and help ensure terminology remains consistent across the project.

Where a term is defined here, that definition should be used consistently throughout the repository.

The glossary evolves alongside DiaperScout and acts as the single authoritative source for project terminology.

---

# Atlas Concepts

## Atlas

The published body of trustworthy knowledge maintained by DiaperScout.

The Atlas represents DiaperScout's current understanding of the world.

The Atlas is the product.

## Atlas Media

Media that forms part of the published Atlas.

Examples include:

* Official manufacturer images
* Approved product photography
* Packaging artwork

Atlas Media belongs to Products.

## Canonical Knowledge

Knowledge that has successfully completed editorial review and forms part of the Atlas.

Canonical Knowledge represents DiaperScout's current published understanding of the world.

## Editorial Decision

The outcome of editorial review.

Examples include:

* Accept
* Reject
* Request Additional Evidence
* Defer

Editorial Decisions determine whether the Atlas changes.

## Editorial History

The historical record explaining how the Atlas has evolved over time.

Editorial History preserves meaningful decisions while avoiding unnecessary operational noise.

## Evidence

Information supporting an Observation.

Evidence explains why the Atlas should believe an Observation.

Evidence may include:

* Photographs
* Barcode images
* Documentation
* Written notes
* Timestamps
* Locations

## Guide

The user experience through which people explore the Atlas.

The Guide presents Products, Community Observations, retailer information and educational content in a form designed for exploration.

The Guide presents the Atlas.

The Atlas contains the knowledge.

## Observation

A factual report describing something observed in the real world.

Examples include:

* Product Discoveries
* Retail Observations
* Correction Requests
* Manufacturer Submissions

Observations preserve evidence.

Observations do not directly modify the Atlas.

## Observation Media

Media attached to an Observation.

Examples include:

* Discovery photographs
* Shelf photographs
* Barcode photographs
* Supporting documentation

Observation Media belongs to Observations rather than Products.

## Provenance

Information describing where Canonical Knowledge originated and the evidence supporting it.

Provenance should allow significant knowledge within the Atlas to explain itself.

---

# Community

## Community Observation

A contribution describing an Explorer's real-world experience, opinion or discovery relating to a Product.

Community Observations complement Product Specifications by providing knowledge that cannot be represented through objective attributes alone.

Examples include:

* Reviews
* Ratings
* Fit notes
* Comparisons
* Photographs
* Availability discoveries

## Community Observations

The collection of Community Observation content associated with a Product.

Community Observations are intentionally separate from Product Specifications.

## Explorer

A person using or participating in DiaperScout.

Explorers discover, document and share knowledge about absorbent products.

Explorers may contribute Observations, evidence, corrections and other approved community contributions.

Explorer is the canonical user-facing term for a person participating in the DiaperScout world.

## Moderator

A trusted community member responsible for reviewing evidence and maintaining the Atlas through editorial decisions.

Moderators do not own the Atlas.

They act as its custodians on behalf of the community.

## Contributor

An Explorer who contributes information to DiaperScout.

A Contributor may submit:

* Community Observations
* Photographs
* Product information
* Corrections
* Location information
* Other approved contributions

Contributor describes an activity or contribution context rather than a separate account type.

## Stewardship

The ongoing responsibility of improving and maintaining the Atlas.

Stewardship is demonstrated through consistently valuable contribution rather than popularity.

## Trust

A measure of a contributor's demonstrated stewardship.

Trust may influence community responsibilities and promotion recommendations.

Trust does not replace evidence.

## Verified Manufacturer

A manufacturer whose identity has been confirmed.

Verified Manufacturers may submit official observations.

Verification establishes identity.

It does not bypass editorial review.

---

# Product Model

## Brand

The consumer-facing identity under which a Product is sold.

A Brand may manufacture Products itself or be owned by another organisation.

## GTIN

Global Trade Item Number.

A unique identifier used to identify a retail product or pack.

GTINs normally belong to Pack Types rather than Products.

## Manufacturer

The organisation responsible for producing a Product.

Manufacturers may submit official observations through verified accounts.

## Pack Type

The lowest level of the Product Model.

A Pack Type describes how a Size Variant is sold.

Examples include:

* Pack of 10
* Pack of 14
* Case of 48

Pack Types describe packaging rather than Products.

## Product

The highest level of the Product Model.

A Product represents the identity of a product regardless of its variants, sizes or packaging.

Products exist independently of retailers and observations.

## Product Family

An optional grouping used by manufacturers to identify related Products.

## Product Model

The hierarchical structure used by DiaperScout to organise Product Specifications.

```text
Product
└── Product Variant
    └── Size Variant
        └── Pack Type
```

## Product Specification

The objective, authoritative description of a Product maintained by DiaperScout.

A Product Specification records verifiable facts about a Product and intentionally excludes subjective opinions and experiences.

## Product Status

Indicates whether a Product is currently manufactured.

Examples include:

* Current
* Discontinued
* Prototype

## Product Type

The broad category describing how a Product functions.

Examples include:

* Tape Brief
* Pull-up
* Pad
* Booster

## Product Variant

The second level of the Product Model.

A Product Variant represents a distinct version of a Product with meaningful differences in construction, appearance or features.

Examples include:

* Printed
* Plain
* Plastic-backed
* Cloth-backed

## Regional Variation

A recognised difference between Products sold in different countries or regions.

Regional Variations form part of the Product Specification.

## Size Variant

The third level of the Product Model.

A Size Variant represents one physical size of a Product Variant.

Measurements such as waist range, capacity and dimensions belong here.

---

# Product Attributes

## Appearance

The visual characteristics of a Product Variant, such as colours, prints and artwork.

Appearance should describe observable characteristics only and should not include subjective opinions.

## Backing Type

The material used on the outer surface of a Product.

Examples include:

* Plastic
* Cloth
* Hybrid

Backing Type is a Product Variant attribute.

## Capacity

The published absorbency capacity of a Size Variant.

Capacity should always be recorded as an objective Product Specification where available.

## Fastener Type

The mechanism used to secure a Product.

Examples include:

* Tape
* Hook & Loop
* Pull-up

Fastener Type is a Product Variant attribute.

## Landing Strip

The reinforced frontal area that allows repositionable tapes or hook-and-loop fasteners to attach securely.

Landing Strips are objective construction features.

## Latex Free

Indicates that a Product is manufactured without natural latex.

This is an objective Product Specification attribute.

## SAP

Super Absorbent Polymer.

The absorbent material responsible for retaining liquid within the Product.

## Standing Leak Guards

Raised internal barriers designed to help reduce leakage.

Standing Leak Guards are objective construction features.

## Tape Count

The total number of fastening tapes fitted to a Product.

Examples include:

* Two tapes
* Four tapes

Tape Count is an objective Product Variant attribute.

## Target Gender

The gender or genders identified by the manufacturer as the intended market for a Product.

This attribute reflects manufacturer positioning rather than who may actually use the Product.

## Waist Range

The manufacturer-published waist measurement for a Size Variant.

Measurements should be recorded using centimetres.

## Wetness Indicator

A feature that changes appearance after becoming wet.

Wetness Indicators are objective Product Variant attributes.

---

# Retail

## Retail Observation

An Observation describing the availability of a Product at a retailer.

Retail Observations contribute evidence.

They do not represent live stock information.

## Retailer

An organisation that sells Products.

Retailers do not directly own Products.

Availability is derived from observations made by the community.

---

# Principles

## Controlled Vocabulary

A predefined set of permitted values for an attribute.

Controlled Vocabularies improve consistency, simplify filtering and reduce ambiguity throughout the Guide.

Examples include:

* Product Type
* Backing Type
* Fastener Type
* Packaging Type
* Product Status

## Objective

Information that can be independently verified and remains true regardless of who records it.

Objective information belongs in the Product Specification.

Examples include:

* Manufacturer
* Capacity
* Waist Range
* Backing Type

## Subjective

Information based on personal experience, interpretation or opinion.

Subjective information belongs in Community Observations rather than the Product Specification.

Examples include:

* Comfort
* Softness
* Quietness
* Confidence

## Workflow

The process by which Observations become trusted knowledge.

Every significant workflow follows the same principles:

```text
Observation

↓

Evidence

↓

Editorial Review

↓

Atlas
```

---

# See Also

For further information, see:

* `docs/north-star/`
* `docs/product/`
* `docs/community/`
* `docs/architecture/`
