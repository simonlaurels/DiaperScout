# ADR-0004 — Observation-Based Knowledge

## Status

Accepted

---

## Context

Traditional applications typically allow users to create or edit records directly.

Changes immediately become the current state of the database.

DiaperScout has a different objective.

The Atlas is intended to represent a trustworthy understanding of the real world rather than simply reflecting the most recent edit.

This required a decision about how knowledge should be represented within the system.

One option was to allow direct modification of canonical records.

Another was to treat every contribution as an observation of reality that may, following editorial review, influence the Atlas.

---

## Decision

DiaperScout adopts an **Observation-Based Knowledge Model**.

Knowledge is not created by editing records.

Knowledge emerges through structured observations supported by evidence.

Every significant contribution begins as an Observation.

Observations describe reality.

Evidence supports those observations.

Editorial review determines how those observations influence Canonical Knowledge.

The Atlas represents the current editorial understanding of the world rather than the complete collection of observations.

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
```

---

## Consequences

This decision has several important consequences.

### Positive

- Every published fact has supporting evidence.
- Provenance is preserved.
- Historical observations remain available for future review.
- Knowledge naturally evolves as new observations are made.
- Future workflows integrate into a single conceptual model.

### Trade-offs

- The architecture is more complex than a traditional CRUD application.
- Additional storage is required for observations and evidence.
- Editorial review becomes an essential architectural responsibility.

These trade-offs are considered worthwhile because they produce an Atlas that can explain why it believes something rather than simply storing the latest value.

---

## Alternatives Considered

### Direct Record Editing

Allow contributors to edit Products directly.

Rejected because it destroys the distinction between evidence and published knowledge.

---

### Event Log Only

Treat every change as a technical event.

Rejected because implementation events do not necessarily represent meaningful observations about the real world.

The architecture models observations rather than software events.

---

### Database as Source of Truth

Treat the latest database values as authoritative.

Rejected because the database is a persistence mechanism rather than the conceptual source of truth.

The Atlas is the published source of truth.

---

## Related Documents

- Knowledge Architecture
- Domain Model
- Editorial Architecture
- Workflow Architecture
- Database Model

---

## Notes

Observations represent reality.

The Atlas represents the current understanding of reality.

These are intentionally different concepts.

As additional observations are made, the Atlas is refined rather than replaced.

This distinction allows DiaperScout to preserve historical evidence, explain editorial decisions and continually improve the quality of its published knowledge without losing the journey by which that knowledge was established.