# Backup and Recovery

**Document Status:** Draft  
**Version:** 1.0  
**Owner:** DiaperScout Project  
**Last Updated:** 2026-08-13

---

# 1. Purpose

This document defines the backup and recovery strategy for the DiaperScout production platform.

The objective is to ensure that the platform can recover from:

- Hardware failure
- Database corruption
- Accidental deletion
- Application failure
- Infrastructure failure
- Security incidents
- Object-storage loss
- Deployment failure
- Complete hosting-provider failure

Recovery must protect not only application availability, but also the integrity and provenance of the Atlas.

---

# 2. Recovery Philosophy

The most important asset is the trusted state of the platform.

This includes:

- Canonical Atlas knowledge
- Products
- Product Variants
- Observations
- Evidence metadata
- Editorial Decisions
- Provenance
- Community contribution history
- User data
- Discovery Tasks
- Backpack data

The objective of recovery is therefore:

> **Restore the platform to a known trustworthy state, not merely restart the application.**

---

# 3. Authoritative Sources

DiaperScout consists of several categories of data.

| Data | Primary Source |
|---|---|
| Application code | Git repository |
| Infrastructure configuration | Infrastructure source/configuration |
| PostgreSQL data | PostgreSQL |
| Media objects | Object storage |
| Search index | Derived data |
| Background queues | Database / durable job state |
| Configuration secrets | Secure secret store |
| Build artefacts | CI/CD artefact storage |

Recovery procedures should restore authoritative sources first and reconstruct derived systems afterwards where practical.

---

# 4. Recovery Priority

The recovery priority is:

```text
1. Infrastructure
      ↓
2. PostgreSQL
      ↓
3. Application
      ↓
4. Object Storage / Media
      ↓
5. Authentication
      ↓
6. Background Processing
      ↓
7. Search
      ↓
8. Non-critical Supporting Services
```

The exact sequence may vary depending on the incident.

The priority is to restore the authoritative application state before derived services.

---

# 5. Recovery Objectives

The production environment should establish two primary recovery objectives.

## Recovery Point Objective

RPO defines the maximum acceptable amount of data that may be lost following an incident.

The initial target is:

> **RPO: 24 hours for general disaster recovery, with more frequent recovery capability for PostgreSQL where the hosting provider supports it.**

Critical production data should therefore not rely solely on weekly or manually created backups.

---

## Recovery Time Objective

RTO defines the target time within which the service should be restored.

The initial target is:

> **RTO: 24 hours for a complete infrastructure-loss scenario.**

Routine application failures should have substantially shorter recovery times.

These targets should be reviewed as the platform grows and operational requirements become clearer.

---

# 6. PostgreSQL Backups

PostgreSQL is the authoritative store for structured application data.

Backups must protect:

- Products
- Product Variants
- Observations
- Evidence metadata
- Editorial Decisions
- Provenance
- Community Trust data
- Discovery Tasks
- Backpack data
- Authentication data where applicable
- Audit records

The production PostgreSQL environment should use automated backups.

---

# 7. Point-in-Time Recovery

Where supported by the production PostgreSQL provider, Point-in-Time Recovery should be enabled.

PITR allows recovery to a selected point before an incident rather than only restoring the most recent full backup.

This is particularly valuable for:

- Accidental deletion
- Data corruption
- Failed migrations
- Application defects that modify data incorrectly

PITR should be preferred over restoring an old full backup when the provider supports it safely.

---

# 8. Backup Frequency

The initial PostgreSQL backup strategy should include:

- Automated daily backups
- Continuous or frequent transaction-log/WAL retention where supported
- Additional backups before significant production migrations

Backup frequency should be increased if operational requirements or data volume justify it.

---

# 9. Backup Retention

Backups should use a defined retention policy.

An initial policy may include:

- Daily backups retained for at least 14 days
- Weekly backups retained for at least 8 weeks
- Monthly backups retained for at least 12 months where operationally appropriate

Retention must balance:

- Recovery capability
- Storage cost
- Privacy requirements
- Operational requirements

Long-term retention should not be enabled without a genuine requirement.

---

# 10. Backup Encryption

Backups must be encrypted at rest.

Encryption keys and credentials must be protected separately from the backup data.

Backup access must be restricted to authorised operational personnel and recovery systems.

---

# 11. Backup Isolation

A backup should not depend entirely on the same failure domain as the production system.

Where practical, backups should be stored separately from the primary production infrastructure.

Protection should account for:

- Hosting failure
- Accidental deletion
- Credential compromise
- Ransomware
- Operator error

A backup that can be deleted by the same compromised credentials as the production database provides limited protection.

---

# 12. Object Storage Backups

Cloudflare R2 stores application media.

Important media includes:

- Observation Evidence
- Product images
- Editorial media
- Other user-submitted assets

Object storage must have an appropriate protection strategy.

Where supported and justified, this should include:

- Object versioning
- Retention controls
- Protection against accidental deletion
- Separate backup or replication for critical media

The exact mechanism depends on the selected production storage configuration.

---

# 13. Media Recovery

Media recovery must preserve the distinction between:

```text
Original Evidence
        ↓
Derived Media
```

Original Evidence is authoritative.

Derived assets such as:

- thumbnails;
- resized images;
- optimised formats;

should be considered reconstructable where practical.

A media-processing failure must not result in permanent loss of the original Evidence.

---

# 14. Search Recovery

Search data is derived from authoritative application data.

Search indexes do not need to be treated as the primary backup of Atlas information.

If the search system is lost:

```text
PostgreSQL
    ↓
Published Atlas
    ↓
Search Rebuild
    ↓
Search Index
```

The search index should therefore be rebuildable from authoritative data wherever practical.

---

# 15. Application Recovery

Application code is stored in Git and should not normally require a separate application backup.

A production application can be reconstructed from:

- Source repository
- Dependency definitions
- Infrastructure configuration
- Deployment configuration
- Build pipeline
- Environment configuration

Production-specific secrets must be restored separately through the secure secret-management system.

---

# 16. Infrastructure Recovery

Infrastructure should be reproducible wherever practical.

Recovery should therefore use:

- Infrastructure configuration
- Deployment configuration
- Environment configuration
- Documented operational settings

Manual production configuration should not be the only way to reconstruct the platform.

Infrastructure changes that cannot be reproduced automatically should be documented.

---

# 17. Secrets Recovery

Secrets must be recoverable without being stored in application backups.

Examples include:

- Database credentials
- Object-storage credentials
- Authentication secrets
- Email credentials
- API keys
- Encryption keys

Secrets should be stored in the designated secure secret-management system.

Recovery procedures must establish how authorised operators regain access to those secrets following infrastructure loss.

---

# 18. Authentication Recovery

Authentication infrastructure is critical to restoring access to the platform.

Recovery must preserve the appropriate authentication state, including:

- User accounts
- Authentication methods
- Passkey credentials where stored by the platform
- Session configuration
- Account recovery mechanisms

Authentication recovery must not expose credentials or authentication secrets.

---

# 19. Deployment Recovery

If a deployment causes application failure, the preferred recovery mechanism is to deploy the last known healthy application artefact.

Database compatibility must be considered before rollback.

Where the database has changed, the application should only be rolled back to a version known to be compatible with the current schema.

---

# 20. Database Migration Recovery

Database migrations require particular care.

A migration should not normally be reversed simply because an application deployment failed.

The preferred pattern is:

```text
Expand
   ↓
Deploy
   ↓
Validate
   ↓
Adopt
   ↓
Contract
```

This allows the application to be rolled back without requiring destructive database rollback in many cases.

If a migration has caused data corruption, recovery may require:

- Point-in-Time Recovery
- Restoring a backup
- Data repair
- A corrective migration

The appropriate method depends on the failure.

---

# 21. Disaster Scenarios

Recovery procedures should account for at least the following scenarios.

## Application Failure

Restore or redeploy the last known healthy application version.

---

## Database Failure

Restore PostgreSQL using:

- Provider recovery mechanisms;
- PITR;
- Full backup restoration;

as appropriate.

---

## Accidental Data Deletion

Use PITR or an appropriate backup to recover the affected state.

Where possible, recover data into a separate environment first and validate it before modifying production.

---

## Corrupt Data

Identify the point at which corruption began.

Recover to a known-good state where appropriate.

Do not overwrite the only surviving copy of the affected data before the recovery state has been validated.

---

## Object Storage Failure

Restore media from the configured object-storage recovery mechanism or secondary copy where available.

Reconcile restored media with PostgreSQL metadata.

---

## Complete Hosting Failure

Reconstruct infrastructure using the documented deployment configuration.

Restore:

1. PostgreSQL
2. Application
3. Object storage/media
4. Authentication configuration
5. Background processing
6. Search

Validate the platform before returning it to normal operation.

---

## Security Incident

Security incidents require containment before recovery.

Recovery may include:

- Revoking credentials
- Rotating secrets
- Isolating affected infrastructure
- Restoring known-good data
- Reviewing audit records
- Rebuilding compromised infrastructure
- Re-indexing derived systems

Restoring from backup alone does not resolve a security incident.

---

# 22. Recovery Environment

Where practical, recovery should initially occur in an isolated environment.

This allows operators to:

- Validate the backup
- Inspect restored data
- Verify migrations
- Check media
- Test application startup
- Confirm authentication
- Rebuild search

Only after validation should the recovered environment become the production environment.

---

# 23. Restore Procedure

A general full-recovery sequence is:

```text
1. Identify incident
        ↓
2. Contain incident
        ↓
3. Identify recovery point
        ↓
4. Provision infrastructure
        ↓
5. Restore PostgreSQL
        ↓
6. Restore object storage / media
        ↓
7. Restore secrets/configuration
        ↓
8. Deploy application
        ↓
9. Run migrations if required
        ↓
10. Verify application
        ↓
11. Start background processing
        ↓
12. Rebuild search
        ↓
13. Run smoke tests
        ↓
14. Validate data integrity
        ↓
15. Return to service
```

The sequence should be adapted to the specific incident.

---

# 24. Data Integrity Validation

Following recovery, the platform should verify:

- Database connectivity
- Expected schema version
- Referential integrity
- Atlas availability
- Observation availability
- Editorial history
- Provenance
- Media accessibility
- Authentication
- Background processing
- Search

The recovery should not be considered complete until the trusted state of the platform has been verified.

---

# 25. Search Rebuild

If search is unavailable or corrupted, it should be rebuilt from the authoritative Atlas.

The rebuild process should:

1. Confirm the database is healthy.
2. Identify published Atlas information.
3. Rebuild the search index.
4. Monitor indexing failures.
5. Verify representative search queries.
6. Confirm publication/index consistency.

Search recovery must not modify canonical Atlas information.

---

# 26. Background Processing Recovery

Background queues and Outbox events must be reviewed following recovery.

Operators should establish:

- Which jobs were completed.
- Which jobs remain pending.
- Which jobs failed.
- Whether any jobs may have been processed twice.
- Whether idempotency protections remain effective.

Work should be resumed only after the recovered database and application are known to be consistent.

---

# 27. Recovery and Provenance

Recovery must preserve provenance.

Historical information must remain attributable according to the domain rules.

Recovery procedures must not:

- Recreate Observations as new contributions unnecessarily.
- Remove Editorial Decisions.
- Replace historical Evidence without preserving provenance.
- Reset Community Trust without an explicit reason.
- Alter publication history without an auditable corrective action.

The recovery process itself should be auditable.

---

# 28. Backup Monitoring

Backups must be monitored.

Monitoring should detect:

- Failed backups
- Missing backups
- Unexpectedly small backups
- Backup storage failures
- Expired retention
- Failed replication
- PITR/WAL failures where applicable

A backup system that silently stops working is not a reliable recovery system.

---

# 29. Restore Monitoring

Successful backup creation is not sufficient evidence that recovery will work.

Restore testing should verify that backups can actually be used.

Restore tests should be observable and their results recorded.

---

# 30. Restore Testing

Restore testing should occur periodically.

Tests should include:

- PostgreSQL restoration
- PITR where available
- Application reconstruction
- Migration compatibility
- Object-storage/media recovery
- Search reconstruction
- Authentication recovery
- Background processing recovery

The frequency should increase as the platform becomes more operationally important.

---

# 31. Disaster Recovery Exercise

A full recovery exercise should be performed periodically.

A recovery exercise should simulate a meaningful failure such as:

> Complete loss of the production application environment.

The exercise should measure:

- Time to begin recovery
- Time to restore database
- Time to deploy application
- Time to restore media
- Time to restore authentication
- Time to rebuild search
- Time to return to service

The exercise should identify gaps in the documented recovery process.

---

# 32. Recovery Documentation

Recovery procedures should remain understandable to an operator who did not build the original system.

Documentation should include:

- Recovery order
- Backup locations
- Recovery credentials/process
- Infrastructure reconstruction process
- Database restoration process
- Media restoration process
- Search rebuild process
- Verification steps
- Escalation paths

Sensitive credentials must not be written directly into the recovery documentation.

---

# 33. Recovery Access

Recovery requires privileged access.

Recovery access should therefore be:

- Restricted
- Authenticated securely
- Auditable
- Tested periodically

Emergency recovery access should not depend upon the normal application being operational.

For example, restoring a database should not require logging into the application whose database is currently unavailable.

---

# 34. Privacy During Recovery

Recovery environments may contain production personal data.

They must therefore receive appropriate protection.

Recovered production data must not be copied into development environments casually.

Temporary recovery environments should be:

- Access-controlled
- Encrypted
- Monitored
- Removed or securely destroyed when no longer required

---

# 35. Recovery After Security Incidents

Following a suspected compromise, backups should not automatically be assumed trustworthy.

Operators should establish:

- When the compromise began.
- Which systems were affected.
- Whether backup credentials were compromised.
- Whether backups may have been modified.
- Which recovery point predates the compromise.

Where appropriate, recovery should use a known-good point before the incident.

Credentials must be rotated before restored systems return to normal operation.

---

# 36. Business Continuity

Backup and recovery form part of the wider business-continuity strategy.

The platform should be capable of operating again following:

- Infrastructure loss
- Provider outage
- Application failure
- Data corruption
- Security incident

The objective is to minimise disruption while protecting the integrity of the Atlas.

---

# 37. Recovery Priorities

If full recovery cannot happen immediately, restore services in this order:

```text
1. Infrastructure
2. PostgreSQL
3. Core application
4. Authentication
5. Original media / Evidence
6. Background processing
7. Search
8. Secondary services
```

The Atlas and its underlying provenance take priority over derived convenience features.

---

# 38. Definition of Recovery Complete

Recovery is complete when:

- The application is available.
- PostgreSQL is healthy.
- The expected schema is present.
- Canonical Atlas information is accessible.
- Observations and provenance are intact.
- Required media is available.
- Authentication works.
- Background processing is operating.
- Search is available or a documented degraded mode is active.
- Health checks pass.
- Critical smoke tests pass.
- Recovery actions have been recorded.

---

# 39. Future Evolution

Future recovery improvements may include:

- Multi-region PostgreSQL
- Automated cross-region replication
- Automated failover
- Secondary object-storage replication
- Lower RPO targets
- Lower RTO targets
- Automated disaster-recovery environments
- Regular automated recovery exercises

These should only be introduced where the operational requirements justify their complexity and cost.

---

# 40. Design Philosophy

Backups are not the objective.

**Recoverability is the objective.**

A successful recovery strategy means that DiaperScout can suffer a serious failure without losing the trusted history that makes the platform valuable.

The system should therefore be designed so that:

```text
Authoritative Data
        ↓
Protected
        ↓
Recoverable
        ↓
Validated
        ↓
Trusted Again
```

Recovery should be treated as a normal engineering capability rather than an emergency procedure that exists only on paper.

---

# 41. Relationship to Other Documents

This document defines backup and recovery requirements for the production platform.

Related documents include:

- **Deployment Strategy** — defines deployment and infrastructure recovery.
- **Security** — defines backup security, secrets and incident recovery.
- **Observability** — defines backup and recovery monitoring.
- **Data Access Strategy** — defines PostgreSQL persistence.
- **Database Model** — defines authoritative data structures.
- **Testing Strategy** — defines restore and recovery testing.
- **Implementation Overview** — defines the overall production architecture.

---

# 42. Summary

The DiaperScout recovery model is:

```text
Protect
   ↓
Monitor
   ↓
Detect
   ↓
Recover
   ↓
Validate
   ↓
Restore Service
```

PostgreSQL is the authoritative source for structured application data.

Object storage protects original media.

Search is derived and can be rebuilt.

Application code and infrastructure are reconstructed from controlled source.

Backups are encrypted, isolated and tested.

Database migrations are designed for recoverability.

Security incidents require containment and credential rotation before normal recovery.

Most importantly:

> **The recovery process must preserve the integrity, provenance and trustworthiness of the Atlas.**