# Data Model Principles

## Overview

The DiaperScout Product Specification exists to document absorbent products as faithfully and objectively as possible.

These principles define how the Guide models products, evaluates new attributes and maintains consistency over time.

Every decision within the Product Specification should align with these principles.

---

# Core Philosophy

DiaperScout is an explorer's guide.

Its purpose is to help Explorers discover products, understand how they differ and make informed decisions using trustworthy information.

The Guide achieves this by combining two complementary sources of knowledge:

- Product Specifications describe objective facts about products.
- Community Observations describe real-world experiences of those products.

Retail information provides a third, distinct layer describing how products are offered for sale.

Neither layer should replace the others.

---

# Principle 1 — Describe Products, Not People

The Product Specification describes products.

It does not describe the people who use them.

Attributes should answer questions such as:

- What is this product?
- How is it constructed?
- What features does it have?
- Who manufactures it?

They should not attempt to categorise or describe the people who may purchase or wear it.

This keeps the Guide objective, respectful and internationally applicable.

---

# Principle 2 — Prefer Objective Facts

Product Specifications should contain information that can be verified.

Examples include:

- Manufacturer
- Product type
- Fastener type
- Standing leak guards
- Wetness indicator
- Number of tapes
- Country of manufacture

These facts should not change depending on who observes the product.

If reasonable people could disagree about an attribute based on personal experience alone, it probably belongs in Community Observations instead.

---

# Principle 3 — Unknown Is Better Than Incorrect

The Guide should never invent information.

If a fact cannot be confirmed it should remain unknown until reliable evidence becomes available.

An incomplete Product Specification is preferable to an inaccurate one.

Incorrect information reduces trust.

Unknown information invites future discovery.

---

# Principle 4 — Record Facts Once

Every fact should have a single authoritative home.

Avoid storing the same information in multiple places.

For example:

- A manufacturer belongs to the Product.
- A waist measurement belongs to the Size Variant.
- Manufacturer pack quantity belongs to the Size Variant.
- A retailer's selling quantity belongs to the Retail Offer.

Recording information once reduces duplication and prevents conflicting data.

---

# Principle 5 — Model Reality

The Product Specification should reflect how products actually exist.

The core Product Model is:

```text
Product
└── Product Variant
    └── Size Variant
```

A product may exist in multiple variants and multiple sizes.

Manufacturers may also state information about how a size is packaged, such as the number of individual products in its standard pack. That remains product information and may be recorded at Size Variant level where applicable.

Retailers may sell the same underlying product in different quantities or presentations. Those arrangements belong to Retail information rather than the core Product Model.

---

# Principle 6 — Separate Manufacturer Product Information from Retail Selling Information

Manufacturer product information and retailer selling information answer different questions.

Manufacturer information may state:

> "This size is supplied with 24 individual products in the standard pack."

Retail information may state:

> "This retailer sells one pack."

or:

> "This retailer sells three packs together."

or:

> "This retailer sells individual samples."

The manufacturer's stated quantity is Product Specification information.

The retailer's chosen quantity or presentation is Retail Offer information.

Changing the retail offer must not silently change the underlying Product Specification.

---

# Principle 7 — Separate Facts from Experiences

Product Specifications describe products.

Community Observations describe experiences.

For example:

| Product Specification | Community Observation |
| --------------------- | --------------------- |
| Has standing leak guards | Leak guards worked well overnight |
| Fastener type is tape | Tapes stayed secure after twelve hours |
| Contains a wetness indicator | Wetness indicator was difficult to see |
| Made in Germany | Easier to buy in Germany than the UK |

This separation allows objective facts and subjective experiences to complement one another without becoming confused.

---

# Principle 8 — Store Facts, Not Conclusions

The Guide should record information that allows Explorers to reach their own conclusions.

For example:

Prefer:

- Absorbency capacity
- Waist range
- Product dimensions
- Manufacturer pack quantity

Instead of:

- Good for nights
- Suitable for heavy wetters
- Comfortable
- Premium quality

The latter are interpretations that vary between individuals and belong in Community Observations.

---

# Principle 9 — Every Attribute Must Justify Its Existence

Every attribute increases the complexity of the Product Specification.

Before introducing a new attribute, ask:

- Does it improve product discovery?
- Does it improve product comparison?
- Does it provide important reference information?
- Can it be recorded consistently?
- Is it objective?
- Does it belong in the Product Specification rather than Community Observations?
- Is it manufacturer product information rather than retailer-specific selling information?

If the answer is no, the attribute probably should not exist.

A smaller, well-designed Product Specification is more valuable than a larger, inconsistent one.

---

# Principle 10 — Design for Longevity

Products change.

Manufacturers merge.

Retailers disappear.

Packaging evolves.

The Product Specification should be stable enough to accommodate these changes without requiring fundamental redesign.

Retail selling arrangements can change without changing the underlying product.

A well-designed model should continue to represent products accurately for many years.

---

# Principle 11 — Respect the Product

Every product deserves to be documented accurately regardless of:

- popularity
- country of origin
- intended market
- price
- manufacturer

The Guide should treat discontinued products, boutique manufacturers and mainstream brands with equal care.

Discovery begins with documentation.

---

# Evaluating New Attributes

When proposing a new attribute, consider the following questions.

## Is it objective?

Can independent contributors verify it?

## Is it stable?

Will it remain true for the life of that Product Specification?

## Is it useful?

Does it improve discovery, comparison or reference?

## Is it measurable?

Can contributors reasonably record it consistently?

## Does it belong at the correct level?

Should it belong to:

- Product
- Product Variant
- Size Variant
- Retail Offer

## Is it already represented elsewhere?

Avoid duplicate information.

## Could it be a Community Observation instead?

Experiences should not become Product Specifications.

---

# Decision Checklist

A proposed Product Specification attribute should normally satisfy most of the following.

- Objective
- Verifiable
- Stable
- Useful
- Discoverable
- Comparable
- Non-duplicated
- Correctly scoped
- Internationally applicable
- Future-proof

Failure to satisfy these criteria does not automatically reject an idea, but it should prompt further discussion.

---

# Summary

The Product Specification is intentionally conservative.

Its purpose is not to capture every possible detail.

Its purpose is to capture the right details.

By modelling products objectively, separating manufacturer product information from retailer selling arrangements, and leaving personal experiences to Community Observations, DiaperScout creates a Guide that remains trustworthy, maintainable and useful as it grows.

Every Product Specification should help Explorers answer:

> **"What is this product?"**

Retail information helps answer:

> **"How is this product being offered here?"**

Every Community Observation helps answer:

> **"What is it like to use?"**

All three questions matter.

Keeping them separate is one of the foundations of DiaperScout.
