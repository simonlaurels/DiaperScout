# ADR-0005 — Deterministic Search

## Status

Accepted

---

## Context

One of DiaperScout's primary purposes is to help people discover products and explore the Atlas.

Modern search systems increasingly rely on artificial intelligence and probabilistic interpretation to infer user intent.

While these approaches can be useful for unstructured information, DiaperScout stores highly structured knowledge.

Product names, manufacturers, specifications and attributes are all represented explicitly within the Atlas.

A decision was required regarding whether search should prioritise deterministic behaviour or inferred behaviour.

---

## Decision

DiaperScout adopts a **Deterministic Search** model.

Search should reflect the structured knowledge contained within the Atlas.

Free-text search is limited to Product names.

Structured discovery is performed using Product Specification attributes and other canonical metadata.

Search should always produce predictable and explainable results.

Where structured knowledge exists, filtering is preferred over inference.

Artificial intelligence is not required to interpret structured Product data.

---

## Consequences

This decision has several important consequences.

### Positive

- Search behaviour remains predictable.
- Contributors understand why results are returned.
- Filtering becomes increasingly powerful as the Atlas grows.
- Search results remain consistent across platforms.
- Search performance can be optimised independently of search behaviour.

### Trade-offs

- Users must occasionally refine searches using filters rather than relying on broad free-text matching.
- Synonyms and spelling variations require explicit support where appropriate.
- Search relies upon well-maintained Product Specifications.

These trade-offs are considered worthwhile because they produce a search experience that is transparent, reproducible and faithful to the Atlas.

---

## Alternatives Considered

### AI-Powered Semantic Search

Use artificial intelligence to infer user intent.

Rejected because the Atlas already contains structured knowledge that can be searched deterministically.

Artificial intelligence introduces behaviour that may be difficult to explain or reproduce.

---

### Full-Text Search Across All Fields

Index every field and perform unrestricted text searching.

Rejected because it reduces the value of structured Product Specifications and encourages inconsistent metadata.

---

### Attribute-Free Search

Rely primarily on free-text searching.

Rejected because structured filtering provides a more accurate and predictable method of exploring canonical knowledge.

---

## Related Documents

- Search
- Product Specification
- API Architecture
- Domain Model

---

## Notes

Deterministic Search reflects the philosophy that the Atlas is a structured body of knowledge rather than an unstructured collection of documents.

As the Atlas grows, improvements should focus on expanding structured metadata and filtering capabilities rather than increasing search complexity.

Artificial intelligence may assist contributors in future workflows, but it should not replace deterministic exploration of canonical knowledge.

Search should help users discover the Atlas because the Atlas is well organised, not because the search engine guesses what they meant.