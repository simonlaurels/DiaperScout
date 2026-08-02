# ADR-0006 — Media Ownership Model

## Status

Accepted

---

## Context

DiaperScout stores a wide variety of media.

Examples include:

- official manufacturer product images;
- community discovery photographs;
- shelf photographs;
- barcode photographs;
- supporting documentation.

Initially these appeared to be a single category of media.

As the architecture evolved it became clear that these assets serve fundamentally different purposes.

Some media represents published knowledge within the Atlas.

Other media exists solely to support an Observation.

Treating all media identically would blur the distinction between canonical knowledge and historical evidence.

A clear ownership model was therefore required.

---

## Decision

DiaperScout adopts a **Media Ownership Model** based on two distinct categories of media.

### Atlas Media

Atlas Media forms part of the published Atlas.

Examples include:

- official manufacturer images;
- approved product photography;
- packaging artwork.

Atlas Media belongs to Products.

It represents Canonical Knowledge.

---

### Observation Media

Observation Media supports an Observation.

Examples include:

- discovery photographs;
- shelf photographs;
- barcode photographs;
- photographs supporting correction requests.

Observation Media belongs to Observations.

It represents Evidence.

Observation Media never becomes Atlas Media automatically.

Editorial review determines whether Atlas Media should change.

---

## Consequences

This decision has several important consequences.

### Positive

- Canonical imagery remains distinct from evidential photography.
- Historical observations remain understandable.
- Provenance is preserved.
- Products remain independent of individual observations.
- Future media types naturally fit the same ownership model.

### Trade-offs

- Additional storage may be required because evidential media is preserved.
- Editorial decisions are required before Atlas Media changes.
- Media management becomes slightly more complex than a single shared library.

These trade-offs are considered worthwhile because they preserve both trust and provenance.

---

## Alternatives Considered

### Product Owns All Media

Store every image directly against the Product.

Rejected because evidential photographs become mixed with canonical imagery and historical provenance is weakened.

---

### Shared Media Library

Store all media independently without explicit ownership.

Rejected because ownership becomes ambiguous and architectural responsibilities become unclear.

---

### Replace Canonical Images Automatically

Allow accepted observations to automatically replace Atlas Media.

Rejected because publication of canonical imagery should always remain an editorial decision.

---

## Related Documents

- Media & Evidence
- Knowledge Architecture
- Editorial Architecture
- Database Model

---

## Notes

The distinction between Atlas Media and Observation Media is conceptual rather than technical.

Different storage technologies may be used in the future without affecting this architectural principle.

This model also allows future media types—such as videos, documents or 3D assets—to integrate naturally.

Regardless of format, every media asset belongs to either:

- the Atlas; or
- an Observation.

There is no third category.

Maintaining this distinction preserves the long-term integrity of both published knowledge and the evidence from which that knowledge is derived.