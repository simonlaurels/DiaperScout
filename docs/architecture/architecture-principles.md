# Architecture Principles

## Purpose

This document defines the architectural principles that guide every technical decision within DiaperScout.

It does not prescribe programming languages, frameworks, databases or hosting platforms.

Instead, it defines the philosophy that every implementation should uphold.

Whenever multiple technically valid solutions exist, preference should normally be given to the solution that best satisfies these principles.

---

# The Atlas Comes First

The architecture exists to bring the idea of the Atlas to life.

The Atlas is the product.

The application is one way to experience the Atlas.

Every architectural decision should therefore strengthen, preserve or improve the Atlas.

---

# Architecture Serves the Product

The Product Specification defines what DiaperScout is.

The architecture exists to implement that specification faithfully.

If implementation becomes difficult, the preferred solution is to improve the architecture rather than compromise the product vision.

---

# Model the Real World

The architecture should model genuine real-world concepts.

Entities exist because they represent meaningful parts of the domain, not because they simplify implementation.

For example:

- Manufacturers produce Products.
- Retailers stock Products.
- Scouts make Observations.
- Editors curate the Atlas.

The software should reflect reality wherever practical.

---

# Knowledge is Built from Evidence

The Atlas is not edited directly.

Knowledge enters the system as evidence.

Evidence may originate from:

- Scout discoveries;
- Community observations;
- Manufacturer submissions;
- Correction requests;
- Moderator investigations.

The Atlas represents the editorial interpretation of that evidence.

---

# One Editorial Pipeline

All evidence follows the same editorial pathway.

Different sources possess different authority.

They do not possess different architectural pathways.

Whether evidence originates from a Scout, a Manufacturer or a Moderator, it should become part of the Atlas through the same editorial process.

---

# Canonical Knowledge and Evidence are Different

Canonical knowledge represents the published Atlas.

Evidence supports that knowledge.

These concepts should remain separate throughout the architecture.

For example:

- Product Specifications belong to the Atlas.
- Retail observations belong to evidence.
- Product images belong to the Atlas.
- Shelf photographs belong to observations.

Maintaining this distinction strengthens integrity, traceability and long-term maintainability.

---

# Deterministic Before Intelligent

Where structured knowledge exists, deterministic behaviour should be preferred.

Search, filtering, workflow and editorial processes should remain understandable, reproducible and explainable.

Artificial intelligence may assist contributors in the future, but it should not replace structured knowledge or deterministic decision making.

---

# Explainable Knowledge

Every significant fact published by the Atlas should be explainable.

The architecture should always be capable of answering:

- Why is this believed?
- Where did the information originate?
- Who reviewed it?
- When did it become canonical?

Trustworthy knowledge should be capable of explaining itself.

---

# Preserve Knowledge

The Atlas represents accumulated knowledge.

Knowledge should not be discarded unnecessarily.

Observations, editorial history and supporting evidence all contribute to understanding how the Atlas has evolved.

Backups exist to preserve the Atlas and its supporting knowledge rather than simply protecting software.

---

# Progressive Responsibility

Responsibility should be earned through stewardship.

Contributors gain trust through consistently valuable observations.

The system may recommend contributors for greater responsibility, but people remain responsible for important decisions.

Trust should reflect contribution rather than popularity.

---

# Continuous Evolution

The Atlas is continuously evolving.

The architecture should support gradual change without depending upon maintenance windows.

Background processing should react to events.

Schema evolution should favour safe, incremental change.

---

# API First

The API represents the public interface of the Atlas.

Every client, including the official Progressive Web App, should consume the same API.

Business rules belong within backend services rather than individual applications.

---

# Self-Documenting Architecture

The architecture should favour clarity.

Identifiers, APIs, entities and workflows should describe the real world.

Future contributors should understand why the system exists before learning how it is implemented.

---

# Simplicity Before Abstraction

The architecture should solve today's problems well.

Speculative abstraction should be avoided.

The domain model should evolve naturally as the Atlas grows rather than attempting to predict every possible future requirement.

---

# Architectural Integrity

Every component should reinforce the same conceptual model.

No subsystem should bypass the architectural principles established within this document.

Examples include:

- no direct editing of canonical knowledge;
- no privileged pathway around editorial review;
- no duplication of business rules within clients;
- no loss of provenance through implementation shortcuts.

Consistency is more valuable than isolated optimisation.

---

# The Long View

Architectural decisions should consider the long-term health of the project.

When multiple technically correct solutions exist, preference should normally be given to the one that:

- preserves knowledge;
- improves maintainability;
- strengthens trust;
- simplifies future evolution;
- protects the integrity of the Atlas.

The architecture should remain understandable to contributors many years after it was first implemented.

---

# Architectural Decision Test

Before introducing a significant architectural change, contributors should ask:

- Does this strengthen the Atlas?
- Does this preserve trustworthy knowledge?
- Does this improve long-term maintainability?
- Does this make the system easier to understand?
- Does this avoid unnecessary complexity?
- Will this still make sense in ten years?

If the answer to these questions is "yes", the decision is likely to align with the architectural philosophy of DiaperScout.