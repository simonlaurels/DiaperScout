# ADR-0001 — Atlas First

## Status

Accepted

---

## Context

DiaperScout is fundamentally different from a traditional product database or retail catalogue.

The purpose of the platform is not merely to store information about products.

Its purpose is to build and preserve a trustworthy Explorer's Atlas through structured community observation and editorial stewardship.

Several architectural approaches were considered.

One approach treats the application as the primary product, with the database acting as its persistence layer.

Another approach treats the Atlas itself as the primary product, with applications acting as different ways of exploring and contributing to that Atlas.

Choosing between these approaches influences every subsequent architectural decision.

---

## Decision

DiaperScout adopts an **Atlas First** architecture.

The Atlas is the primary product.

Applications exist to present, explore and improve the Atlas.

Every architectural component should ultimately support one or more of the following objectives:

- preserving trustworthy knowledge;
- expanding the Atlas through structured observations;
- protecting editorial integrity;
- presenting the Atlas consistently to users.

Implementation details such as databases, APIs, web applications and deployment platforms exist to support the Atlas rather than define it.

---

## Consequences

This decision has several important consequences.

### Positive

- The architecture remains focused on preserving knowledge rather than software.
- Multiple applications can share the same Atlas.
- APIs naturally expose the Atlas rather than implementation details.
- Editorial workflows become central to the architecture.
- Long-term maintainability is improved because technology choices remain secondary.

### Trade-offs

- Some implementation approaches that optimise for convenience may conflict with Atlas integrity.
- Additional architectural layers are required to separate observations from published knowledge.
- Editorial workflows introduce additional complexity compared to direct editing.

These trade-offs are considered worthwhile because they strengthen the long-term quality and trustworthiness of the Atlas.

---

## Alternatives Considered

### Application First

Treat the web application as the primary product.

Rejected because it places too much emphasis on implementation rather than knowledge.

---

### Database First

Treat the database schema as the central design artefact.

Rejected because persistence is an implementation concern rather than the purpose of the project.

---

### Community First

Treat community interaction as the primary architectural concern.

Rejected because the community exists to improve the Atlas rather than becoming the product itself.

---

## Related Documents

- Architecture Principles
- Knowledge Architecture
- Domain Model
- API Architecture
- Deployment & Operations

---

## Notes

This is the foundational Architecture Decision Record for DiaperScout.

Future ADRs should be consistent with this principle.

Where a proposed implementation conflicts with the concept of the Atlas as the primary product, the implementation should normally be reconsidered before the architectural philosophy is compromised.