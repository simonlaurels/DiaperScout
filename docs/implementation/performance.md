# Performance

**Document Status:** Draft  
**Version:** 1.0  
**Owner:** DiaperScout Project  
**Last Updated:** 2026-08-02

---

# 1. Purpose

This document defines the performance strategy for the DiaperScout platform.

Performance should be considered throughout the lifetime of the project rather than treated as a final optimisation phase.

The objective is to build a platform that feels responsive, scales naturally and remains maintainable.

---

# 2. Philosophy

Performance should be driven by measurement rather than assumption.

The platform should avoid premature optimisation while remaining aware of the performance implications of architectural and implementation decisions.

Optimisation should only occur where evidence demonstrates that improvement is necessary.

---

# 3. Guiding Principles

Performance decisions should follow these principles:

- Measure before optimising.
- Prefer simple solutions.
- Optimise bottlenecks.
- Avoid unnecessary complexity.
- Maintain readability.
- Optimise user experience rather than benchmark scores.

---

# 4. Application Performance

The platform should remain responsive during normal operation.

Typical user interactions should feel immediate.

Examples include:

- Product search
- Barcode lookup
- Review submission
- Observation creation
- Image browsing

Long-running work should not unnecessarily delay user interactions.

---

# 5. Database Performance

PostgreSQL is expected to provide excellent performance for the projected scale of DiaperScout.

Performance should primarily be achieved through:

- Appropriate indexes
- Efficient queries
- Thoughtful schema design
- PostgreSQL capabilities
- Entity Framework Core best practices

Entity Framework Core queries should be reviewed where performance concerns arise.

---

# 6. Search Performance

Search is considered one of the platform's most important capabilities.

The search experience should:

- Return results quickly
- Support partial matches
- Tolerate minor spelling mistakes
- Scale as the catalogue grows

The initial implementation should use PostgreSQL full-text search and trigram similarity.

Dedicated search infrastructure should only be introduced if PostgreSQL becomes a demonstrated bottleneck.

---

# 7. Image Performance

Images should be optimised for delivery.

Practices include:

- Responsive image sizes
- Thumbnail generation
- Efficient compression
- CDN delivery through Cloudflare

Product gallery images and observation images may use different optimisation strategies based on their purpose.

---

# 8. API Performance

API endpoints should:

- Return only necessary data
- Support pagination
- Avoid unnecessary database queries
- Minimise payload size

Endpoints should be designed around user needs rather than exposing complete database objects.

---

# 9. Frontend Performance

The Blazor Web App should prioritise:

- Fast initial loading
- Responsive interactions
- Efficient rendering
- Progressive enhancement

Client-side work should be kept proportionate to the value it provides.

---

# 10. Background Processing

Operations that are not required to complete the current user interaction should execute asynchronously where appropriate.

Examples may include:

- Image optimisation
- Atlas recalculation
- Notification delivery
- Future analytics processing

Users should not wait unnecessarily for background tasks to complete.

---

# 11. Caching

Caching should be introduced only where it provides measurable benefit.

Potential candidates include:

- Frequently viewed products
- Search suggestions
- Static reference data

Caching should remain an optimisation rather than a dependency.

Correctness should never rely upon cached data.

---

# 12. Monitoring

Performance should be continuously observed using the platform's observability tooling.

Important metrics include:

- API response times
- Database query duration
- Search latency
- Image delivery
- Page load times

Performance regressions should be investigated promptly.

---

# 13. Scalability

The platform should scale through good engineering practices rather than unnecessary architectural complexity.

Initial priorities include:

- Efficient APIs
- Well-designed database queries
- Appropriate indexing
- CDN usage
- Stateless application services

More advanced scaling techniques should be introduced only when operational requirements justify them.

---

# 14. Future Evolution

Future performance improvements may include:

- Read replicas
- Distributed caching
- Dedicated search infrastructure
- Background workers
- Regional deployments

These capabilities should address demonstrated needs rather than anticipated ones.

---

# 15. Design Philosophy

Performance should support the user experience without compromising maintainability.

DiaperScout favours straightforward, measurable optimisation over speculative complexity.

A well-designed platform should remain fast because it is simple, not because it has accumulated layers of optimisation.