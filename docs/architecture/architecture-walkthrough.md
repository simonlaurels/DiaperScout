# Architecture Walkthrough

## Purpose

This document provides a narrative walkthrough of the DiaperScout architecture.

Rather than describing individual architectural components in isolation, it follows a single observation from discovery to publication, demonstrating how the various parts of the system work together.

This document is intended to be the recommended introduction to the DiaperScout architecture and should be read before the remaining architecture documents.

---

# Introduction

DiaperScout is not simply a database of products.

It is an Explorer's Atlas.

Every piece of knowledge within the Atlas begins life as an observation made by someone in the real world.

The architecture exists to transform those observations into trustworthy, explainable knowledge while preserving the evidence that supports every editorial decision.

This walkthrough follows that journey.

---

# The Story Begins

A Scout walks into a supermarket.

While browsing the incontinence aisle they notice a product they have never seen before.

Curious, they open DiaperScout and scan the barcode.

The Atlas has never seen this product before.

A new journey begins.

---

# Step 1 – Observation

Because the barcode is unknown, DiaperScout offers the Scout the opportunity to expand the Atlas.

The Scout records:

- Barcode
- Front product photograph
- Retailer
- Approximate location
- Optional price

Nothing within the Atlas changes.

The Scout has simply recorded an Observation.

The Atlas has gained evidence.

It has not yet gained knowledge.

---

# Step 2 – Evidence

The Observation is safely stored.

Everything supporting that Observation is preserved.

This includes:

- barcode;
- photographs;
- retailer;
- location;
- timestamp.

These items become Evidence.

Evidence belongs to the Observation.

No Product exists yet, so nothing belongs to the Atlas.

---

# Step 3 – Validation

Before editorial review begins, the platform validates the submission.

Examples include:

- barcode validation;
- media validation;
- duplicate detection;
- integrity checking.

Validation protects the Atlas from accidental errors while ensuring genuine discoveries continue through the workflow.

---

# Step 4 – Background Processing

Some work occurs asynchronously.

Examples include:

- image optimisation;
- thumbnail generation;
- metadata extraction;
- workflow preparation.

The contributor's submission is already safely stored.

Background processing simply prepares the Observation for editorial review.

Throughout this process the contributor can see meaningful progress.

---

# Step 5 – Editorial Review

A moderator reviews the Observation.

Importantly, the moderator is **not editing the Atlas**.

Instead they are evaluating the available Evidence.

Typical questions include:

- Is this genuinely a new Product?
- Is the barcode correct?
- Is the evidence sufficient?
- Is additional information required?

The Atlas remains unchanged.

---

# Step 6 – Editorial Decision

The moderator reaches a decision.

Possible outcomes include:

- Accept
- Reject
- Request Additional Evidence
- Defer Pending Investigation

Every decision should be supported by evidence.

The evidence remains preserved regardless of the outcome.

---

# Step 7 – Atlas Updated

If the Observation is accepted:

- a new Product is created;
- an initial Product Specification is established;
- the barcode becomes associated with the Product;
- official manufacturer information may later enrich the Product.

For the first time, the Atlas changes.

The Atlas has grown.

---

# Step 8 – Supporting Systems Respond

Publishing a Product causes other architectural components to respond.

Examples include:

- Search indexes are updated.
- Retail availability is recalculated.
- Scout trust is recalculated.
- Notifications are generated where appropriate.
- Future Scout Tasks are evaluated.

These actions occur independently.

None of them alter the editorial decision that created the Product.

---

# Step 9 – The Product Evolves

Weeks later a verified Manufacturer submits updated Product Specifications and official imagery.

Although highly authoritative, the submission still follows the same architectural pathway.

```text
Observation

↓

Evidence

↓

Editorial Review

↓

Atlas Updated
```

The contributor has changed.

The workflow has not.

The architecture remains consistent.

---

# Step 10 – The Atlas Asks for Help

Months later the Atlas notices something.

No Scout has confirmed this Product at one retailer for over ninety days.

Rather than silently assuming the information is still correct, the Atlas creates a Scout Task.

> Confirm whether this Product is still stocked at this retailer.

A nearby Scout accepts the task.

The cycle begins again.

---

# A Living Atlas

Every completed Scout Task generates another Observation.

Every Observation may produce new Evidence.

Every Editorial Decision refines the Atlas.

The Atlas continually improves through structured community exploration.

```text
Reality

↓

Observation

↓

Evidence

↓

Editorial Review

↓

Atlas

↓

Exploration

↓

New Observation
```

The Atlas is never considered complete.

It continually becomes more accurate as new observations are made.

---

# Architectural Themes

Every architecture document describes one part of this journey.

| Document | Explains |
|----------|----------|
| Architecture Principles | Why the architecture exists |
| Knowledge Architecture | How knowledge evolves |
| Domain Model | The concepts represented by the Atlas |
| Entity Reference | Each entity in detail |
| Backend Services | Who performs each responsibility |
| API Architecture | How clients interact with the Atlas |
| Database Model | How knowledge is preserved |
| Search | How users explore the Atlas |
| Workflow Architecture | How work progresses |
| Editorial Architecture | How trust is established |
| Authentication & Roles | Who may perform each responsibility |
| Media & Evidence | How evidence is preserved |
| Scout Task System | How the Atlas guides community effort |
| Deployment & Operations | How the Atlas is protected |

Each document focuses on one aspect of the architecture.

Together they describe a single coherent system.

---

# The Big Picture

DiaperScout is not a CRUD application.

It is not simply a database of products.

It is an editorial platform for building and preserving a trustworthy Explorer's Atlas.

Everything within the architecture exists to support one simple idea:

> **People observe the world.**
>
> **Evidence supports those observations.**
>
> **Editorial review establishes trust.**
>
> **The Atlas preserves that knowledge for future explorers.**

The architecture exists to make that journey reliable, transparent and sustainable for many years to come.