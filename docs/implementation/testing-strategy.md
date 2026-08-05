# Testing Strategy

**Document Status:** Draft  
**Version:** 1.0  
**Owner:** DiaperScout Project  
**Last Updated:** 2026-08-02

---

# 1. Purpose

This document defines the testing strategy for the DiaperScout platform.

Testing exists to provide confidence that the platform behaves correctly as it evolves.

The objective is not to achieve a particular code coverage percentage, but to ensure that the platform can continue to grow without introducing regressions.

Testing should be viewed as an integral part of development rather than a separate activity performed after implementation.

---

# 2. Principles

The testing strategy follows several guiding principles.

- Test behaviour rather than implementation.
- Prefer simple tests over clever tests.
- Automate wherever practical.
- Keep tests fast.
- Keep tests deterministic.
- Every bug fixed should be considered for a regression test.
- A failing test should indicate a genuine problem.

---

# 3. Testing Pyramid

DiaperScout adopts a testing pyramid.

```text
          UI Tests
      Integration Tests
          Unit Tests
```

The majority of tests should exist at the lower levels.

This provides fast feedback while maintaining confidence in the complete platform.

---

# 4. Unit Tests

Unit tests verify individual pieces of behaviour in isolation.

Typical candidates include:

- Domain entities
- Value objects
- Business rules
- Domain services
- Utility classes

Unit tests should:

- Execute quickly
- Avoid external dependencies
- Not require PostgreSQL
- Not require Cloudflare
- Not require network access

---

# 5. Integration Tests

Integration tests verify collaboration between components.

Examples include:

- Entity Framework Core with PostgreSQL
- API endpoints
- Authentication
- Search
- Image storage
- Dependency Injection configuration

Integration tests provide confidence that the application behaves correctly as a complete system.

---

# 6. API Tests

The HTTP API is considered a critical contract.

API tests should verify:

- Status codes
- Validation
- Authentication
- Authorisation
- Error handling
- Response structure

The API should be treated as a stable public interface.

---

# 7. End-to-End Tests

A smaller number of end-to-end tests should validate complete user journeys.

Examples include:

- Sign in
- Scan barcode
- Add product
- Submit review
- Upload observation
- Moderate content

These tests provide confidence that the platform functions correctly from the user's perspective.

---

# 8. Domain Testing

The Domain project represents the business rules of DiaperScout.

Business rules should be extensively tested.

Examples include:

- Rating validation
- Observation rules
- Scout reputation
- Product state transitions

The Domain should be one of the most thoroughly tested areas of the platform.

---

# 9. Database Testing

Database behaviour should be validated through integration testing.

Testing should include:

- Entity Framework Core mappings
- Migrations
- Constraints
- Query behaviour
- Search functionality

Database testing should use PostgreSQL rather than alternative providers wherever practical.

---

# 10. Authentication Testing

Authentication testing should verify:

- Magic links
- Passkeys
- Authorisation
- Session management

Security-sensitive functionality should receive comprehensive automated testing.

---

# 11. Performance Testing

Performance should be evaluated throughout development.

Areas of interest include:

- Product search
- Review retrieval
- Image loading
- Atlas generation
- Observation processing

Performance regressions should be investigated promptly.

---

# 12. Manual Testing

Automated testing does not replace manual testing.

Manual testing remains valuable for:

- User experience
- Accessibility
- Visual layout
- Mobile behaviour
- Progressive Web App installation

Developer judgement remains an important part of quality assurance.

---

# 13. Continuous Integration

All automated tests should execute within the continuous integration pipeline.

Code should not be merged if critical automated tests fail.

Testing should become part of normal development rather than a separate release activity.

---

# 14. Philosophy

Testing should provide confidence—not bureaucracy.

The goal is not to maximise the number of tests.

The goal is to maximise confidence that DiaperScout behaves correctly as the platform grows.

Well-designed architecture naturally leads to testable software.

The implementation should therefore prioritise clear responsibilities and simple interactions, allowing testing to remain straightforward throughout the lifetime of the project.