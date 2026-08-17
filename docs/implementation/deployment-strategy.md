# Deployment Strategy

**Document Status:** Draft  
**Version:** 1.1  
**Owner:** DiaperScout Project  
**Last Updated:** 2026-08-13

---

# 1. Purpose

This document defines how the DiaperScout platform is deployed from local development through to production.

The deployment strategy aims to produce a platform that is:

- Repeatable
- Reliable
- Observable
- Secure
- Recoverable
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
- Observable
- Recoverable

A deployment is not complete merely because the application starts.

The deployment process must also establish confidence that:

- the application is healthy;
- required infrastructure is available;
- database changes have succeeded;
- background processing is functioning;
- authentication is operational;
- critical application workflows remain available.

---

# 3. Deployment Principles

## Infrastructure as Code

Infrastructure configuration should be version controlled alongside the application wherever practical.

Infrastructure should be reproducible rather than manually configured.

Manual production configuration should be treated as an exception and documented where it cannot reasonably be automated.

---

## Automation First

Deployments should execute through automated pipelines.

Manual deployment should only occur during exceptional operational circumstances.

Manual intervention must not become a hidden requirement for normal deployment.

---

## Small, Frequent Releases

DiaperScout should favour smaller deployments over infrequent large releases.

Smaller deployments reduce operational risk and simplify troubleshooting.

Changes should be independently deployable where practical.

---

## Safe Rollback

Every deployment should have a defined recovery strategy.

Application code should support rollback where practical.

Database migrations require additional care because schema changes may not be safely reversible after application data has been written against the new schema.

Where a database change cannot safely be rolled back, the preferred strategy is:

```text
Expand
   ↓
Deploy compatible application
   ↓
Migrate data where required
   ↓
Adopt new behaviour
   ↓
Retire obsolete schema
```

Rollback should therefore mean restoring a known safe application state rather than blindly reversing every database migration.

---

# 4. Deployment Environments

The platform should distinguish between:

```text
Development
     ↓
Test / CI
     ↓
Staging
     ↓
Production
```

Not every environment needs identical infrastructure, but the production runtime model should be represented closely enough in staging to provide meaningful deployment confidence.

---

# 5. Local Development

Local development is orchestrated using **.NET Aspire**.

Aspire provides:

- Service orchestration
- Configuration
- Service discovery
- Local infrastructure
- Health monitoring
- Development dashboard

Developers should use Aspire as the standard entry point for running the platform locally.

Local development should use production-relevant technologies where practical, particularly:

- PostgreSQL
- Object storage-compatible infrastructure
- Background processing

Development substitutes should not silently become production dependencies.

---

# 6. Source Control

Git is the authoritative source for all application code.

GitHub hosts the primary repository.

Every deployment originates from a committed revision.

Production deployments should reference an identifiable source revision.

Direct modification of production application files is not supported.

---

# 7. Continuous Integration

Every change should pass automated validation before deployment.

Typical validation includes:

- Build
- Unit tests
- Integration tests
- Architecture tests
- API tests
- Static analysis
- Formatting
- Security scanning

Code that fails required validation should not progress through the deployment pipeline.

---

# 8. Build Artefacts

Production deployments should use immutable build artefacts wherever practical.

The application should be built once and promoted between environments rather than rebuilt differently for each environment.

This reduces the possibility that:

- staging tests one build;
- production receives a different build.

A deployment should be traceable to:

- source revision;
- build;
- deployment time;
- target environment.

---

# 9. Continuous Deployment

Deployment should be automated following successful validation.

The deployment pipeline should:

1. Build the application.
2. Execute automated tests.
3. Run security and architecture checks.
4. Publish deployment artefacts.
5. Provision or update required infrastructure.
6. Apply approved database migrations.
7. Deploy application updates.
8. Verify application health.
9. Verify critical dependencies.
10. Confirm successful deployment.

Where deployment verification fails, the pipeline should prevent the release from being considered successful.

---

# 10. Promotion

Production releases should normally be promoted through the deployment pipeline rather than rebuilt independently.

The intended flow is:

```text
Commit
  ↓
CI
  ↓
Build
  ↓
Automated Tests
  ↓
Staging
  ↓
Deployment Verification
  ↓
Production
```

A failed staging deployment must not automatically progress to production.

---

# 11. Hosting Strategy

The implementation intentionally separates application development from hosting decisions.

The platform should remain deployable to multiple hosting providers where practical.

Potential hosting environments include:

- Azure
- DigitalOcean
- Railway
- Render
- Self-managed Linux infrastructure

The implementation should avoid unnecessary coupling to any single cloud provider.

Provider-specific infrastructure should remain outside the Domain and Application layers.

---

# 12. Cloudflare

Cloudflare forms part of the production edge platform.

Responsibilities may include:

- DNS
- CDN
- HTTPS
- Edge caching
- Static asset delivery
- Appropriate security controls

Cloudflare services should improve performance and security without affecting application portability.

Cloudflare configuration should be version controlled or otherwise managed through repeatable operational processes where practical.

---

# 13. Object Storage

Cloudflare R2 stores binary assets.

Examples include:

- Product images
- Observation images
- Evidence media
- Future media assets

Application metadata remains within PostgreSQL.

Object storage credentials must not be exposed to clients.

Access should be mediated through the application or controlled public delivery mechanisms appropriate to the media.

---

# 14. Database Deployment

Database schema changes are managed through Entity Framework Core migrations.

Deployment should ensure:

- Schema updates are version controlled.
- Migrations are reviewed.
- Migrations are tested.
- Migrations execute safely.
- Data migrations are deliberate.
- Destructive operations receive explicit review.

Manual production schema modification should be avoided.

---

# 15. Database Migration Strategy

Database migrations should favour backwards-compatible evolution.

Where practical, changes should follow an expand-and-contract approach.

For example:

```text
Release 1
    ↓
Add new column/table
    ↓
Deploy compatible application
    ↓
Populate new data
    ↓
Switch application behaviour
    ↓
Remove obsolete structure later
```

This reduces the risk of deployment failure and allows application rollback without immediately requiring database rollback.

---

# 16. Migration Safety

Production migrations must be reviewed for:

- Destructive operations
- Table rewrites
- Long-running operations
- Locking behaviour
- Index creation
- Data migration
- Backward compatibility
- Recovery implications

Large or risky migrations may need to be split across multiple releases.

A migration that cannot safely execute within the operational constraints of the production environment should not be deployed without an explicit plan.

---

# 17. Configuration Management

Configuration should differ by environment without requiring code changes.

Examples include:

- Development
- Test
- Staging
- Production

Configuration should be supplied through environment-appropriate mechanisms.

Sensitive values must never be stored in source control.

Examples include:

- Database credentials
- Object storage credentials
- Authentication secrets
- Email credentials
- API keys
- Encryption keys

Secrets should be managed through the hosting platform or secure secret-management facilities.

---

# 18. Environment Separation

Production resources should remain isolated from development and test environments.

Development and test systems must not have unnecessary access to:

- Production PostgreSQL
- Production object storage
- Production authentication infrastructure
- Production secrets

Production data must not be copied into development or test environments without appropriate anonymisation and approval.

---

# 19. Health Monitoring

Following deployment, the platform should verify:

- Web application availability
- API availability
- Database connectivity
- Object storage availability
- Background processing
- Authentication
- Required external dependencies

Health checks should distinguish between:

- application liveness;
- application readiness;
- dependency health.

A service that is running but cannot access required infrastructure should not be considered fully ready.

---

# 20. Deployment Verification

Deployment verification should include automated checks where practical.

Verification should establish that:

- the application starts;
- required endpoints respond;
- the database is accessible;
- migrations have completed;
- authentication is operational;
- background processing is available;
- object storage is accessible;
- critical configuration is present.

Critical smoke tests should verify the most important production paths.

---

# 21. Background Processing

Background processing is part of the production application and must be deployed and monitored appropriately.

Examples include:

- Media processing
- Thumbnail generation
- Search indexing
- Notification delivery
- Discovery Task generation
- Community Trust evaluation
- Availability freshness calculations

Background processing should be independently observable.

A healthy Web/API process does not necessarily mean that the overall platform is healthy.

---

# 22. Event and Outbox Processing

Where the Outbox pattern is implemented, deployment must include the processes responsible for delivering queued events.

Deployment should verify:

- Outbox processing is running.
- Failed events remain recoverable.
- Events are not silently discarded.
- Duplicate processing is safe where applicable.

Application deployment must not leave an accumulated Outbox queue without detection.

---

# 23. Backups

Production backups must protect the information required to reconstruct the platform.

Backups should include, as appropriate:

- PostgreSQL data
- Canonical Atlas knowledge
- Observations
- Editorial history
- Provenance
- Relevant media
- Required configuration

Backups should be:

- Access-controlled
- Encrypted
- Retained according to defined policy
- Protected against accidental deletion
- Tested for restoration

---

# 24. Disaster Recovery

The platform should be recoverable following significant infrastructure failure.

Recovery planning should prioritise:

1. PostgreSQL data
2. Canonical Atlas
3. Observations and Evidence metadata
4. Editorial history
5. Provenance
6. Media
7. Application infrastructure
8. Supporting services

Infrastructure should be reproducible wherever practical.

The objective is not merely to restart the application.

The objective is to restore the trusted state of the platform.

---

# 25. Restore Testing

Backups are not considered reliable until restoration has been tested.

Restore testing should verify:

- Database restoration
- Migration compatibility
- Media availability
- Application startup
- Authentication
- Critical Atlas workflows

Restore tests should be performed periodically according to operational requirements.

---

# 26. Deployment Availability

Deployments should minimise user disruption.

Where practical, the deployment process should allow:

- Existing requests to complete.
- New application instances to become healthy before traffic is directed to them.
- Background processing to drain safely.
- Database changes to remain compatible with the currently running application during migration.

The platform should not require downtime for ordinary application releases unless the change genuinely requires it.

---

# 27. Blue/Green and Canary Deployment

Blue/Green and Canary deployment are not initial requirements.

They may be introduced later where:

- traffic volume justifies them;
- deployment risk warrants them;
- hosting infrastructure supports them;
- operational complexity is justified.

The deployment architecture should not require these mechanisms from the beginning.

---

# 28. Rollback

Application rollback should be possible where practical.

Rollback may involve:

- Redeploying a previous application artefact.
- Reverting configuration.
- Restoring a previous compatible infrastructure state.

Database rollback must be treated separately.

If a migration is destructive or irreversible, restoring the previous application version alone may not be safe.

The preferred approach is to design migrations so that application rollback remains possible without destructive database rollback.

---

# 29. Failed Deployment Recovery

If deployment verification fails:

1. Prevent further promotion.
2. Determine whether the application or infrastructure is unhealthy.
3. Preserve relevant logs and diagnostics.
4. Restore the last known healthy application state where safe.
5. Resolve database compatibility issues before attempting another deployment.
6. Verify the recovery.
7. Record the failure and required follow-up.

Failed deployments should not be hidden or manually repaired without understanding the underlying cause.

---

# 30. Logging and Observability

Deployment and production operations should produce sufficient information to understand:

- What was deployed.
- When it was deployed.
- Which source revision was deployed.
- Whether migrations ran.
- Whether health checks succeeded.
- Whether background processing is healthy.
- Whether external dependencies are available.

Logs must not expose:

- Secrets
- Authentication tokens
- Private authentication material
- Database credentials
- Unnecessary personal information

---

# 31. Deployment Auditability

Production deployments should be traceable.

The operational record should identify:

- Source revision
- Build artefact
- Environment
- Deployment time
- Deployment result
- Database migration state
- Operator or automation identity

Administrative deployment access should itself be protected and auditable.

---

# 32. Security

Deployment must comply with the platform Security strategy.

Production deployment should enforce:

- Secure secret handling
- Least-privilege credentials
- Environment separation
- Protected deployment access
- Dependency/security scanning
- Secure infrastructure configuration
- Auditable production changes

Production credentials must never be embedded in application artefacts.

---

# 33. Dependency and Supply Chain Security

The production build should use controlled dependency versions.

The CI process should provide appropriate checks for:

- Vulnerable dependencies
- Malicious or unexpected packages
- Unapproved dependency changes
- Build failures

Dependencies should be reviewed and updated as part of normal maintenance.

Build artefacts should be generated by the controlled CI environment.

---

# 34. Operational Access

Production access should be restricted to authorised personnel.

Administrative access should use appropriate authentication and should not be shared.

Where possible:

- Access should be role-based.
- Privileged actions should be logged.
- Production access should be minimised.
- Emergency access should be auditable.

Developers should not require unrestricted production access for normal development work.

---

# 35. Release Strategy

Releases should favour small, understandable changes.

A release should ideally have:

- A clear purpose.
- A known source revision.
- Tested application behaviour.
- Reviewed database changes.
- A defined recovery strategy.

Large feature releases should be decomposed where practical.

Feature flags may be used where they provide meaningful control over release timing, but they should not become a permanent substitute for clean deployment design.

---

# 36. Production Data Protection

Production data must be treated as a critical asset.

Deployment tooling must not:

- Reset production databases.
- Seed development data into production.
- Delete historical data without explicit authorisation.
- Replace canonical Atlas information unexpectedly.
- Overwrite production media unintentionally.

Destructive operations require explicit review.

---

# 37. Search Deployment

Search indexes are derived data.

Deployment should ensure that search infrastructure can be rebuilt from authoritative application data where practical.

A search-index failure should not corrupt the Atlas.

Following a deployment, search indexing should be monitored for:

- Failed indexing operations
- Queue backlog
- Missing publications
- Duplicate processing

---

# 38. Media Processing Deployment

Media processing should be independently recoverable.

If media processing fails:

- Original Evidence should remain preserved.
- Processing should be retryable.
- Derived assets should be regenerable.
- Failed processing should be observable.

Deployment should not make original contributor Evidence dependent upon the success of a derived-media process.

---

# 39. Performance

Deployment should not introduce avoidable performance regressions.

Where appropriate, deployment verification should consider:

- Application startup time
- Database connectivity
- Critical query performance
- API response times
- Background queue processing
- Media processing

Performance testing remains defined by the Testing Strategy.

---

# 40. Local-to-Production Consistency

Development and production will not be identical environments.

However, the following should remain conceptually consistent:

- Application architecture
- Database technology
- Persistence behaviour
- Authentication model
- Background processing model
- Configuration boundaries
- Security principles

Local development should provide enough fidelity to expose important integration problems before production deployment.

---

# 41. Future Evolution

Future deployment enhancements may include:

- Blue/Green deployments
- Canary releases
- Regional deployments
- Automated scaling
- Advanced disaster recovery
- Automated rollback
- Multi-region database strategies

These capabilities should build upon the existing deployment pipeline rather than replacing its core principles.

They should only be introduced where operational requirements justify their additional complexity.

---

# 42. Design Philosophy

Deployment should become a routine engineering activity rather than a stressful operational event.

A successful deployment strategy allows developers to focus on improving DiaperScout while maintaining confidence that new releases can be delivered:

- safely;
- consistently;
- observably;
- predictably;
- recoverably.

Automation, observability, repeatability and recoverability are fundamental characteristics of the deployment process.

---

# 43. Relationship to Other Documents

This document defines how the production application is delivered and recovered.

Related documents include:

- **Implementation Overview** — defines the overall implementation strategy.
- **Data Access Strategy** — defines persistence and database access.
- **Testing Strategy** — defines deployment validation and testing.
- **Security** — defines deployment and operational security requirements.
- **Solution Structure** — defines project and architectural boundaries.
- **Project Layout** — defines the physical repository structure.
- **Deployment & Operations** — defines the broader operational philosophy.
- **Backup and Recovery documentation** — defines detailed recovery procedures where applicable.

---

# 44. Summary

The DiaperScout deployment model is:

```text
Development
     ↓
Continuous Integration
     ↓
Build
     ↓
Automated Tests
     ↓
Staging
     ↓
Deployment Verification
     ↓
Production
     ↓
Health Monitoring
```

The production environment should be treated as a controlled, observable system rather than a manually maintained server.

Database changes must be deployed safely.

Application rollback must account for database compatibility.

Backups must be restorable.

Background processing must be monitored.

Secrets must remain outside the application artefacts.

Production changes must be auditable.

The ultimate purpose of deployment and operations is to protect the Atlas while allowing DiaperScout to evolve safely.