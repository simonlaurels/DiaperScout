# Architecture

## Purpose

The Architecture documentation describes how DiaperScout is implemented.

Where the North Star defines the project's vision, the Product Specification defines what the Atlas is, and the Community documentation defines how the Atlas grows, the Architecture documentation defines how software brings those ideas to life.

The architecture exists to bring the idea of the Atlas to life.

These documents intentionally focus on architectural principles and responsibilities rather than implementation technologies. Their purpose is to explain *why* the system is designed the way it is before describing *how* it is built.

---

# Relationship to the Repository

The Architecture documentation should not be read in isolation.

It builds upon the concepts established elsewhere within the repository.

The recommended reading order is:

```text
North Star

↓

Product Specification

↓

Community

↓

Architecture README

↓

Architecture Walkthrough

↓

Architecture Principles

↓

Remaining Architecture Documents
```

Each stage introduces concepts that are assumed by the next.

The Architecture documentation therefore represents the implementation of the ideas established by the earlier documents rather than redefining them.

---

# Guiding Philosophy

DiaperScout is not simply a software application.

It is an Explorer's Atlas.

The Atlas is the product.

The application is one way to experience that Atlas.

The architecture therefore exists to:

- preserve trustworthy knowledge;
- model the real world faithfully;
- support community exploration;
- protect the integrity of the Atlas;
- enable long-term evolution;
- remain understandable to future contributors.

---

# Scope

The Architecture documentation describes:

- architectural philosophy;
- domain modelling;
- knowledge flow;
- backend responsibilities;
- public APIs;
- workflows;
- editorial processes;
- operational principles.

It intentionally avoids prescribing specific technologies wherever practical.

Technology selections are documented separately through Architecture Decision Records (ADRs) as the project evolves.

---

# Documentation Structure

The Architecture documentation is organised into a number of focused documents.

| Document | Purpose |
|----------|---------|
| Architecture Walkthrough | A narrative introduction to how the architecture works |
| Architecture Principles | Core architectural philosophy |
| Knowledge Architecture | How knowledge becomes part of the Atlas |
| Domain Model | Core entities and relationships |
| Backend Services | Logical backend responsibilities |
| API Architecture | Public interface to the Atlas |
| Database Model | Persistent representation of the domain |
| Search | Discovering knowledge within the Atlas |
| Workflow Architecture | How work moves through the system |
| Editorial Architecture | How evidence becomes trusted knowledge |
| Authentication & Roles | Identity, trust and responsibility |
| Media & Evidence | Managing canonical media and supporting evidence |
| Scout Task System | Community stewardship of the Atlas |
| Deployment & Operations | Operational philosophy and resilience |

---

# Architecture Decision Records

Technology choices intentionally sit outside the core architecture documents.

As implementation decisions are made, they should be captured as Architecture Decision Records (ADRs).

ADRs record:

- the decision;
- the context;
- the reasoning;
- the consequences.

This preserves the rationale behind important technical choices as the project evolves.

---

# Guiding Principle

When reading any architecture document, remember the central principle of DiaperScout:

> **The Atlas is the product.**

Everything within the architecture exists to preserve, improve and present that Atlas.

If an architectural decision conflicts with that objective, the architecture should evolve before the Atlas does.