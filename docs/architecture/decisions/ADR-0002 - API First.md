# ADR-0002 — API First

## Status

Accepted

---

## Context

DiaperScout is expected to support multiple ways of interacting with the Atlas.

Current and future clients may include:

- Progressive Web App (PWA)
- Administrative tools
- Moderator interfaces
- Future native mobile applications
- Third-party integrations
- Automated testing tools

Without a common interface, business rules would risk becoming duplicated across multiple applications.

This would increase maintenance effort, reduce consistency and make future expansion more difficult.

A single architectural approach was required to ensure every client experiences the Atlas in the same way.

---

## Decision

DiaperScout adopts an **API First** architecture.

The public API is the authoritative interface to the Atlas.

Every client communicates with the backend exclusively through published API contracts.

No client receives privileged access to backend services or persistent storage.

Business rules are implemented once within backend services and exposed consistently through the API.

Applications present information.

They do not own business logic.

---

## Consequences

This decision has several important consequences.

### Positive

- Every client behaves consistently.
- Business rules exist in one place.
- Future clients can be developed without redesigning the backend.
- Automated testing can target the same public interface used by production applications.
- Backend services remain independent of presentation technologies.

### Trade-offs

- Every capability must first be exposed through the API before clients can use it.
- API design requires additional thought and discipline.
- Poor API design would affect every client simultaneously.

These trade-offs are considered worthwhile because they produce a simpler and more maintainable architecture.

---

## Alternatives Considered

### Web Application First

Allow the Progressive Web App to communicate directly with backend components.

Rejected because it would tightly couple the backend to a single client.

---

### Multiple Client APIs

Allow different applications to expose different APIs.

Rejected because it would duplicate business rules and create inconsistent behaviour.

---

### Shared Database Access

Allow trusted applications to access the database directly.

Rejected because it bypasses backend services, weakens architectural boundaries and exposes implementation details.

---

## Related Documents

- Architecture Principles
- Backend Services
- API Architecture
- Authentication & Roles

---

## Notes

The API represents the Atlas.

It does not expose database structures or implementation details.

Clients should think in terms of Products, Observations, Scout Tasks and other domain concepts rather than tables, services or storage technologies.

Future implementation technologies may change.

The API contract should remain stable wherever practical.

This decision reinforces the principle that the Atlas is the product and that every client should experience it through the same consistent public interface.