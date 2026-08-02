# Scout Task System

## Purpose

This document describes how DiaperScout generates, prioritises and manages Scout Tasks.

Scout Tasks direct community effort towards areas where additional evidence would most improve the Atlas.

Rather than relying solely on contributors to decide what to investigate, the Atlas continuously identifies where its own understanding can be strengthened.

The objective is not to create activity.

The objective is to improve the quality, confidence and completeness of the Atlas.

---

# Philosophy

Scouts are explorers.

They are also custodians of the Atlas.

Scout Tasks exist because the Atlas recognises uncertainty within its own knowledge.

Every task should answer a genuine question that the Atlas cannot currently answer with sufficient confidence.

The Atlas therefore becomes an active participant in its own improvement.

---

# Design Principles

Scout Tasks should:

- strengthen the Atlas;
- encourage meaningful exploration;
- improve confidence;
- avoid unnecessary duplication;
- prioritise evidence over activity;
- remain understandable.

Every task should have a clear reason for existing.

---

# How Tasks are Created

Scout Tasks are generated automatically by the architecture.

Tasks arise whenever additional evidence would improve the Atlas.

Examples include:

- stale observations;
- conflicting evidence;
- incomplete Product Specifications;
- unresolved correction requests;
- newly discovered products.

Task generation should be deterministic and explainable.

---

# Task Categories

## Availability Confirmation

A Product has not been observed at a retailer for an extended period.

Example:

```text
BetterDry M10

Tesco Yate

Last confirmed 94 days ago.
```

Scout Task:

> Confirm whether this Product is still available.

---

## New Product Discovery

An unknown barcode has been submitted.

Scout Task:

> Help identify and document this Product.

---

## Conflicting Evidence

Existing observations disagree.

Examples include:

- conflicting retailer observations;
- contradictory evidence;
- manufacturer clarification required.

Scout Task:

> Gather additional evidence.

---

## Correction Investigation

A contributor has challenged existing Atlas knowledge.

Scout Task:

> Investigate the reported discrepancy.

---

## Regional Verification

Evidence suggests Products differ between countries or regions.

Scout Task:

> Verify the regional variation.

---

## Specification Completion

A Product exists within the Atlas but important specification data is incomplete.

Scout Task:

> Help complete the Product Specification.

---

# Prioritisation

Not every task has equal value.

The architecture should prioritise tasks that most improve the Atlas.

Examples include:

- unresolved Product discoveries;
- conflicting evidence;
- stale retailer observations;
- incomplete Product Specifications.

The objective is to maximise community impact rather than simply generating work.

---

# Geographic Awareness

Where appropriate, Scout Tasks should be geographically relevant.

Examples include:

- nearby retailers;
- local availability confirmation;
- regional investigations.

Scouts should normally receive tasks they are realistically able to complete.

---

# Task Lifecycle

Scout Tasks follow a consistent lifecycle.

```text
Knowledge Gap

↓

Scout Task Created

↓

Observation Submitted

↓

Editorial Review

↓

Atlas Updated

↓

Task Completed
```

The task itself never changes the Atlas.

The Observation created by completing the task follows the standard editorial workflow.

---

# Task Completion

Completing a Scout Task always generates a new Observation.

That Observation enters the same editorial workflow as every other contribution.

Scout Tasks never bypass the editorial architecture.

Every contribution continues to follow:

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

# Task Expiration

Scout Tasks should naturally expire when they are no longer useful.

Examples include:

- another Scout has completed the task;
- newer evidence resolves the uncertainty;
- editorial review determines no further investigation is required.

The Atlas should avoid asking contributors to perform unnecessary work.

---

# Duplicate Prevention

The architecture should avoid generating duplicate tasks.

Where several uncertainties can be resolved by a single investigation, they should normally become one Scout Task.

Reducing duplication improves both contributor experience and Atlas quality.

---

# Trust

Successfully completing Scout Tasks contributes positively towards community trust.

The quality of submitted evidence is always more important than the number of completed tasks.

Stewardship should be recognised.

Activity alone should not.

---

# Transparency

Every Scout Task should explain why it exists.

Examples include:

- Last confirmed 97 days ago.
- Product recently discovered.
- Conflicting observations require investigation.
- Product Specification incomplete.
- Manufacturer clarification requested.

Understanding the reason behind a task improves trust, motivation and contribution quality.

---

# Atlas Maintenance

Scout Tasks allow the Atlas to maintain itself through community exploration.

Rather than assuming old information remains correct, the Atlas continually asks the community to strengthen areas of uncertainty.

This allows the Atlas to evolve continuously without relying upon arbitrary expiry dates.

---

# Architectural Consequences

The Scout Task architecture results in several important characteristics.

- Community effort is directed where it provides the greatest value.
- The Atlas actively identifies gaps in its own knowledge.
- Every completed task generates new evidence.
- Editorial review remains the only pathway into canonical knowledge.
- Community activity remains purposeful rather than arbitrary.

---

# Evolution

As DiaperScout evolves, additional categories of Scout Task may emerge.

Future tasks should continue to satisfy one simple principle:

> **Every Scout Task should improve the Atlas.**

Tasks should never exist simply to encourage activity.

They exist because the Atlas genuinely benefits from the additional evidence.

---

# Relationship to Other Documents

This document describes how DiaperScout directs community effort towards maintaining the Atlas.

Related documents describe the surrounding architecture.

- **Workflow Architecture** explains how completed Scout Tasks become Observations.
- **Knowledge Architecture** explains how those Observations become knowledge.
- **Editorial Architecture** explains how the resulting evidence is evaluated.
- **Authentication & Roles** describes the responsibilities of Scouts and other contributors.

Together these documents define how the Atlas continually improves itself through structured community stewardship.