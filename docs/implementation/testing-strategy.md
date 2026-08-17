# Testing Strategy

**Document Status:** Draft  
**Version:** 1.1  
**Owner:** DiaperScout Project  
**Last Updated:** 2026-08-13

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
- Test important architectural boundaries.
- Use real infrastructure where provider-specific behaviour matters.

Testing should provide confidence without becoming bureaucracy.

---

# 3. Testing Pyramid

DiaperScout adopts a testing pyramid.

```text
             UI / E2E Tests
          Integration Tests
              Unit Tests
```

The majority of tests should exist at the lower levels.

This provides fast feedback while maintaining confidence in the complete platform.

The pyramid is a guideline rather than a rigid numerical target.

Some security-sensitive or workflow-critical behaviour may appropriately have more integration or end-to-end coverage than ordinary code.

---

# 4. Test Projects

The production solution contains three primary test projects:

```text
tests/
├── DiaperScout.UnitTests/
├── DiaperScout.IntegrationTests/
└── DiaperScout.ArchitectureTests/
```

End-to-end tests may be introduced within the appropriate integration/UI test infrastructure when the production application requires them.

The test structure should reflect the architectural boundaries of the production solution.

---

# 5. Unit Tests

Unit tests verify individual pieces of behaviour in isolation.

Typical candidates include:

- Domain entities
- Value objects
- Business rules
- Domain services
- Application logic
- Validators
- Pure transformation logic

Unit tests should:

- Execute quickly
- Avoid external dependencies
- Not require PostgreSQL
- Not require Cloudflare R2
- Not require network access
- Be deterministic

Unit tests should focus on observable behaviour rather than internal implementation details.

---

# 6. Domain Testing

The Domain project represents the business rules of DiaperScout.

Business rules should be thoroughly tested.

Examples include:

- Product rules
- Product Variant rules
- Size Variant rules
- Pack Type rules
- Observation rules
- Evidence rules
- Editorial state transitions
- Discovery Task state transitions
- Community Trust rules
- Domain invariants
- Historical/provenance rules

The Domain should be one of the most thoroughly tested areas of the platform.

Tests should verify that invalid domain states cannot be created through normal domain behaviour.

---

# 7. Application Testing

Application tests verify use cases and orchestration.

Examples include:

- Product queries
- Product search
- Observation submission
- Evidence submission
- Editorial decisions
- Discovery Task participation
- Backpack operations
- Manufacturer submissions

Application tests should verify that the correct domain behaviour is invoked and that relevant persistence/events are coordinated appropriately.

Application tests should not duplicate every Domain test.

---

# 8. Integration Tests

Integration tests verify collaboration between real application components.

Examples include:

- Entity Framework Core with PostgreSQL
- API endpoints
- Authentication
- Authorisation
- Dependency Injection configuration
- Object storage integration
- Background processing
- Search infrastructure
- Application-to-Infrastructure workflows

Integration tests provide confidence that the separately tested components work correctly together.

---

# 9. PostgreSQL Testing

Database behaviour should be validated using PostgreSQL rather than an alternative provider wherever practical.

Testing should include:

- Entity Framework Core mappings
- Migrations
- Constraints
- Foreign keys
- Unique indexes
- Query behaviour
- Pagination
- Concurrency
- PostgreSQL-specific behaviour

A fake or in-memory database must not be treated as evidence that PostgreSQL behaviour is correct.

Disposable PostgreSQL containers are preferred for automated integration testing where practical.

---

# 10. Migration Testing

Database migrations should be tested as part of the integration test process.

Testing should verify:

- A clean database can be created.
- Migrations apply successfully.
- Existing databases can be upgraded.
- Important data is preserved.
- Constraints are created correctly.
- Destructive changes are deliberate.

Production migrations should receive additional review for:

- locking;
- table rewrites;
- index creation;
- data migration;
- backward compatibility.

---

# 11. API Tests

The HTTP API is a critical application contract.

API tests should verify:

- Status codes
- Validation
- Authentication
- Authorisation
- Error handling
- Response structure
- Pagination
- Filtering
- Resource ownership
- API versioning

The API should be treated as a stable public interface.

API tests should verify that clients cannot bypass server-side authorisation.

---

# 12. Authentication Testing

Authentication testing should verify:

## Magic Links

- Valid links
- Expired links
- Already-used links
- Invalid links
- Replay attempts
- Rate limiting

## Passkeys

- Registration
- Successful authentication
- Invalid assertions
- Removed credentials
- Multiple credentials
- Recovery

## Sessions

- Session creation
- Session expiry
- Sign out
- Revocation
- Multiple devices

Security-sensitive authentication functionality should receive comprehensive automated testing.

---

# 13. Authorisation Testing

Authorisation testing should verify both allowed and denied operations.

Tests should cover:

- Anonymous User
- Authenticated User
- Contributor
- Moderator
- Administrator
- Verified Manufacturer

Tests should verify that:

- Contributors cannot perform editorial actions.
- Community Trust does not grant permissions.
- Administrators do not automatically become Moderators.
- Verified Manufacturers cannot bypass editorial review.
- Users cannot modify resources they do not own.
- Users cannot approve their own submissions.
- Client-side UI restrictions cannot bypass server-side authorisation.

---

# 14. Observation Testing

Observations are a central part of the DiaperScout platform.

Tests should cover:

- Creating an Observation
- Validation
- Draft state
- Submission
- Evidence association
- Workflow transitions
- Contributor attribution
- Duplicate submission protection
- Concurrency
- Editorial outcomes

Observation tests should preserve the distinction between:

> What the User observed

and:

> What DiaperScout ultimately publishes.

---

# 15. Evidence Testing

Evidence should be tested independently and as part of Observation workflows.

Tests should cover:

- Evidence creation
- Evidence association
- Media metadata
- Upload processing
- Invalid media
- Media processing failure
- Provenance
- Original Evidence preservation

Derived media or processing results must not silently replace the original Evidence.

---

# 16. Editorial Workflow Testing

Editorial review is the authoritative gateway into the Atlas.

The core workflow should have integration coverage:

```text
Observation
    ↓
Evidence
    ↓
Editorial Review
    ↓
Editorial Decision
    ↓
Atlas Update
```

Tests should cover:

- Accepted submissions
- Rejected submissions
- Deferred submissions
- Additional Evidence requests
- Re-submission
- Self-approval prevention
- Moderator authorisation
- Concurrent editorial decisions
- Provenance
- Atlas update behaviour

The tests should explicitly verify that community submissions cannot directly mutate canonical Atlas data.

---

# 17. Product Discovery Testing

Product discovery should verify that an Explorer can report an unknown Product without directly creating canonical Atlas data.

The workflow should be tested as:

```text
Explorer
   ↓
Observation
   ↓
Evidence
   ↓
Editorial Review
   ↓
Product Created or Matched
   ↓
Atlas
```

Tests should cover:

- Matching an existing Product
- Identifying a genuinely new Product
- Product Variant identification
- Regional variation
- Insufficient Evidence
- Duplicate discovery
- Editorial rejection

---

# 18. Retail Observation Testing

Retail Observations should verify that:

- A Product can be observed at a Location.
- Retailer context can be recorded where appropriate.
- Observation time is preserved.
- Evidence can support the observation.
- Historical observations remain available.
- Availability is derived from evidence.
- An observation does not create guaranteed live stock.

Tests should explicitly prevent the system from treating a community observation as permanent inventory.

---

# 19. Discovery Task Testing

Discovery Tasks should verify:

- Knowledge Gap creation
- Task generation
- Task availability
- Task participation
- Task state transitions
- Completion
- Partial resolution
- Unresolved tasks
- Invalid/closed tasks
- Concurrent participation
- Contribution resulting from a task

Tests should verify that Discovery Tasks do not create:

- roles;
- permissions;
- public rankings;
- automatic privileged access.

---

# 20. Community Trust Testing

Community Trust is an internal signal rather than an authorisation mechanism.

Tests should verify:

- Accepted contributions can influence Community Trust where appropriate.
- Contribution quality can affect evaluation.
- Rejected or problematic contributions are handled correctly.
- Community Trust does not directly grant permissions.
- Community Trust is not exposed as a public reputation score where the API does not permit it.
- Community Trust calculations do not depend solely on contribution volume.

Community Trust behaviour should remain separate from authentication and authorisation tests.

---

# 21. Backpack Testing

Backpack functionality should be tested for:

- Saving Products
- Saving Locations
- Collections
- Scrapbook content
- Retrieval
- Updates
- Deletion where appropriate
- Ownership
- Privacy

Tests must verify that one User cannot access another User's private Backpack data.

---

# 22. Search Testing

Search testing should verify:

- Product search
- Filtering
- Sorting
- Pagination
- Barcode lookup
- Location search
- Brand search
- Manufacturer search
- Search indexing
- Index updates after publication

Search should normally operate against published or appropriately indexed knowledge.

Unreviewed community information must not accidentally appear as canonical Atlas knowledge.

---

# 23. Background Processing Testing

Background processing should be tested independently from the initiating HTTP request.

Examples include:

- Media processing
- Thumbnail generation
- Search indexing
- Notification delivery
- Discovery Task generation
- Community Trust evaluation
- Availability freshness calculations

Tests should verify:

- Successful processing
- Retry behaviour
- Failure handling
- Idempotency
- Recovery
- Duplicate event handling

A background failure should not corrupt the authoritative domain state.

---

# 24. Event Testing

Meaningful events should be tested where they drive important workflows.

Examples include:

- Observation Submitted
- Evidence Added
- Editorial Decision Recorded
- Atlas Updated
- Product Published
- Discovery Task Generated
- Discovery Task Completed
- Contribution Accepted
- Community Trust Updated

Tests should verify that events:

- contain the required information;
- are published at the correct point in the workflow;
- can be processed safely;
- do not cause duplicate effects when retried.

---

# 25. Outbox Testing

Where an Outbox is implemented, tests should verify:

- Domain state and event are committed atomically.
- Failed event processing does not lose the event.
- Events are retried appropriately.
- Successfully processed events are not processed repeatedly.
- Poison/failing events can be identified and handled.

The Outbox should only be tested where it is actually implemented.

---

# 26. Concurrency Testing

Concurrency should be tested wherever multiple actors may update the same state.

Important areas include:

- Editorial review
- Discovery Tasks
- Observation workflows
- Administrative configuration
- User-owned drafts

Tests should verify that stale updates do not silently overwrite newer state.

---

# 27. End-to-End Tests

A smaller number of end-to-end tests should validate complete user journeys.

Examples include:

- Browse the Atlas
- Sign in
- Save a Product
- Discover an unknown Product
- Submit an Observation
- Upload Evidence
- Complete a Discovery Task
- Editorial review
- Publication of accepted knowledge

End-to-end tests should focus on the most important journeys rather than attempting to test every possible UI interaction.

---

# 28. PWA and Offline Testing

Where offline functionality is implemented, tests should verify:

- Draft creation while offline
- Local persistence
- Reconnection
- Synchronisation
- Duplicate submission prevention
- Authentication before protected synchronisation
- Clear distinction between local and server-confirmed state

The application must never tell a User that an Observation has been submitted until the server has confirmed receipt.

---

# 29. UI Testing

UI testing should verify important user-facing behaviour.

Examples include:

- Navigation
- Forms
- Validation feedback
- Authentication flows
- Contribution flows
- Editorial interfaces
- Responsive behaviour
- Accessibility-critical interactions

Visual testing should be used selectively.

Not every component requires an automated screenshot test.

---

# 30. Accessibility Testing

Accessibility should be tested throughout development.

Testing should include:

- Keyboard navigation
- Focus behaviour
- Form labels
- Validation messages
- Semantic HTML
- Screen-reader compatibility
- Colour contrast
- Responsive behaviour
- Reduced-motion considerations where relevant

Automated accessibility tools should supplement, not replace, manual accessibility testing.

---

# 31. Performance Testing

Performance should be evaluated throughout development.

Areas of interest include:

- Product search
- Atlas queries
- Location queries
- Observation retrieval
- Editorial queues
- Image loading
- Media processing
- Search indexing
- Background processing

Performance regressions should be investigated promptly.

Performance testing should use realistic data volumes where database/query performance matters.

---

# 32. Security Testing

Security-sensitive functionality should receive dedicated testing.

Areas include:

- Authentication
- Authorisation
- Session management
- Account recovery
- Resource ownership
- Input validation
- File uploads
- API access
- Rate limiting
- Enumeration protection
- Privileged operations

Tests should verify that security boundaries hold when normal UI behaviour is bypassed.

---

# 33. Manual Testing

Automated testing does not replace manual testing.

Manual testing remains valuable for:

- User experience
- Accessibility
- Visual layout
- Mobile behaviour
- Progressive Web App installation
- Offline behaviour
- Authentication UX
- Error recovery
- Real-world contribution flows

Developer judgement remains an important part of quality assurance.

---

# 34. Regression Testing

Every production defect should be considered for a regression test.

The test should reproduce the failure and demonstrate that the fix prevents recurrence.

Regression tests should be placed at the lowest appropriate testing level.

For example:

- Domain defect → Unit Test
- Persistence defect → Integration Test
- API contract defect → API Test
- Complete workflow defect → Integration or End-to-End Test

---

# 35. Test Data

Test data should be:

- deterministic;
- isolated;
- representative;
- safe to commit;
- free of real personal information.

Production data must never be copied into automated tests without appropriate anonymisation and explicit approval.

---

# 36. Test Isolation

Tests should not depend on:

- developer machine state;
- test execution order;
- external production services;
- shared mutable databases;
- previous test runs.

Each test should establish the state it requires.

Integration tests should use isolated database state.

---

# 37. Continuous Integration

All automated tests should execute within the continuous integration pipeline.

The CI pipeline should include, as appropriate:

- Build
- Unit Tests
- Integration Tests
- Architecture Tests
- API Tests
- Static analysis
- Formatting checks

Code should not be merged if critical automated tests fail.

---

# 38. Architecture Testing

Architecture tests should protect the solution boundaries.

Examples include:

- Domain must not reference Infrastructure.
- Domain must not reference ASP.NET Core.
- Application must not reference Web.
- Application must not reference API.
- Web must not reference Infrastructure.
- API must not directly access persistence.
- Shared must remain dependency-light.

Architecture tests prevent gradual erosion of the production design.

---

# 39. Coverage

Code coverage is a useful diagnostic but is not the primary measure of testing quality.

A high coverage percentage does not guarantee useful tests.

The priority is:

```text
Correct behaviour
       ↓
Important behaviour covered
       ↓
Meaningful regression protection
       ↓
Coverage as supporting evidence
```

Coverage targets may be introduced later where they provide useful engineering guidance.

---

# 40. Test Naming

Test names should clearly describe the behaviour being verified.

Prefer:

```text
SubmitObservation_WithValidEvidence_CreatesSubmission
```

over:

```text
TestObservation1
```

A developer should be able to understand what failed without opening the implementation immediately.

---

# 41. Test Structure

Tests should generally follow:

```text
Arrange
Act
Assert
```

Keep each test focused.

Avoid tests that verify many unrelated behaviours at once.

Shared test helpers should be used where they genuinely improve readability, but test setup should remain understandable.

---

# 42. Mocking

Mocking should be used selectively.

Mock:

- external services;
- genuinely expensive or unavailable dependencies;
- boundaries where isolation provides meaningful value.

Do not mock every dependency merely because a mocking framework makes it possible.

Important infrastructure behaviour should be tested with real infrastructure.

---

# 43. External Services

External providers should be tested at two levels.

## Contract/Integration Testing

Verify that the application integrates correctly with the provider where practical.

## Application Testing

Use controlled test doubles for deterministic application tests.

Examples include:

- Object storage
- Email
- External APIs

Provider-specific behaviour should not be assumed to work merely because an interface was mocked.

---

# 44. Definition of Test Confidence

A feature should not be considered adequately tested simply because its happy path works.

Confidence should cover:

- normal behaviour;
- invalid input;
- failure paths;
- security;
- concurrency where relevant;
- persistence;
- integration;
- important user journeys.

The depth of testing should be proportional to the risk and importance of the feature.

---

# 45. Definition of Done

A production feature should normally have:

- relevant unit tests;
- relevant integration tests;
- API tests where applicable;
- security/authorisation tests where applicable;
- regression tests for known defects;
- architecture tests where the feature affects boundaries;
- manual verification for important UX behaviour.

Critical workflows should have complete-path coverage.

---

# 46. Critical Production Workflow

The most important DiaperScout workflow is:

```text
Explorer
   ↓
Observation
   ↓
Evidence
   ↓
Editorial Review
   ↓
Editorial Decision
   ↓
Atlas
```

This workflow should have strong automated integration coverage.

The tests should demonstrate that:

1. An authenticated Explorer can submit an Observation.
2. Evidence can be attached.
3. The Observation enters the correct workflow.
4. An authorised Moderator can review it.
5. The correct Editorial Decision is recorded.
6. Accepted information can update the Atlas.
7. Relevant supporting systems can react.
8. Community contribution history is preserved.
9. Community Trust remains separate from authorisation.

---

# 47. Testing Philosophy

Testing should provide confidence, not bureaucracy.

The goal is not to maximise the number of tests.

The goal is to maximise confidence that DiaperScout behaves correctly as the platform grows.

Well-designed architecture naturally leads to testable software.

The implementation should therefore prioritise clear responsibilities and simple interactions, allowing testing to remain straightforward throughout the lifetime of the project.

---

# 48. Relationship to Other Documents

This document defines how the production application is verified.

Related documents include:

- **Solution Structure** — defines project boundaries.
- **Project Layout** — defines test project organisation.
- **Domain Model** — defines business rules to be tested.
- **Database Model** — defines persistence behaviour.
- **Data Access Strategy** — defines persistence implementation and testing requirements.
- **API Architecture** — defines the API contract.
- **Authentication Strategy** — defines identity behaviour.
- **Authorization** — defines permission boundaries.
- **Workflow Architecture** — defines workflow state transitions.
- **Security** — defines broader security requirements.
- **Coding Standards** — defines implementation conventions.

---

# 49. Summary

DiaperScout testing is based on a simple principle:

> **Test the behaviour that matters, at the lowest level that provides meaningful confidence.**

The strategy combines:

```text
Unit Tests
     ↓
Integration Tests
     ↓
API / Security Tests
     ↓
End-to-End Tests
     ↓
Manual Verification
```

The most important workflows receive the strongest coverage.

The architecture itself is tested.

Real PostgreSQL is used where persistence behaviour matters.

Security boundaries are tested explicitly.

Every significant production defect should leave behind a regression test where practical.

The objective is a production platform that can evolve confidently without sacrificing the integrity, provenance and trustworthiness of the Atlas.