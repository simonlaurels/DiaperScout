# Workflow Architecture

## Purpose

This document describes how work flows through DiaperScout.

Unlike the Atlas, which represents published knowledge, workflows describe how observations become evidence, how evidence becomes trusted knowledge and how the Atlas continually evolves.

Workflows also govern long-running operations, community contribution and background processing.

The objective is to ensure every meaningful action follows a consistent, observable and resilient lifecycle.

---

# Philosophy

DiaperScout is a workflow-driven system.

Contributors do not edit the Atlas directly.

Instead, they begin workflows that preserve evidence, expose progress and ultimately refine the Atlas.

Every workflow should strengthen trust in both the Atlas and the process that maintains it.

---

# Design Principles

Every workflow should be:

- observable;
- deterministic;
- resilient;
- recoverable;
- event-driven;
- understandable.

Contributors should always understand what is happening to their work.

---

# Workflow Lifecycle

Although individual workflows differ, they share a common lifecycle.

```text
Observation

↓

Validation

↓

Processing

↓

Editorial Review

↓

Decision

↓

Atlas Updated

↓

Supporting Systems Updated
```

Every workflow should preserve evidence before performing any further processing.

---

# Event-Driven Architecture

Workflows progress through events rather than scheduled maintenance.

Examples include:

- Observation Submitted
- Evidence Validated
- Barcode Recognised
- Editorial Decision Recorded
- Atlas Updated
- Product Published
- Scout Task Generated

Events describe completed work.

They do not dictate how other services respond.

Each service reacts according to its own responsibilities.

---

# Long-Running Work

Some operations require asynchronous processing.

Examples include:

- image optimisation;
- thumbnail generation;
- barcode verification;
- search indexing;
- Scout Task generation;
- trust recalculation.

Long-running work should continue independently of the contributor's session.

The contributor's work should already be safely recorded.

---

# Progress Reporting

Contributors should receive meaningful progress updates.

Examples include:

- ✓ Observation received
- ✓ Evidence validated
- ✓ Images processed
- ✓ Ready for editorial review
- ✓ Published

Progress should communicate meaningful milestones rather than technical implementation details.

The objective is confidence rather than verbosity.

---

# Discovery Workflow

When an unknown product is discovered:

1. An Observation is recorded.
2. Supporting Evidence is attached.
3. Media is processed.
4. Editorial review occurs.
5. A Product may be created.
6. The Atlas is updated.
7. Supporting systems respond.

The contributor should always understand where the discovery currently sits within this process.

---

# Retail Observation Workflow

Retail observations strengthen the Atlas' understanding of product availability.

Each observation records:

- retailer;
- location;
- timestamp;
- optional price;
- supporting evidence.

Accepted observations contribute towards the Atlas' understanding of retailer availability.

They do not directly overwrite previous knowledge.

---

# Correction Workflow

Correction requests challenge existing Atlas knowledge.

They enter editorial review together with supporting evidence.

Possible outcomes include:

- accepted;
- rejected;
- additional evidence requested;
- deferred.

The Atlas changes only when an editorial decision determines that it should.

---

# Manufacturer Workflow

Verified manufacturers contribute official observations.

Manufacturer submissions follow the same workflow as every other observation.

Their authority influences editorial confidence.

It does not bypass editorial review.

Maintaining a single editorial pathway strengthens the consistency of the architecture.

---

# Scout Task Workflow

The Atlas continually identifies opportunities for improvement.

Examples include:

- stale retailer observations;
- conflicting evidence;
- incomplete Product Specifications;
- unresolved correction requests.

These opportunities become Scout Tasks.

Completed Scout Tasks generate new Observations, beginning another cycle of refinement.

---

# Offline Operation

The Progressive Web App should support temporary loss of connectivity.

Where practical:

- Observations should be stored locally.
- Media uploads should resume automatically.
- Workflows should continue once connectivity returns.

Contributors should never lose completed work because connectivity was interrupted.

---

# Failure Recovery

Workflows should be resilient.

Temporary failures should not result in lost knowledge.

Where practical:

- interrupted work should resume automatically;
- background processing should retry recoverable failures;
- contributors should only be interrupted when additional action is genuinely required.

Protecting contributor effort is more important than immediate completion.

---

# Workflow History

Significant workflow milestones should remain visible.

Examples include:

- submitted;
- under review;
- evidence requested;
- accepted;
- rejected;
- published.

Workflow history provides transparency without exposing unnecessary implementation detail.

---

# Architectural Consequences

This workflow architecture produces several important characteristics.

- Contributors submit Observations rather than editing the Atlas.
- Evidence is always preserved before processing.
- Long-running work remains observable.
- Editorial review remains the single pathway into the Atlas.
- Supporting services react to events rather than polling for change.
- Every completed workflow strengthens the Atlas.

---

# Evolution

Future workflows should follow the same architectural principles.

Every workflow should:

- begin with an Observation;
- preserve supporting Evidence;
- expose meaningful progress;
- conclude with a clear editorial outcome;
- strengthen the Atlas.

Consistency across workflows is more valuable than introducing specialised behaviour for individual features.

---

# Relationship to Other Documents

This document describes how work moves through DiaperScout.

Related documents describe the architecture from complementary perspectives.

- **Knowledge Architecture** explains how workflows build knowledge.
- **Editorial Architecture** explains how workflows establish trust.
- **Backend Services** defines the services responsible for each stage.
- **API Architecture** describes how workflows are initiated by clients.

Together these documents explain how DiaperScout continuously refines the Atlas through structured community contribution.