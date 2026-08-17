# Performance

**Document Status:** Draft  
**Version:** 1.1  
**Owner:** DiaperScout Project  
**Last Updated:** 2026-08-13

---

# 1. Purpose

This document defines the performance strategy for the DiaperScout platform.

Performance should be considered throughout the lifetime of the project rather than treated as a final optimisation phase.

The objective is to build a platform that:

- Feels responsive.
- Uses resources efficiently.
- Scales naturally.
- Remains observable.
- Remains maintainable.

Performance optimisation should support the experience of the platform without introducing unnecessary architectural complexity.

---

# 2. Philosophy

Performance should be driven by measurement rather than assumption.

The platform should avoid premature optimisation while remaining aware of the performance implications of architectural and implementation decisions.

Optimisation should normally occur when evidence demonstrates that improvement is necessary.

The preferred sequence is:

```text
Measure
   ↓
Identify bottleneck
   ↓
Understand cause
   ↓
Optimise
   ↓
Measure again
```

---

# 3. Guiding Principles

Performance decisions should follow these principles:

- Measure before optimising.
- Prefer simple solutions.
- Optimise demonstrated bottlenecks.
- Avoid unnecessary complexity.
- Maintain readability and maintainability.
- Optimise user experience rather than benchmark scores.
- Protect correctness while improving performance.
- Treat performance as part of normal engineering.

A technically fast system that is difficult to maintain is not considered a successful performance outcome.

---

# 4. User Experience

The most important performance measure is the experience of the User.

Typical interactions should feel responsive.

Important journeys include:

- Opening the Atlas
- Product search
- Barcode lookup
- Product browsing
- Location browsing
- Observation creation
- Evidence upload
- Backpack operations
- Editorial workflows

Long-running operations should not unnecessarily block the User.

Where an operation cannot complete immediately, it should be moved to appropriate background processing and provide clear progress or completion state.

---

# 5. Application Performance

The application should minimise unnecessary work.

Performance should be supported through:

- Efficient application logic
- Appropriate data retrieval
- Pagination
- Asynchronous processing
- Avoiding duplicate queries
- Appropriate caching
- Efficient serialisation
- Efficient media delivery

Application performance should be measured through the observability infrastructure.

---

# 6. Database Performance

PostgreSQL is the primary persistence platform.

Performance should primarily be achieved through:

- Appropriate indexes
- Efficient queries
- Thoughtful schema design
- Appropriate constraints
- PostgreSQL capabilities
- Entity Framework Core best practices

Database performance should be measured rather than optimised speculatively.

---

# 7. Entity Framework Core

Entity Framework Core queries should be designed deliberately.

Particular attention should be given to:

- N+1 queries
- Excessive eager loading
- Large result sets
- Unnecessary tracking
- Inefficient projections
- Pagination
- Repeated queries

Where read-only data is retrieved, tracking should not be enabled unnecessarily.

Application code should retrieve only the data required for the operation.

---

# 8. Query Performance

Important queries should be monitored as the platform grows.

Areas of particular interest include:

- Product search
- Product retrieval
- Atlas queries
- Location queries
- Observation retrieval
- Editorial queues
- Backpack retrieval
- Discovery Tasks

Slow queries should be investigated using PostgreSQL query analysis rather than addressed through arbitrary caching.

---

# 9. Indexing

Indexes should support demonstrated query patterns.

Indexes should be considered for:

- Frequently queried identifiers
- Foreign keys where appropriate
- Search fields
- Filtering fields
- Sorting fields
- Composite query patterns

Indexes should not be added indiscriminately.

Every index has costs in:

- Storage
- Write performance
- Maintenance
- Migration complexity

Index decisions should therefore be based on actual query requirements.

---

# 10. Search Performance

Search is one of the platform's most important interactive capabilities.

The initial implementation should use PostgreSQL capabilities, including:

- Full-text search
- Trigram similarity
- Appropriate indexes

The search experience should:

- Return results quickly.
- Support relevant partial matches.
- Tolerate appropriate spelling variation.
- Provide deterministic ordering where required.
- Scale with the Atlas.

Dedicated search infrastructure should only be introduced if PostgreSQL becomes a demonstrated bottleneck.

---

# 11. Search as Derived Data

Search indexes are derived from authoritative Atlas data.

The search system must not become a dependency for the integrity of the Atlas.

The preferred model is:

```text
PostgreSQL
    ↓
Canonical Atlas
    ↓
Search Index
```

If search becomes unavailable, canonical Atlas information must remain intact.

Search indexing should therefore be asynchronous where appropriate and recoverable.

---

# 12. API Performance

API endpoints should:

- Return only necessary data.
- Support pagination where appropriate.
- Avoid unnecessary database queries.
- Minimise payload size.
- Use efficient serialisation.
- Avoid exposing complete database entities unnecessarily.

API design should be based on actual client requirements rather than persistence structures.

---

# 13. Pagination

Large result sets should not normally be returned in a single response.

Pagination should be used for:

- Product lists
- Search results
- Observations
- Editorial queues
- Discovery Tasks
- Backpack collections
- Administrative lists

Pagination strategy should be selected according to the query and expected data volume.

Offset pagination may be sufficient for some workloads.

Cursor/keyset pagination may be preferable for large or frequently changing datasets.

---

# 14. Caching

Caching should be introduced only where it provides measurable benefit.

Potential candidates include:

- Frequently viewed Products
- Stable reference data
- Search suggestions
- Static application configuration
- Public media

Caching should remain an optimisation rather than a correctness dependency.

Canonical Atlas state must remain authoritative.

Cache invalidation should be tied to appropriate publication or data-change events where necessary.

---

# 15. Atlas Freshness

Performance optimisation must not compromise the meaning of Atlas information.

A cached response must not cause the application to present obsolete information as though it were current when freshness matters.

Where information has a defined freshness or availability window, the caching strategy must respect that model.

Caching must never bypass editorial publication rules.

---

# 16. Image Performance

Images are expected to represent a significant proportion of bandwidth usage.

Images should therefore be optimised for delivery.

Practices include:

- Responsive image sizes
- Thumbnail generation
- Appropriate compression
- Modern image formats where supported
- Lazy loading
- CDN delivery through Cloudflare
- Appropriate cache headers

Original Evidence should remain preserved even when derived delivery formats are generated.

---

# 17. Media Processing

Media processing that is not required to complete the immediate User interaction should occur asynchronously where appropriate.

Examples include:

- Thumbnail generation
- Image resizing
- Image optimisation
- Metadata extraction
- Derived media generation

The User should not unnecessarily wait for expensive processing.

The original uploaded Evidence should remain available for recovery and reprocessing.

---

# 18. Frontend Performance

The Blazor Web App should prioritise:

- Fast initial loading
- Responsive navigation
- Efficient rendering
- Appropriate asset loading
- Minimal unnecessary JavaScript
- Efficient data retrieval
- Appropriate use of browser caching

Client-side complexity should remain proportionate to the value it provides.

Performance work should not turn the client into an unnecessarily complicated application.

---

# 19. Progressive Web App Performance

The PWA should make appropriate use of browser capabilities to improve perceived performance.

Potential techniques include:

- Asset caching
- Offline shell
- Local draft storage
- Background synchronisation where appropriate
- Efficient image loading

Offline functionality must not be allowed to compromise correctness.

The client must distinguish between:

```text
Saved locally
     ≠
Accepted by server
```

---

# 20. Background Processing

Operations that do not need to complete during the current User interaction should execute asynchronously where appropriate.

Examples include:

- Image processing
- Search indexing
- Notifications
- Discovery Task generation
- Community Trust evaluation
- Availability freshness calculations
- Outbox processing

Background processing should be observable and independently scalable where required.

---

# 21. Background Queue Performance

Background processing should be monitored for:

- Queue depth
- Processing duration
- Throughput
- Retry rate
- Failure rate
- Oldest pending item

A queue that continually grows indicates that the system is not keeping up with demand.

Scaling background workers should be considered before increasing application complexity elsewhere.

---

# 22. Outbox Performance

Where an Outbox pattern is implemented, event processing should be monitored for:

- Queue depth
- Processing latency
- Throughput
- Retry count
- Failed events
- Oldest pending event

Outbox processing should remain efficient without compromising reliable event delivery.

Event handlers should be designed to be idempotent where appropriate.

---

# 23. Database Connection Management

Database connections should be managed through the standard Entity Framework Core and PostgreSQL connection-pooling mechanisms.

The application should avoid:

- Unnecessarily long-lived connections
- Holding connections during external operations
- Excessive concurrent database work
- Unbounded connection creation

Connection-pool exhaustion should be observable through application and database telemetry.

---

# 24. Concurrency

Concurrency should be considered where multiple operations may update shared state.

Important areas include:

- Editorial review
- Observation workflows
- Discovery Tasks
- Administrative configuration
- Background processing

Concurrency controls should preserve correctness before performance.

An incorrect result produced quickly is still a failure.

---

# 25. Asynchronous Processing

I/O-bound operations should use asynchronous APIs where supported.

Examples include:

- Database access
- HTTP requests
- Object storage
- File operations
- Background work

Asynchronous processing should not be used merely for stylistic reasons.

The goal is to avoid unnecessarily blocking application resources.

---

# 26. External Services

External services should not unnecessarily block critical User interactions.

External operations should use appropriate:

- Timeouts
- Retries
- Cancellation
- Circuit-breaking where justified
- Background processing

External service failures should not cause unnecessary resource exhaustion within the application.

---

# 27. CDN and Edge Caching

Cloudflare should be used to improve delivery of appropriate public assets.

Potentially cacheable resources include:

- Static application assets
- Public images
- Public media
- Other immutable resources

Private or User-specific content must not be cached publicly.

Cache headers should be deliberately configured rather than relying on accidental behaviour.

---

# 28. Performance Budgets

Performance budgets should be introduced for important user-facing areas as the application matures.

Potential budgets may include:

- Initial page load
- API response time
- Search response time
- Image payload size
- JavaScript payload
- Largest contentful image
- Background processing latency

Budgets should be based on observed usage and realistic target devices rather than arbitrary numbers.

Where a budget is exceeded, the cause should be investigated.

---

# 29. Performance Testing

Performance testing should use realistic workloads.

Testing should cover, where appropriate:

- API throughput
- Database query performance
- Search performance
- Concurrent Users
- Image delivery
- Background processing
- Queue throughput

Performance testing should be repeated after significant architectural changes.

Synthetic benchmarks should not be treated as a substitute for production telemetry.

---

# 30. Load Testing

Load testing should be introduced when the platform approaches usage levels where capacity becomes a meaningful risk.

Tests should model realistic behaviour rather than simply generating large numbers of identical requests.

Important scenarios include:

- Concurrent search
- Product browsing
- Observation submission
- Evidence upload
- Editorial processing
- Background processing

Load testing should identify the actual bottleneck before scaling decisions are made.

---

# 31. Monitoring

Performance should be continuously observed using the platform's observability tooling.

Important metrics include:

- API response time
- Request rate
- Error rate
- Database query duration
- Database connection usage
- Search latency
- Page-load performance
- Image delivery
- Background queue depth
- Background processing duration
- Outbox latency

Performance regressions should be investigated promptly.

---

# 32. Performance Baselines

As the platform matures, representative performance baselines should be recorded.

Baselines may include:

- Typical API latency
- Search latency
- Database query duration
- Background processing throughput
- Image delivery performance

Baselines provide context for identifying meaningful regressions.

A performance change should be considered significant based on user impact and operational evidence rather than a small numerical difference alone.

---

# 33. Scalability

The platform should scale through good engineering practices rather than unnecessary architectural complexity.

Initial priorities include:

- Efficient APIs
- Well-designed database queries
- Appropriate indexing
- CDN usage
- Stateless application services
- Asynchronous background processing
- Appropriate connection management

More advanced scaling techniques should only be introduced when operational requirements justify them.

---

# 34. Horizontal Scaling

The application should remain capable of horizontal scaling where practical.

Application instances should avoid relying on local mutable state for correctness.

Shared state should reside in appropriate infrastructure such as:

- PostgreSQL
- Object storage
- Durable background queues
- Appropriate shared services

This allows additional application instances to be introduced without changing application behaviour.

---

# 35. Vertical Scaling

Vertical scaling may be appropriate before introducing more complex distributed architecture.

Examples include:

- Additional CPU
- Additional memory
- Faster storage
- Increased database capacity

Vertical scaling is often preferable when it solves the demonstrated bottleneck with less operational complexity.

---

# 36. Dedicated Infrastructure

Additional infrastructure should only be introduced when existing components become a demonstrated bottleneck.

Potential future additions include:

- Read replicas
- Distributed caching
- Dedicated search infrastructure
- Dedicated background workers
- Regional application instances

Each addition introduces operational complexity and should therefore have a clear justification.

---

# 37. Performance and Correctness

Performance optimisations must not compromise:

- Data integrity
- Editorial integrity
- Provenance
- Security
- Privacy
- Authentication
- Authorisation

Examples:

- Caching must not bypass publication rules.
- Search optimisation must not modify canonical data.
- Asynchronous processing must not claim success before server confirmation.
- Database optimisation must not weaken constraints.
- Client-side optimisation must not become a security boundary.

Correctness always takes priority.

---

# 38. Performance and Observability

Performance work should use the observability strategy defined elsewhere in the project.

Important telemetry includes:

```text
Request
  ↓
Trace
  ↓
Application
  ↓
Database / External Service
  ↓
Metrics + Logs
```

This should make it possible to distinguish:

- Application latency
- Database latency
- External service latency
- Queue latency
- Client-side performance

Performance optimisation should therefore be based on evidence from real measurements.

---

# 39. Performance and Deployment

Deployments should be monitored for performance regressions.

Following a deployment, operators should be able to compare:

- Request latency
- Error rates
- Database performance
- Search performance
- Background processing
- Resource utilisation

against previous behaviour.

A deployment that is functionally correct but causes a significant performance regression should be treated as a production issue.

---

# 40. Resource Efficiency

Performance includes efficient use of infrastructure resources.

The platform should avoid:

- Unnecessary database queries
- Excessive object allocations
- Repeated expensive calculations
- Unbounded background queues
- Oversized API responses
- Unoptimised media
- Unnecessary external calls

Resource efficiency improves both performance and operating cost.

---

# 41. Future Evolution

Future performance improvements may include:

- Read replicas
- Distributed caching
- Dedicated search infrastructure
- Dedicated background workers
- Automated scaling
- Regional deployments
- Advanced CDN strategies
- More sophisticated client-side caching

These capabilities should address demonstrated needs rather than anticipated ones.

---

# 42. Design Philosophy

Performance should support the User experience without compromising maintainability.

DiaperScout favours straightforward, measurable optimisation over speculative complexity.

A well-designed platform should remain fast because it is simple, observable and appropriately engineered — not because it has accumulated layers of optimisation.

The guiding principle is:

> **Make it correct, make it observable, measure it, then make it faster where the evidence says it matters.**