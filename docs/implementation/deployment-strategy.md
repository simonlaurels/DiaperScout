# Deployment Strategy

**Document Status:** Draft  
**Version:** 1.0  
**Owner:** DiaperScout Project  
**Last Updated:** 2026-08-02

---

# 1. Purpose

This document defines how the DiaperScout platform is deployed from local development through to production.

The deployment strategy aims to produce a platform that is:

- Repeatable
- Reliable
- Observable
- Secure
- Easy to operate
- Cloud agnostic where practical

Deployment should be automated wherever possible.

---

# 2. Philosophy

Deployment should be a predictable engineering process rather than a manual operational task.

Developers should be able to move code from development to production with minimal manual intervention.

Every deployment should be:

- Repeatable
- Version controlled
- Automated
- Recoverable

---

# 3. Deployment Principles

## Infrastructure as Code

Infrastructure configuration should be version controlled alongside the application wherever practical.

Infrastructure should be reproducible rather than manually configured.

---

## Automation First

Deployments should execute through automated pipelines.

Manual deployment should only occur during exceptional operational circumstances.

---

## Small, Frequent Releases

DiaperScout should favour smaller deployments over infrequent large releases.

Smaller deployments reduce operational risk and simplify troubleshooting.

---

## Safe Rollback

Every deployment should support rollback where practical.

The ability to recover quickly is considered more valuable than preventing every possible deployment failure.

---

# 4. Local Development

Local development is orchestrated using **.NET Aspire**.

Aspire provides:

- Service orchestration
- Configuration
- Service discovery
- Local infrastructure
- Health monitoring
- Development dashboard

Developers should use Aspire as the standard entry point for running the platform.

---

# 5. Source Control

Git is the authoritative source for all application code.

GitHub hosts the primary repository.

Every deployment originates from a committed revision.

Direct modification of production systems is not supported.

---

# 6. Continuous Integration

Every change should pass automated validation before deployment.

Typical validation includes:

- Build
- Unit tests
- Integration tests
- Static analysis
- Formatting
- Security scanning

Code that fails validation should not progress through the deployment pipeline.

---

# 7. Continuous Deployment

Deployment should be automated following successful validation.

The deployment pipeline should:

1. Build the application
2. Execute automated tests
3. Publish deployment artefacts
4. Deploy infrastructure updates
5. Deploy application updates
6. Verify application health

---

# 8. Hosting Strategy

The implementation intentionally separates application development from hosting decisions.

The platform should remain deployable to multiple hosting providers.

Potential hosting environments include:

- Azure
- DigitalOcean
- Railway
- Render
- Self-managed Linux infrastructure

The implementation should avoid unnecessary coupling to any single cloud provider.

---

# 9. Cloudflare

Cloudflare forms part of the production edge platform.

Responsibilities include:

- DNS
- CDN
- HTTPS
- Edge caching
- Static asset delivery

Cloudflare services should improve performance without affecting application portability.

---

# 10. Object Storage

Cloudflare R2 stores binary assets.

Examples include:

- Product images
- Observation images
- Future media assets

Application metadata remains within PostgreSQL.

---

# 11. Database Deployment

Database schema changes are managed through Entity Framework Core migrations.

Deployment should ensure:

- Schema updates are version controlled
- Migrations execute safely
- Rollback procedures are documented

Manual schema modification should be avoided.

---

# 12. Configuration Management

Configuration should differ by environment without requiring code changes.

Examples include:

- Development
- Test
- Staging
- Production

Sensitive values should never be stored in source control.

Secrets should be managed through the hosting platform or secure secret storage.

---

# 13. Health Monitoring

Following deployment, the platform should verify:

- API availability
- Database connectivity
- Storage availability
- Background services
- Authentication

Failed health checks should prevent incomplete deployments from remaining active.

---

# 14. Future Evolution

Future deployment enhancements may include:

- Blue/Green deployments
- Canary releases
- Regional deployments
- Automated scaling
- Disaster recovery improvements

These capabilities should build upon the existing deployment pipeline rather than replacing it.

---

# 15. Design Philosophy

Deployment should become a routine engineering activity rather than a stressful operational event.

A successful deployment strategy allows developers to focus on improving DiaperScout while maintaining confidence that new releases can be delivered safely, consistently and predictably.

Automation, observability and repeatability are considered fundamental characteristics of the deployment process.