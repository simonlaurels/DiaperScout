# ADR-0007 — Event-Driven Workflows

## Status

Accepted

---

## Context

Many operations within DiaperScout occur after an Observation has been submitted.

Examples include:

- media processing;
- thumbnail generation;
- barcode verification;
- search indexing;
- Scout Task generation;
- trust recalculation;
- retailer confidence updates.

One architectural approach would be to perform these operations synchronously as part of the original request.

Another would be to execute them using scheduled background jobs.

A third approach is to allow services to react to significant events as they occur.

A decision was required to determine how workflows should progress through the system.

---

## Decision

DiaperScout adopts an **Event-Driven Workflow** architecture.

Services publish events when significant work has been completed.

Other services react to those events according to their own responsibilities.

Events describe something that has already happened.

They do not instruct other services what to do.

For example:

```text
Observation Submitted

↓

Evidence Validated

↓

Editorial Decision Recorded

↓

Atlas Updated

↓

Search Indexed

↓

Scout Tasks Evaluated
```

Each service remains responsible for its own behaviour.

---

## Consequences

This decision has several important consequences.

### Positive

- Services remain loosely coupled.
- Long-running work can occur asynchronously.
- New capabilities can respond to existing events without modifying existing services.
- Contributors receive immediate acknowledgement that their work has been safely recorded.
- Workflows remain observable and resilient.

### Trade-offs

- Workflow execution becomes distributed across multiple services.
- Event sequencing requires careful design.
- Failures must be recoverable without losing events.
- Additional operational monitoring is required.

These trade-offs are considered worthwhile because they produce a more resilient and extensible architecture.

---

## Alternatives Considered

### Synchronous Processing

Perform all work within the original request.

Rejected because long-running operations would delay user responses and increase the impact of failures.

---

### Scheduled Batch Processing

Execute work periodically using scheduled jobs.

Rejected because it introduces unnecessary delays, increases operational complexity and prevents immediate refinement of the Atlas.

---

### Direct Service Calls

Allow services to call one another directly to complete workflows.

Rejected because it tightly couples services and makes future evolution more difficult.

---

## Related Documents

- Workflow Architecture
- Backend Services
- Deployment & Operations
- API Architecture

---

## Notes

Events describe completed facts rather than requested actions.

For example:

Good event:

> Observation Submitted

Poor event:

> Generate Search Index

This distinction keeps services independent and allows the architecture to evolve without creating tightly coupled workflows.

Event-driven processing is an implementation pattern that supports the architectural principles of continuous refinement, resilience and clear separation of responsibilities.

It is not intended to replace the editorial workflow or alter the knowledge model.

Instead, it provides an effective mechanism for allowing the rest of the platform to respond whenever the Atlas evolves.