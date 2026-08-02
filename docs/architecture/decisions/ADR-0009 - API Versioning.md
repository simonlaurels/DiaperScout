# ADR-0009 — API Versioning

## Status

Accepted

---

## Context

The DiaperScout API is the public interface to the Atlas.

As the platform evolves, new capabilities will be introduced and existing resources may require refinement.

A decision was required regarding how changes to the API should be managed while maintaining compatibility for existing clients.

One approach would allow the API to evolve continuously without explicit versioning.

Another would require every change to create a new API version.

A balanced approach was needed that supports long-term stability while allowing the platform to evolve.

---

## Decision

DiaperScout adopts **explicit API versioning for breaking changes**.

The API should evolve through additive changes wherever practical.

Examples include:

- new resources;
- optional fields;
- additional endpoints;
- expanded capabilities.

Breaking changes should only be introduced through a new API version.

Existing versions should remain available for an appropriate migration period before retirement.

Clients should be given sufficient notice to migrate.

---

## Consequences

This decision has several important consequences.

### Positive

- Client applications remain stable.
- New capabilities can be introduced without disrupting existing integrations.
- API evolution becomes deliberate rather than accidental.
- Third-party integrations have predictable upgrade paths.
- The public contract remains trustworthy.

### Trade-offs

- Multiple API versions may need to be supported simultaneously.
- Breaking changes require additional planning and documentation.
- Deprecated functionality must be maintained until retirement.

These trade-offs are considered worthwhile because they preserve confidence in the public API.

---

## Alternatives Considered

### No Versioning

Allow the API to evolve without explicit versions.

Rejected because breaking changes would unexpectedly affect existing clients.

---

### Version Every Release

Create a new API version for every platform release.

Rejected because it would introduce unnecessary complexity and discourage additive evolution.

---

### Client-Specific APIs

Allow different clients to consume different APIs.

Rejected because it weakens the API First architecture and increases maintenance effort.

---

## Related Documents

- API Architecture
- Backend Services
- Deployment & Operations

---

## Notes

Versioning protects the public contract rather than the implementation.

Where practical, APIs should evolve through additive changes.

Creating a new version should be considered a significant architectural event rather than a routine development activity.

This decision reinforces the principle that the API represents the Atlas through a stable and predictable interface while allowing the platform to evolve over time.