# Workflow Architecture

## Purpose

This document describes how work flows through DiaperScout.

The Atlas represents published knowledge.

Workflows describe how information is submitted, validated, processed, reviewed and, where appropriate, incorporated into the Atlas.

Workflows also govern:

* community contribution;
* Discovery Tasks;
* authentication;
* media processing;
* search indexing;
* notifications;
* background processing;
* other long-running operations.

The objective is to ensure meaningful work follows a consistent, observable and resilient lifecycle.

---

# Philosophy

DiaperScout is a workflow-driven system.

Explorers and Contributors do not edit the Atlas directly.

Instead, they initiate actions that preserve evidence, expose meaningful progress and ultimately allow the Atlas to be refined through appropriate editorial processes.

Every workflow should strengthen trust in both the Atlas and the process that maintains it.

---

# Design Principles

Every workflow should be:

* observable;
* deterministic where practical;
* resilient;
* recoverable;
* appropriately event-driven;
* understandable;
* idempotent where retries are possible;
* respectful of contributor effort.

Workflows should expose meaningful progress without exposing unnecessary implementation details.

---

# Workflow Categories

Not every workflow follows the same lifecycle.

DiaperScout primarily contains:

## Knowledge Workflows

Transform community or external information into evidence that may improve the Atlas.

Examples:

* Product Discovery;
* Retail Observation;
* Correction;
* Manufacturer Submission.

---

## Discovery Workflows

Identify Knowledge Gaps and provide structured ways for Explorers to investigate them.

Examples:

* Discovery Tasks;
* regional verification;
* specification completion;
* conflicting evidence investigation.

---

## Platform Workflows

Support the operation of the application.

Examples:

* authentication;
* media processing;
* search indexing;
* notification delivery;
* background maintenance.

Each workflow should use the lifecycle appropriate to its responsibility.

---

# Core Knowledge Workflow

The primary knowledge workflow is:

```text
Observation
    ↓
Validation
    ↓
Evidence Processing
    ↓
Editorial Review
    ↓
Editorial Decision
    ↓
Atlas Update
    ↓
Supporting Systems Updated
```

Not every Observation results in an Atlas update.

Possible outcomes include:

* accepted;
* rejected;
* deferred;
* additional Evidence requested;
* no change required.

The important principle is:

> **Community information enters the Atlas through editorial review.**

---

# Observation Submission

When an Explorer submits an Observation:

1. The Observation is recorded.
2. The submitted information is validated.
3. Supporting Evidence is attached where provided.
4. Media is processed where required.
5. The Observation enters the appropriate workflow.
6. Editorial review occurs where required.
7. An Editorial Decision is recorded.
8. The Atlas is updated only where appropriate.
9. Supporting systems react to the resulting events.

The original Observation and Evidence remain attributable to their contributor.

---

# Observation State

An Observation may move through states such as:

```text
Draft
  ↓
Submitted
  ↓
Under Review
  ├── Evidence Requested
  │       ↓
  │   Resubmitted / Updated
  │
  ├── Accepted
  ├── Rejected
  └── Deferred
```

The exact state machine depends on the Observation type.

Workflow state describes processing.

It must not silently alter the historical factual content of the Observation.

---

# Evidence

Evidence should be preserved before significant processing occurs.

Evidence may include:

* photographs;
* packaging;
* barcode information;
* measurements;
* written notes;
* manufacturer documentation;
* other supporting material.

Processing may create derived representations such as:

* thumbnails;
* extracted barcode values;
* image metadata;
* searchable text.

Derived processing must not destroy the original Evidence.

---

# Product Discovery Workflow

When an unknown Product is discovered:

```text
Unknown Product
      ↓
Observation
      ↓
Evidence
      ↓
Editorial Review
      ↓
Product Created or Matched
      ↓
Atlas Updated
```

The workflow should establish whether:

* the Product is genuinely new;
* it matches an existing Product;
* it represents a new Product Variant;
* it represents a regional variation;
* the evidence is insufficient.

A Contributor must not be able to create canonical Product information directly.

---

# Retail Observation Workflow

Retail Observations record evidence that a Product was seen at a Location.

An observation may record information such as:

* Product;
* Location;
* Retailer context where applicable;
* observation date/time;
* supporting Evidence;
* other approved observation attributes.

Accepted Retail Observations contribute to DiaperScout's understanding of availability.

They do not directly overwrite previous observations.

They do not create guaranteed live stock information.

The resulting availability information is derived from the accumulated evidence.

---

# Availability Freshness

Availability information should take account of the age of supporting Observations.

For example:

```text
Observed today
Observed recently
Observed previously
No recent evidence
```

The precise freshness model belongs to the relevant Product/Availability specification.

The important architectural rule is:

> **Availability is evidence-derived, not a permanent stock state.**

---

# Correction Workflow

Correction Requests challenge existing Atlas knowledge.

The workflow is:

```text
Correction Request
       ↓
Supporting Evidence
       ↓
Editorial Review
       ↓
Decision
       ↓
Atlas Updated if Required
```

Possible outcomes include:

* accepted;
* rejected;
* additional Evidence requested;
* deferred.

The Atlas changes only when an appropriate editorial decision determines that it should.

Historical provenance should remain sufficient to understand why the change occurred.

---

# Manufacturer Workflow

Verified Manufacturers may submit official information.

Manufacturer submissions follow the same core editorial pathway as community contributions.

```text
Verified Manufacturer
        ↓
Submission
        ↓
Evidence
        ↓
Editorial Review
        ↓
Decision
        ↓
Atlas
```

Manufacturer verification establishes identity.

It does not bypass editorial review.

A single editorial pathway maintains consistency between:

* community information;
* manufacturer information;
* other approved sources.

---

# Discovery Task Workflow

The Atlas may identify Knowledge Gaps that could be resolved through further investigation.

These become Discovery Tasks.

Examples include:

* stale Retail Observations;
* conflicting Evidence;
* incomplete Product Specifications;
* unresolved Correction Requests;
* regional uncertainty;
* missing Products.

The workflow is:

```text
Knowledge Gap
      ↓
Discovery Task
      ↓
Explorer chooses to investigate
      ↓
Observation / Evidence
      ↓
Editorial Review
      ↓
Atlas Updated if appropriate
```

Discovery Tasks are voluntary.

They do not create a special community role.

They do not create public ranking.

They do not automatically grant permissions.

---

# Discovery Task Completion

A Discovery Task may result in:

* resolved;
* partially resolved;
* unresolved;
* invalid;
* closed.

Completion describes the state of the Knowledge Gap.

It does not represent a reward level.

A completed Discovery Task may contribute to internal Community Trust where the resulting contribution demonstrates reliable stewardship.

Community Trust remains separate from task state and authorisation.

---

# Community Trust Workflow

Community Trust is an internal signal derived from contribution and moderation history.

A contribution may produce an event such as:

```text
Contribution Accepted
        ↓
Community Contribution Recorded
        ↓
Community Trust Evaluation
```

Community Trust may increase or decrease based on demonstrated reliability.

It should not change simply because an Explorer:

* completes many tasks;
* submits large numbers of observations;
* remains active for a long period.

Quality and stewardship matter more than volume.

Community Trust does not bypass editorial review or automatically grant privileged permissions.

---

# Editorial Workflow

Editorial review is the authoritative gateway into the Atlas.

The general flow is:

```text
Observation
    ↓
Evidence
    ↓
Editorial Review
    ↓
Decision
    ├── Accepted ───────► Atlas
    ├── Rejected
    ├── Deferred
    └── Evidence Requested
```

Editorial decisions should retain sufficient history to explain:

* what was reviewed;
* what Evidence was considered;
* what decision was made;
* when the decision occurred;
* who was responsible for the decision.

---

# Event-Driven Architecture

Meaningful workflow transitions may publish events.

Examples include:

* Observation Submitted;
* Evidence Added;
* Evidence Processed;
* Barcode Recognised;
* Editorial Decision Recorded;
* Atlas Updated;
* Product Published;
* Discovery Task Generated;
* Discovery Task Completed;
* Contribution Accepted;
* Community Trust Updated.

Events describe completed facts.

They do not prescribe how another service must implement its response.

Each consuming service decides how the event affects its own responsibility.

---

# Event Principles

Events should:

* represent meaningful domain changes;
* contain sufficient information for consumers;
* be versionable;
* avoid unnecessary internal implementation details;
* support idempotent processing where appropriate.

Not every database change requires an event.

Events should exist where another responsibility genuinely needs to react.

---

# Long-Running Work

Some operations require asynchronous processing.

Examples include:

* image optimisation;
* thumbnail generation;
* barcode processing;
* search indexing;
* Discovery Task generation;
* notification delivery;
* Community Trust evaluation;
* media processing.

Long-running work should continue independently of the Explorer's active session.

The original contribution should already be safely persisted before asynchronous work begins.

---

# Progress Reporting

Explorers should receive meaningful progress information where an operation takes time.

Examples include:

* ✓ Observation received
* ✓ Evidence saved
* ✓ Images processed
* ✓ Submitted for review
* ✓ Published

Progress should communicate meaningful milestones rather than technical implementation details.

The objective is confidence rather than verbosity.

---

# Offline Operation

The Progressive Web App should support temporary loss of connectivity where practical.

Offline-capable workflows may include:

* Observation drafting;
* Observation submission;
* Evidence capture;
* media preparation.

Where a workflow can operate offline:

1. Work is stored locally.
2. The Explorer is shown that the work is safely stored.
3. Synchronisation occurs when connectivity returns.
4. The server validates and accepts the submission.
5. Normal editorial processing continues.

The application must clearly distinguish:

> **Saved locally**

from:

> **Submitted to DiaperScout**

An Explorer should never be told that an Observation has been submitted until the server has confirmed receipt.

---

# Failure Recovery

Workflows should tolerate recoverable failures.

Examples include:

* temporary network failure;
* object storage failure;
* search indexing failure;
* notification delivery failure;
* transient database errors;
* background worker interruption.

Where practical:

* interrupted work should resume automatically;
* recoverable background failures should retry;
* operations should be idempotent;
* failed work should be visible to operators;
* Explorers should only be interrupted when additional action is genuinely required.

Protecting contributor effort is more important than immediate completion.

---

# Workflow History

Significant workflow milestones should remain available as history.

Examples include:

* submitted;
* validated;
* evidence requested;
* under review;
* accepted;
* rejected;
* published.

Workflow history should provide transparency without exposing unnecessary implementation detail.

Historical workflow information must not be confused with the canonical Atlas itself.

---

# Workflow Ownership

Each workflow stage has a clear owner.

| Stage                  | Logical Owner                |
| ---------------------- | ---------------------------- |
| Authentication         | Authentication Service       |
| Observation submission | Observation Service          |
| Evidence processing    | Observation / Media Services |
| Editorial review       | Editorial Service            |
| Atlas publication      | Atlas Service                |
| Discovery Tasks        | Discovery Service            |
| Community Trust        | Community Service            |
| Search indexing        | Search Service               |
| Notifications          | Notification Service         |

Ownership prevents multiple services from independently modifying the same state.

---

# Transaction Boundaries

Workflows should use the smallest transaction boundary capable of preserving consistency.

For example:

```text
Save Observation
       ↓
Commit
       ↓
Publish Observation Submitted
```

Long-running processing should not hold database transactions open unnecessarily.

Asynchronous processing should use durable state and idempotent handlers.

---

# Concurrency

Concurrent workflow actions must be handled explicitly.

Examples include:

* two Moderators reviewing the same Observation;
* two Explorers attempting to participate in the same Discovery Task;
* multiple processes updating a workflow;
* an Observation being processed while an editorial decision is made.

Optimistic concurrency should be preferred where appropriate.

Conflicts should produce clear outcomes rather than silent overwrites.

---

# Security

Every workflow must enforce authorisation server-side.

Examples:

* Explorers may submit Observations.
* Moderators may perform editorial decisions.
* Administrators may manage platform configuration.
* Verified Manufacturers may submit authorised manufacturer information.

No workflow should rely on client-side state to determine permission.

Community Trust must not be treated as an automatic security credential.

---

# Auditability

Significant workflow transitions should be auditable.

The system should be able to answer:

* What happened?
* When did it happen?
* Which entity changed?
* Which User initiated it?
* What evidence supported it?
* What decision followed?
* Which services processed the resulting event?

Audit information should remain separate from the public-facing Atlas where appropriate.

---

# Supporting Systems

After an Atlas update, supporting systems may react.

Examples include:

```text
Atlas Updated
     │
     ├── Search Index Updated
     ├── Cache Invalidated
     ├── Notifications Generated
     ├── Discovery Gaps Recalculated
     └── Analytics Recorded
```

Supporting systems must not prevent the Atlas transaction from completing unless their participation is genuinely required for consistency.

---

# Architectural Consequences

This workflow architecture produces several important characteristics.

* Explorers and Contributors submit information rather than editing the Atlas.
* Evidence is preserved before significant processing.
* Editorial review remains the single pathway into canonical knowledge.
* Discovery Tasks provide voluntary mechanisms for resolving Knowledge Gaps.
* Community Trust remains an internal stewardship signal.
* Manufacturer submissions follow the same editorial pathway.
* Retail availability is derived from observations.
* Long-running processing is asynchronous where appropriate.
* Supporting systems react to meaningful events.
* Offline work is never represented as server-submitted until confirmed.
* Workflow history preserves meaningful provenance.
* No `Scout` concept exists in the production workflow model.

---

# Evolution

Future workflows should follow the same architectural principles.

Every new workflow should clearly define:

* its initiating action;
* its authoritative owner;
* its state;
* its evidence requirements;
* its failure modes;
* its security requirements;
* its completion criteria;
* its events;
* its audit requirements.

Not every workflow needs to follow the exact Observation → Evidence → Editorial → Atlas lifecycle.

However, every workflow that changes canonical knowledge must ultimately pass through the appropriate editorial authority.

Consistency of principles is more important than forcing every feature into an identical state machine.

---

# Relationship to Other Documents

This document describes how work moves through DiaperScout.

Related documents describe the architecture from complementary perspectives.

* **Knowledge Architecture** explains how workflows build knowledge.
* **Editorial Architecture** explains how workflows establish trusted knowledge.
* **Backend Services** defines the services responsible for each stage.
* **API Architecture** describes how workflows are initiated and consumed.
* **Discovery Task System** defines Knowledge Gaps and Discovery Tasks.
* **Authentication & Roles** defines identity and authorisation.
* **Database Model** defines persistence and historical preservation.

Together these documents describe how DiaperScout continuously refines the Atlas through structured, evidence-based community contribution.
