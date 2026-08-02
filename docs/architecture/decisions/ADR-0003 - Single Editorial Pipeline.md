# ADR-0003 — Single Editorial Pipeline

## Status

Accepted

---

## Context

DiaperScout accepts information from many different sources.

These include:

- Scouts
- Trusted Scouts
- Moderators
- Verified Manufacturers
- Future trusted partners

Each source contributes information with different levels of authority and expertise.

A design decision was required regarding how these contributions should enter the Atlas.

One approach would allow trusted contributors, such as Manufacturers or Moderators, to update the Atlas directly.

Another approach would require every contribution to pass through a consistent editorial workflow regardless of its source.

---

## Decision

DiaperScout adopts a **Single Editorial Pipeline**.

Every meaningful change to the Atlas begins as an Observation.

Every Observation is supported by Evidence.

Every Observation passes through editorial review.

Only accepted editorial decisions modify Canonical Knowledge.

Different contributors may provide evidence with different levels of authority.

They do not follow different architectural pathways.

There is exactly one path into the Atlas.

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

## Consequences

This decision has several important consequences.

### Positive

- Editorial behaviour remains consistent.
- Every published fact has traceable provenance.
- Manufacturer information remains verifiable.
- Moderators curate knowledge rather than editing data.
- Future contribution sources naturally fit the existing architecture.

### Trade-offs

- Even highly trusted contributors do not bypass editorial review.
- Editorial workflows require additional implementation compared to direct editing.
- Publishing information may take slightly longer than direct updates.

These trade-offs are considered worthwhile because they preserve the long-term trustworthiness of the Atlas.

---

## Alternatives Considered

### Direct Manufacturer Updates

Allow Verified Manufacturers to edit Product Specifications directly.

Rejected because it creates a privileged architectural pathway and weakens provenance.

---

### Moderator Direct Editing

Allow Moderators to edit Products directly.

Rejected because it separates editorial activity from the evidence supporting it.

---

### Multiple Editorial Pipelines

Create separate workflows for different contributor types.

Rejected because it duplicates architectural behaviour, increases complexity and makes future evolution more difficult.

---

## Related Documents

- Architecture Principles
- Knowledge Architecture
- Workflow Architecture
- Editorial Architecture
- Authentication & Roles

---

## Notes

The authority of a contributor influences editorial confidence.

It does not change the architectural pathway.

This distinction is fundamental to DiaperScout.

Regardless of who supplies information, the Atlas evolves through the same consistent process.

Future contributor types should integrate into this pipeline rather than introducing alternative mechanisms for modifying Canonical Knowledge.

This decision reinforces the principles that evidence precedes publication, provenance is preserved and every published fact within the Atlas can explain how it came to exist.