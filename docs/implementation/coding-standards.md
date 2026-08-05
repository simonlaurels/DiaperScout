# Coding Standards

**Document Status:** Draft  
**Version:** 1.0  
**Owner:** DiaperScout Project  
**Last Updated:** 2026-08-02

---

# 1. Purpose

This document defines the coding standards for the DiaperScout platform.

The objective is to produce code that is:

- Readable
- Consistent
- Maintainable
- Testable
- Self-documenting

These standards exist to improve the long-term quality of the codebase rather than enforce personal coding preferences.

---

# 2. Philosophy

Code is read far more often than it is written.

Every piece of code should therefore be written for the next developer who needs to understand it.

That developer may be:

- Another contributor
- Your future self
- A community volunteer

Clarity should always take priority over cleverness.

---

# 3. Guiding Principles

Every implementation should aim to be:

- Simple
- Explicit
- Consistent
- Predictable

Developers should favour straightforward solutions over unnecessarily complex abstractions.

---

# 4. Readability

Readable code is preferred over concise code.

Good names are more valuable than short names.

Avoid unnecessary abbreviations.

Examples:

Good:

```csharp
SubmitObservationAsync()
```

Poor:

```csharp
SubObs()
```

Code should read naturally.

---

# 5. Naming

Classes should represent nouns.

Examples:

- Product
- Observation
- ReviewService

Methods should represent actions.

Examples:

- SearchProductsAsync()
- SubmitReviewAsync()
- ApproveObservationAsync()

Variables should describe their purpose rather than their type.

---

# 6. Class Design

Each class should have a single, well-defined responsibility.

Large classes should be refactored into smaller components before they become difficult to understand.

If a class requires extensive comments to explain its purpose, its design should be reconsidered.

---

# 7. Method Design

Methods should:

- Be focused
- Have a clear purpose
- Avoid excessive nesting
- Return early where appropriate

Very large methods should be treated as a design smell.

---

# 8. Comments

Comments should explain **why**, not **what**.

Avoid comments that simply repeat the code.

Poor:

```csharp
// Increment counter
counter++;
```

Better:

```csharp
// Skip archived observations when calculating Scout reputation.
```

Good code should largely explain itself.

---

# 9. Modern C#

Modern language features should be adopted where they improve clarity.

Examples include:

- Pattern matching
- Primary constructors
- Collection expressions
- Nullable reference types
- Records (where appropriate)

Features should improve readability rather than demonstrate language knowledge.

---

# 10. Dependency Injection

Services should be obtained through Dependency Injection.

Avoid constructing complex dependencies directly.

Prefer constructor injection.

Dependencies should represent business capabilities rather than implementation details.

---

# 11. Entity Framework Core

Entity Framework Core should be used naturally.

Avoid unnecessary abstraction layers.

LINQ queries should remain readable.

Developers should remain aware of the SQL generated beneath Entity Framework Core.

---

# 12. Error Handling

Errors should be handled intentionally.

Exceptions should not be swallowed silently.

Unexpected failures should:

- Be logged
- Preserve useful diagnostic information
- Return safe responses

---

# 13. Asynchronous Programming

Use asynchronous APIs where appropriate.

Avoid blocking asynchronous operations.

Methods returning asynchronous work should follow the standard `Async` naming convention.

---

# 14. Testing

Code should be written with testing in mind.

Tightly coupled implementations should be avoided.

Simple, well-defined responsibilities naturally lead to easier testing.

---

# 15. Formatting

Formatting should be applied automatically using standard .NET tooling.

Developers should avoid debating formatting preferences.

Consistency is more valuable than personal style.

---

# 16. Documentation

Public APIs should be documented where appropriate.

Architectural decisions belong in project documentation rather than code comments.

Documentation should explain intent rather than duplicate implementation details.

---

# 17. Code Reviews

Code reviews should focus on:

- Correctness
- Readability
- Maintainability
- Security
- Performance
- Simplicity

Reviews should remain constructive and collaborative.

The objective is to improve the platform rather than criticise individuals.

---

# 18. Refactoring

Developers are encouraged to improve existing code when appropriate.

Small, incremental improvements are preferred over large-scale rewrites.

Technical debt should be reduced continuously rather than deferred indefinitely.

---

# 19. Design Philosophy

The DiaperScout codebase should feel calm.

Developers should rarely encounter surprising behaviour, unnecessary complexity or obscure implementation techniques.

The preferred implementation is usually the one that is easiest to explain to another developer.

Every line of code should contribute towards a platform that remains understandable, maintainable and enjoyable to develop for many years.