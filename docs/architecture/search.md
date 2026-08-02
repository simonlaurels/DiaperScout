# Search

## Purpose

This document describes how people discover knowledge within the DiaperScout Atlas.

Search exists to help contributors and visitors explore trustworthy knowledge.

It should remain deterministic, predictable and understandable.

Search should reveal the Atlas rather than attempt to infer user intent.

---

# Philosophy

DiaperScout is an Explorer's Atlas.

Search exists to help people navigate that Atlas.

Where structured knowledge exists, structured discovery should always be preferred over interpretation.

Users should understand why a result appears.

The same search should always produce the same result.

---

# Design Principles

Search should be:

- deterministic;
- explainable;
- fast;
- predictable;
- based upon canonical knowledge;
- independent of artificial intelligence.

Search should always reflect the current published Atlas.

---

# The Atlas is Searchable

Search indexes the published Atlas.

It does not directly search:

- pending observations;
- moderation queues;
- rejected submissions;
- editorial history.

Search should guide users towards trusted knowledge rather than raw evidence.

---

# Product Discovery

Product discovery is the primary purpose of search.

Free-text search is intentionally simple.

Free-text search applies to:

- Product Name

The objective is rapid identification of known products.

Structured knowledge should be explored through filters rather than increasingly complex text matching.

---

# Structured Exploration

Most exploration occurs through structured filtering.

Examples include:

- Manufacturer
- Size
- Backing Type
- Fastening System
- Wetness Indicator
- Absorbency
- Colour
- Print
- Country
- Retail Availability

Structured filters should reflect Product Specification attributes rather than implementation details.

This makes exploration deterministic and reproducible.

---

# Barcode Search

Barcode scanning is a first-class navigation mechanism.

A recognised barcode should immediately locate the corresponding Product.

Barcode lookup should never rely upon free-text interpretation.

If the barcode is unknown, the search experience naturally transitions into the discovery workflow.

---

# Retail Discovery

Search should allow users to explore the retail world independently of Products.

Examples include:

- retailers stocking a Product;
- Products available at a retailer;
- recently confirmed availability;
- nearby confirmed observations.

Retail availability represents the Atlas' current understanding rather than live stock information.

---

# Geographic Exploration

Geographic search supports exploration based upon location.

Examples include:

- nearby retailers;
- Products confirmed nearby;
- country-specific availability.

Location should enrich exploration rather than replace structured search.

---

# Scout Exploration

Search also supports maintaining the Atlas.

Examples include:

- Products not confirmed recently;
- retailers requiring verification;
- conflicting observations;
- outstanding correction requests.

These searches help generate Scout Tasks that direct community effort where it is most valuable.

---

# Freshness

Knowledge changes over time.

Search should reflect the Atlas' current confidence.

Information supported by recent observations should naturally appear more confidently than information which has not been verified for an extended period.

The Atlas should remain honest about uncertainty.

---

# Performance

Search should remain responsive regardless of Atlas size.

Implementation techniques may include:

- indexing;
- caching;
- background index updates.

Performance improvements should never alter search behaviour.

Users should experience consistent results regardless of implementation.

---

# Artificial Intelligence

Artificial intelligence is not required for search.

The Atlas contains structured knowledge.

Search should therefore rely upon deterministic indexing and structured filtering rather than probabilistic interpretation.

Artificial intelligence may assist contributors in the future, but it should not replace the structured knowledge already present within the Atlas.

---

# Evolution

As the Atlas grows, structured exploration should become increasingly powerful.

The objective is not to build a search engine that guesses what users mean.

The objective is to build an Atlas that is naturally easy to explore because its knowledge is well organised.

---

# Relationship to Other Documents

This document describes how users discover knowledge within the Atlas.

Related documents describe the systems that make search possible.

- **Knowledge Architecture** explains how knowledge becomes searchable.
- **Backend Services** defines the Search Service.
- **Workflow Architecture** explains how newly accepted knowledge becomes searchable.
- **API Architecture** describes how search capabilities are exposed to clients.

Together these documents define how DiaperScout helps people discover trustworthy knowledge while remaining deterministic and explainable.