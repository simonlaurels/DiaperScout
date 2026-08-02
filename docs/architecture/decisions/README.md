# Architecture Decision Records

## Purpose

Architecture Decision Records (ADRs) capture significant architectural and technical decisions made during the implementation of DiaperScout.

The architecture documents describe the enduring philosophy of the project.

ADRs record the implementation decisions made while applying that philosophy.

Together they explain both **why** the architecture exists and **why** specific implementation choices were made.

---

# Relationship to the Architecture

The Architecture documentation defines principles that are expected to remain stable for many years.

ADRs document implementation choices that may evolve as technology changes.

For example:

Architecture Principle:

> The API is the public interface to the Atlas.

ADR:

> ASP.NET Core Minimal APIs were selected to implement the public API.

If implementation changes in the future, the ADR changes.

The architectural principle does not.

---

# When to Create an ADR

An ADR should be created whenever a decision is expected to have long-term architectural significance.

Examples include:

- selecting a database technology;
- introducing a new architectural pattern;
- choosing an authentication provider;
- changing storage architecture;
- introducing a major dependency;
- adopting a deployment strategy.

Routine implementation decisions should not normally require ADRs.

---

# ADR Lifecycle

Every ADR has one of the following statuses.

| Status | Meaning |
|---------|---------|
| Proposed | Under discussion |
| Accepted | Adopted |
| Superseded | Replaced by a later ADR |
| Rejected | Considered but not adopted |

Accepted ADRs should normally remain unchanged.

If a decision changes, create a new ADR that supersedes the previous one.

This preserves the historical reasoning behind the project.

---

# Numbering

ADRs use sequential numbering.

Examples:

```text
ADR-0001-atlas-first.md

ADR-0002-api-first.md

ADR-0003-single-editorial-pipeline.md
```

Numbers should never be reused.

---

# Writing ADRs

Good ADRs should be:

- concise;
- honest about trade-offs;
- understandable by future contributors;
- linked to the Architecture documentation.

The objective is not simply to record a decision.

The objective is to explain why that decision was reasonable at the time.

---

# ADR Template

Use `adr-template.md` when creating new Architecture Decision Records.

Each ADR should remain focused on a single significant decision.

---

# Initial ADRs

The first ADRs define the architectural foundations of DiaperScout.

- ADR-0001 — Atlas First
- ADR-0002 — API First
- ADR-0003 — Single Editorial Pipeline
- ADR-0004 — Observation-Based Knowledge
- ADR-0005 — Deterministic Search
- ADR-0006 — Media Ownership Model
- ADR-0007 — Event-Driven Workflows
- ADR-0008 — Earned Responsibility
- ADR-0009 — API Versioning
- ADR-0010 — Technology Independence

These ADRs establish the implementation philosophy that future technical decisions should build upon.