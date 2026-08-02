# Knowledge Architecture

## Purpose

This document describes how knowledge is represented, refined and published within DiaperScout.

Rather than treating the Atlas as a collection of editable database records, DiaperScout models knowledge as evidence that is curated into a trustworthy Explorer's Atlas.

Understanding this distinction is fundamental to understanding the architecture of the project.

---

# Philosophy

Knowledge is not created by editing records.

Knowledge emerges through observation, evidence and editorial judgement.

The Atlas therefore represents the current published understanding of the world rather than the raw information collected by the community.

Every architectural component ultimately exists to support this model.

---

# The Layers of Knowledge

The architecture intentionally separates knowledge into four distinct layers.

```text
Reality

↓

Observation

↓

Evidence

↓

Editorial Judgement

↓

Atlas
```

Each layer has a different purpose.

Maintaining these boundaries preserves both trust and clarity.

---

# Reality

Reality exists independently of DiaperScout.

Products exist.

Retailers stock products.

Manufacturers create products.

Scouts observe them.

The architecture should always attempt to model reality rather than replacing it.

---

# Observations

Observations describe things that someone has witnessed.

Examples include:

- discovering a new product;
- seeing a product in a retailer;
- reporting a correction;
- submitting manufacturer information.

Observations describe events.

They do not directly change the Atlas.

---

# Evidence

Evidence supports an Observation.

Examples include:

- photographs;
- barcode scans;
- official manufacturer documentation;
- written notes;
- timestamps;
- locations.

Evidence answers one question:

> **Why should the Atlas believe this observation?**

---

# Editorial Judgement

Editors evaluate evidence.

Possible outcomes include:

- accepted;
- rejected;
- deferred;
- additional evidence requested.

Editorial judgement transforms evidence into trusted knowledge.

Editorial judgement does not replace evidence.

It explains why evidence was accepted.

---

# The Atlas

The Atlas is the published result of this process.

Users browse the Atlas.

Search indexes the Atlas.

The public API exposes the Atlas.

The Atlas intentionally presents only the current canonical understanding.

Supporting evidence remains available where appropriate but is not itself the Atlas.

---

# Sources of Knowledge

Knowledge may originate from many different contributors.

Examples include:

- Scouts;
- Trusted Scouts;
- Moderators;
- Verified Manufacturers.

Different contributors possess different authority.

They do not follow different architectural pathways.

Every source contributes through the same evidence model.

---

# Canonical Knowledge

Canonical knowledge represents the best current understanding available.

Examples include:

- Product Specifications;
- Manufacturer information;
- Canonical imagery;
- Regional variations.

Canonical knowledge should remain concise, understandable and trustworthy.

---

# Provenance

Every significant piece of canonical knowledge should retain provenance.

The architecture should always be capable of explaining:

- where information originated;
- what evidence supported it;
- who reviewed it;
- when it became canonical.

Knowledge that cannot explain itself should not become part of the Atlas.

---

# Historical Record

The Atlas represents today's understanding.

The historical record explains how that understanding evolved.

Editorial history should preserve meaningful evolution while avoiding unnecessary operational noise.

The objective is comparable to a well-used Scout's Atlas.

Corrections remain understandable.

Mistakes need not become permanent history.

---

# Derived Knowledge

Some information within the Atlas is derived from existing knowledge.

Examples include:

- retailer confidence;
- Scout Tasks;
- stale observation detection;
- retailer availability summaries.

Derived knowledge should always be reproducible from the underlying observations and editorial decisions.

Derived information should never become the authoritative source of truth.

---

# Continuous Refinement

The Atlas is never considered complete.

New evidence continually refines understanding.

The architecture should therefore support continuous improvement without compromising trust, provenance or consistency.

Knowledge evolves.

The process by which knowledge evolves should remain stable.

---

# Knowledge Lifecycle

Every significant piece of knowledge follows the same lifecycle.

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

Future Observation

↓

Refined Atlas
```

This cycle continues throughout the lifetime of the project.

The Atlas is therefore not simply edited.

It is continually refined through structured community exploration.

---

# Architectural Consequences

This model influences every part of the architecture.

- Products remain independent of observations.
- Evidence belongs to observations.
- Manufacturer submissions become evidence.
- Search indexes the Atlas rather than raw observations.
- APIs expose canonical knowledge while allowing supporting provenance where appropriate.
- Scout Tasks arise from uncertainty within the Atlas rather than arbitrary activity.

Every architectural subsystem should reinforce this model.

No subsystem should bypass it.

---

# Relationship to Other Documents

This document explains how knowledge flows through DiaperScout.

Related documents describe other aspects of the architecture:

- **Architecture Principles** explains why the architecture is designed this way.
- **Domain Model** describes the concepts involved.
- **Editorial Architecture** explains how evidence becomes trusted knowledge.
- **Workflow Architecture** describes how work moves through the system.

Together these documents define how the Atlas is created, maintained and refined.