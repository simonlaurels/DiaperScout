# Community Architecture

## Purpose

This document defines the architecture of the DiaperScout community.

It describes the major concepts that make up the community, the relationships between those concepts, and the responsibilities of each.

It intentionally avoids describing workflows, user interface behaviour or technical implementation.

The philosophy behind this architecture is described in *The Guide and its Scouts*.

---

# Architectural Principles

The community architecture is guided by several core principles.

- The Guide is the primary product.
- Every product deserves documentation.
- Every Explorer deserves respect.
- Observations become evidence.
- Trust adds weight to evidence.
- Evidence is challenged by more evidence.
- Recognition celebrates contribution rather than competition.
- Automation should gather information that computers can reliably discover.
- Scouts should contribute knowledge that only people can discover.

Every architectural decision should support these principles.

---

# Community Overview

The DiaperScout community exists to improve the Guide.

It is composed of four distinct participants.

- Explorers
- Scouts
- Editors
- Automation

Each has a different responsibility.

None replaces another.

Together they continuously improve the Guide.

```text
              Automation
                    │
                    │
Explorer ──► Guide ◄── Scout
                    ▲
                    │
                 Editors
```

---

# Explorers

Explorers use the Guide.

They search for products.

They discover retailers.

They compare products.

They learn from the experiences of Scouts.

Every account begins as an Explorer.

Contributing is never required.

---

# Scouts

Scouts improve the Guide.

A Scout is an Explorer whose first accepted contribution has become part of the Guide.

Scouts contribute knowledge from:

- the physical world
- careful research

Scouts do not create truth.

They create observations.

Those observations become evidence.

Each Scout has:

- a Scout Callsign
- a Backpack
- public contributions
- earned pins
- internal trust

---

# Backpacks

The Backpack represents the Scout rather than their popularity.

It tells the story of the Scout's journey.

It exists to celebrate meaningful contribution.

A Backpack may include:

- pinned Scout Pins
- contribution highlights
- notable discoveries
- public observations
- occasional impact summaries

Backpacks intentionally avoid traditional social networking concepts such as followers, popularity scores or public rankings.

---

# Observations

Observations are the foundation of community knowledge.

Every observation represents something observed by a Scout.

An observation always has:

- an author
- a subject
- a point in time

Many observations also include:

- a retailer
- a location
- photographs
- measurements
- supporting information

Observations remain attributed to the Scout who created them.

Corrections are transparent.

Observation history is preserved.

---

# Evidence

Individual observations become evidence.

Evidence represents the Guide's understanding of the world.

Evidence is built from many independent observations.

Evidence is never created by voting.

Evidence naturally evolves as additional observations are gathered.

The Guide presents summaries of evidence while preserving the underlying observations.

---

# Trust

Trust is an internal property of the Guide.

Trust belongs to Scouts.

Trust is earned through consistently contributing high-quality observations.

Trust is never intended to become a public reputation system.

Trust exists solely to improve the quality of evidence presented by the Guide.

---

# Product Specifications

Product Specifications remain independent from Community Observations.

Specifications describe products.

Observations describe experiences.

Specifications contain factual information obtained from authoritative sources.

Observations provide evidence gathered by the community.

Neither replaces the other.

Together they help Explorers understand products.

---

# Candidate Products

Unknown products initially become Candidate Products.

Candidate Products preserve discoveries while their identity is established.

They allow observations to accumulate before editorial review.

Candidate Products may ultimately become:

- verified products
- identified product variants
- merged duplicates

No genuine observation should be lost because a product's identity changes.

---

# Editors

Editors preserve the integrity of the Guide.

Editors are custodians rather than judges.

Editors do not determine truth.

Editors:

- verify Product Specifications
- verify Candidate Products
- merge duplicate products
- perform transparent corrections
- preserve historical information

Editors maintain the Guide without replacing community evidence.

---

# Automation

Automation complements the community.

Its purpose is to remove repetitive work wherever reliable automation is possible.

Automation should gather information that computers can discover consistently.

Examples include:

- manufacturer data
- online retailer information
- catalogue imports
- metadata updates

Automation should never replace genuine community observations from the physical world.

---

# Community Recognition

Recognition exists to celebrate contribution.

Recognition does not create competition.

DiaperScout intentionally avoids:

- leaderboards
- follower counts
- popularity rankings
- engagement metrics

Instead, the community recognises:

- meaningful discoveries
- careful documentation
- stewardship
- long-term contribution

Scout Pins and Backpacks celebrate a Scout's journey rather than their status.

---

# Knowledge Flow

Knowledge moves through the Guide in a consistent way.

```text
Explorer
      │
      ▼
Scout
      │
creates
      ▼
Observation
      │
becomes
      ▼
Evidence
      │
improves
      ▼
Guide
      │
helps
      ▼
Explorer
```

Editors preserve the integrity of the Guide throughout this process.

Automation contributes wherever reliable information can be gathered without human observation.

---

# Summary

The community exists for one purpose:

To help the Guide better understand the world.

Every concept within this architecture exists to support that purpose.

If a proposed feature does not improve the Guide, it should be reconsidered.