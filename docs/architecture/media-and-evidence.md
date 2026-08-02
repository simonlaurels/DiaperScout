# Media & Evidence

## Purpose

This document describes how DiaperScout stores, manages and preserves media and supporting evidence.

Media is not simply visual content.

Within DiaperScout, media exists for one of two purposes:

- representing the Atlas;
- supporting observations.

Maintaining this distinction is fundamental to preserving both trust and provenance.

---

# Philosophy

Media represents knowledge.

Some media communicates published knowledge.

Other media supports the evidence from which that knowledge was derived.

These responsibilities must remain distinct throughout the architecture.

The Atlas should present canonical media.

Observations should preserve evidential media.

---

# Media Categories

DiaperScout recognises two distinct categories of media.

## Atlas Media

Atlas Media represents published knowledge.

Examples include:

- official manufacturer product images;
- approved product photography;
- packaging artwork;
- branding imagery.

Atlas Media belongs to Products.

It forms part of the published Atlas.

---

## Observation Media

Observation Media supports community observations.

Examples include:

- discovery photographs;
- shelf photographs;
- barcode photographs;
- photographs supporting correction requests;
- supporting documentation.

Observation Media belongs to the Observation that created it.

It never becomes Atlas Media automatically.

---

# Evidence

Media is one form of evidence.

Evidence may also include:

- written notes;
- timestamps;
- GPS location;
- manufacturer documentation;
- barcode values.

Evidence exists to answer one question:

> **Why should the Atlas believe this observation?**

---

# Ownership

Ownership should remain explicit.

Products own:

- Atlas Media.

Observations own:

- Observation Media;
- supporting evidence.

Ownership should never become ambiguous.

Replacing Atlas Media should never affect historical observations.

Removing an Observation should never affect canonical Atlas Media.

---

# Editorial Workflow

Observation Media enters the editorial workflow together with its Observation.

Editors evaluate the complete body of evidence.

Acceptance of an Observation does not automatically promote Observation Media into Atlas Media.

Editorial decisions determine whether Atlas Media should change.

---

# Canonical Images

Where available, official manufacturer imagery should normally become the primary imagery presented by the Atlas.

Community photographs remain valuable evidence.

They should not normally replace official imagery unless an editorial decision determines that they provide a more accurate representation of the Product.

The Atlas should always present the most trustworthy representation available.

---

# Historical Evidence

Observation Media forms part of the historical record.

It explains how knowledge entered the Atlas.

Historical evidence should normally be preserved unless:

- legal requirements demand removal;
- privacy concerns require removal;
- moderation determines removal is necessary.

Historical evidence strengthens provenance.

---

# Processing

Media processing may include:

- validation;
- optimisation;
- thumbnail generation;
- format conversion;
- duplicate detection;
- integrity checking.

Processing should never alter the evidential meaning of the original media.

Where practical, original uploads should be preserved.

Derived versions exist for presentation rather than replacing the original.

---

# Provenance

Every piece of media should retain provenance.

The architecture should always be capable of identifying:

- who submitted it;
- when it was submitted;
- what Observation it supports;
- whether it is Atlas Media or Observation Media;
- how it became part of the Atlas.

Media should always be explainable.

---

# Integrity

Media should be validated before becoming part of the platform.

Validation may include:

- supported formats;
- corruption detection;
- malicious content checks;
- duplicate detection.

Integrity checks exist to protect both contributors and the Atlas.

---

# Privacy

Media may unintentionally contain personal information.

Examples include:

- people;
- vehicle registration numbers;
- addresses;
- metadata.

The architecture should support appropriate moderation while preserving the evidential value of the media wherever practical.

---

# Storage

Storage technology is an implementation detail.

Regardless of implementation:

- Atlas Media remains attached to Products.
- Observation Media remains attached to Observations.
- Original uploads remain traceable.
- Provenance is preserved.
- Historical evidence remains understandable.

The storage architecture should reinforce the conceptual model rather than blur it.

---

# Architectural Consequences

This model produces several important characteristics.

- Canonical Product imagery remains separate from evidential photography.
- Editorial review determines how media influences the Atlas.
- Historical evidence remains preserved.
- Provenance remains intact throughout the media lifecycle.
- Storage technology can evolve without changing the ownership model.

---

# Evolution

Future media types should naturally belong to one of the two existing categories:

- Atlas Media; or
- Observation Media.

Maintaining this distinction preserves the long-term integrity of both the Atlas and the evidence that supports it.

---

# Relationship to Other Documents

This document describes how media and evidence are managed within DiaperScout.

Related documents describe the surrounding architecture.

- **Knowledge Architecture** explains how evidence becomes knowledge.
- **Editorial Architecture** explains how evidence is reviewed.
- **Workflow Architecture** explains how media progresses through the system.
- **Database Model** defines how ownership and persistence are represented.

Together these documents define how DiaperScout preserves trustworthy visual evidence while maintaining a clear distinction between published knowledge and historical observations.