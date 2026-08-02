# ADR-0010 — Technology Independence

## Status

Accepted

---

## Context

Technology changes continuously.

Programming languages, databases, cloud providers, storage systems and deployment platforms all evolve over time.

DiaperScout is intended to become a long-lived project.

The architecture should therefore outlast the individual technologies used to implement it.

A decision was required regarding the relationship between the architecture and its implementation technologies.

One approach would tightly couple the architecture to specific frameworks and platforms.

Another would define the architecture independently, allowing implementation technologies to change without fundamentally altering the conceptual model.

---

## Decision

DiaperScout adopts a **Technology Independent** architecture.

The architecture defines:

- concepts;
- responsibilities;
- workflows;
- relationships;
- public contracts.

Implementation technologies realise those concepts.

They do not define them.

Technology choices should support the architecture rather than shape it.

When technologies become obsolete, they should be replaceable without changing the underlying architectural principles.

---

## Consequences

This decision has several important consequences.

### Positive

- The architecture remains stable over time.
- Technologies can be replaced incrementally.
- Documentation focuses on enduring concepts rather than transient implementation details.
- Contributors can evaluate new technologies against architectural principles.
- Long-term maintenance is simplified.

### Trade-offs

- Additional abstraction may occasionally be required between architecture and implementation.
- Technology-specific optimisations may need to be balanced against architectural consistency.
- Implementation documentation becomes an important companion to the architecture.

These trade-offs are considered worthwhile because they preserve the longevity of the project.

---

## Alternatives Considered

### Framework-Driven Architecture

Allow the chosen framework or platform to dictate the architectural structure.

Rejected because architectural decisions become tied to technology lifecycles rather than project requirements.

---

### Vendor-Specific Design

Optimise the architecture around a single cloud provider or technology stack.

Rejected because it reduces portability and makes future migration more difficult.

---

### Implementation-First Documentation

Describe the architecture primarily in terms of implementation technologies.

Rejected because implementation choices change more frequently than architectural principles.

---

## Related Documents

- Architecture Principles
- Backend Services
- Database Model
- Deployment & Operations

---

## Notes

Technology Independence does not imply technology neutrality.

Implementation technologies should be selected deliberately and documented through Architecture Decision Records.

For example:

- a database platform;
- an object storage solution;
- a messaging system;
- an authentication provider.

These are implementation decisions.

They should support the architecture without redefining it.

As DiaperScout evolves, implementation technologies may change.

The architecture should remain recognisable.

Future contributors should be able to understand the system by reading the architecture documentation without first understanding the technologies used to implement it.

This decision reinforces the principle that the Atlas is the enduring asset of the project.

Software exists to build, protect and present the Atlas.

The technologies used to achieve that goal are expected to evolve throughout the lifetime of the project.