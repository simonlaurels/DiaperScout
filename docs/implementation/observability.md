# Observability

**Document Status:** Draft  
**Version:** 1.1  
**Owner:** DiaperScout Project  
**Last Updated:** 2026-08-13

---

# 1. Purpose

This document defines the observability strategy for the DiaperScout platform.

Observability provides insight into the behaviour, health and performance of the platform throughout development and production.

The objective is to make problems easy to detect, understand and resolve.

Observability should provide enough information to answer:

- Is the platform healthy?
- What failed?
- Why did it fail?
- Which component is responsible?
- How many Users or workflows are affected?
- Has performance changed?
- When did the problem begin?
- Has a deployment introduced the problem?

The goal is not to collect data for its own sake, but to provide actionable information.

---

# 2. Philosophy

Observability is a production capability rather than an optional diagnostic feature.

The platform should be observable from:

```text
User Request
     ↓
Web
     ↓
API
     ↓
Application
     ↓
Domain / Infrastructure
     ↓
PostgreSQL / External Services
```

A failure should be traceable through the relevant layers without requiring guesswork.

Observability should support:

- Development
- Testing
- Deployment
- Production operations
- Incident investigation
- Performance analysis

---

# 3. Core Pillars

DiaperScout adopts the three recognised pillars of observability:

- Logs
- Metrics
- Traces

These should complement one another.

```text
Logs
  +
Metrics
  +
Traces
  ↓
Operational Understanding
```

No single pillar should be treated as sufficient for diagnosing every problem.

---

# 4. Logs

Logs are structured records of significant application and infrastructure events.

Examples include:

- Authentication events
- Authorisation failures
- Observation submission
- Editorial decisions
- Atlas publication
- Background processing
- Search indexing
- Media processing
- External service failures
- Deployment events
- Application errors

Logs should describe meaningful events rather than record every method invocation.

---

# 5. Structured Logging

Production logs should use structured logging.

Important contextual information should be represented as structured properties rather than embedded solely within human-readable strings.

Useful context may include:

- Timestamp
- Log level
- Application component
- Environment
- Source revision
- Correlation identifier
- Trace identifier
- Operation name
- Resource identifier where appropriate
- Error information

Structured logs should remain searchable and machine-readable.

---

# 6. Correlation

Related operations should be traceable across application boundaries.

Where practical, requests should carry correlation and trace information through:

```text
Web
 ↓
API
 ↓
Application
 ↓
Infrastructure
 ↓
Database / External Service
```

This allows operators to connect:

- API requests;
- background operations;
- database activity;
- external service calls;

when investigating a specific failure.

Correlation identifiers must not contain personal information.

---

# 7. Logging Levels

Standard logging levels should be used consistently.

| Level | Usage |
|--------|-------|
| Trace | Extremely detailed diagnostics |
| Debug | Development and troubleshooting diagnostics |
| Information | Significant normal platform behaviour |
| Warning | Unexpected but recoverable conditions |
| Error | Failed operations requiring investigation |
| Critical | Platform-threatening failures |

Production logging should normally avoid excessive Trace or Debug output unless temporarily enabled for investigation.

Logs should remain meaningful and proportional.

---

# 8. Logging Examples

Useful production logging includes:

```text
Information:
Observation submitted

Warning:
Search indexing delayed

Error:
Failed to process uploaded Evidence

Critical:
Database unavailable
```

Avoid meaningless logging such as:

```text
Information:
Method started

Information:
Method finished

Information:
Variable changed
```

unless such information is genuinely required for a specific diagnostic purpose.

---

# 9. Sensitive Information

Observability must never compromise security or privacy.

Logs must never contain:

- Passwords
- Magic-link tokens
- Passkeys
- Authentication tokens
- Session secrets
- Database credentials
- API keys
- Encryption keys
- Object-storage credentials

Personally identifiable information should be logged only where operationally necessary.

Sensitive information should be excluded from telemetry by design rather than relying solely on operators to remove it later.

---

# 10. Resource Identifiers

Identifiers may be included in logs where they materially improve diagnosis.

Examples include:

- Product ID
- Observation ID
- Editorial Decision ID
- Discovery Task ID
- Background Job ID
- Deployment ID

Where an identifier could reveal sensitive personal information, it should not be logged.

Email addresses and other personal identifiers should generally be avoided unless genuinely required for operational diagnosis.

---

# 11. Metrics

Metrics are numerical measurements describing platform behaviour.

Important platform metrics include:

- Request rate
- Request duration
- Error rate
- Database connection health
- Database query duration
- Search latency
- Observation submission rate
- Editorial processing time
- Media upload duration
- Background job duration
- Background queue depth
- Failed background jobs
- Search indexing backlog
- Storage failures

Metrics should support both operational monitoring and performance investigation.

---

# 12. Metric Design

Metrics should have a clearly defined purpose.

Avoid creating large numbers of metrics that are never used.

Metrics should use controlled dimensions.

High-cardinality values such as:

- email addresses;
- arbitrary User IDs;
- free-form search terms;
- complete URLs;

should not normally be used as metric dimensions.

High-cardinality telemetry can become expensive and difficult to operate.

---

# 13. Business Metrics

Business and community metrics may be introduced where they provide meaningful product insight.

Examples include:

- Observations submitted
- Observations accepted
- Observations rejected
- Products discovered
- Editorial processing time
- Discovery Tasks completed
- Search usage
- Atlas publication rate

Business metrics must respect privacy requirements.

Community Trust may be measured internally, but telemetry must not inadvertently expose private User information.

---

# 14. Tracing

Distributed tracing allows a request or background operation to be followed through multiple components.

A typical request may appear as:

```text
Web
 ↓
API
 ↓
Application
 ↓
Infrastructure
 ↓
PostgreSQL
```

An external operation may additionally include:

```text
Application
 ↓
Object Storage
```

Tracing should make it possible to identify where time is being spent and where failures occur.

---

# 15. .NET Aspire

.NET Aspire provides the foundation for local observability.

Expected capabilities include:

- Service dashboard
- Health status
- Request tracing
- Logs
- Metrics

Developers should use Aspire during normal development to understand application behaviour.

The local observability experience should resemble production concepts where practical.

---

# 16. OpenTelemetry

OpenTelemetry should be used as the preferred instrumentation model for application telemetry.

Where practical, instrumentation should cover:

- ASP.NET Core
- HTTP calls
- PostgreSQL
- Background processing
- Application operations
- Relevant external services

OpenTelemetry provides a vendor-neutral telemetry model and helps preserve deployment flexibility.

---

# 17. Trace Sampling

Production tracing should use an appropriate sampling strategy.

The platform should capture enough traces to diagnose problems without creating unnecessary telemetry volume.

Important failures and representative requests should remain observable.

Sampling configuration should be treated as an operational concern rather than hard-coded into business logic.

---

# 18. Health Checks

Health checks should verify critical platform dependencies.

Examples include:

- PostgreSQL connectivity
- Object storage availability
- Authentication infrastructure
- Background processing
- Required external services

Health checks should distinguish between:

### Liveness

Is the application process running?

### Readiness

Is the application capable of serving normal traffic?

A process that is running but cannot access a required dependency should not necessarily be considered ready.

---

# 19. Health Endpoints

Health endpoints should be available to:

- Deployment pipelines
- Hosting infrastructure
- Monitoring systems
- Operational tooling

Health endpoints must not expose sensitive diagnostic information publicly.

Detailed dependency information should be available only through appropriate protected operational mechanisms.

---

# 20. Background Processing

Background processing must be observable independently from Web/API health.

Important background operations include:

- Media processing
- Thumbnail generation
- Search indexing
- Notification delivery
- Discovery Task generation
- Community Trust evaluation
- Availability freshness calculations
- Outbox processing

The Web/API being healthy does not mean the entire platform is healthy.

---

# 21. Background Job Metrics

Background processing should expose useful operational metrics such as:

- Queue depth
- Processing duration
- Success count
- Failure count
- Retry count
- Oldest pending job
- Permanently failed jobs

A growing queue should be detectable before it becomes a major service problem.

---

# 22. Outbox Observability

Where the Outbox pattern is implemented, the platform should monitor:

- Pending events
- Processing rate
- Processing failures
- Retry count
- Oldest pending event
- Permanently failed events

An Outbox backlog should be treated as an operational condition requiring investigation when it exceeds appropriate thresholds.

---

# 23. Search Observability

Search is a derived system and should be monitored independently.

Important signals include:

- Search request latency
- Search error rate
- Indexing rate
- Indexing failures
- Queue depth
- Publication-to-index delay
- Missing or failed index updates

Search failures must not compromise canonical Atlas data.

---

# 24. Media Observability

Media processing should provide visibility into:

- Upload success
- Upload failures
- Processing duration
- Processing failures
- Queue depth
- Retry count
- Storage failures

Original Evidence must remain recoverable when processing fails.

Derived media should be regenerable where practical.

---

# 25. Database Observability

PostgreSQL health and performance should be monitored.

Important signals include:

- Connectivity
- Connection pool usage
- Query duration
- Error rate
- Transaction failures
- Lock contention
- Migration status
- Storage capacity
- Database resource usage

Slow or failing database operations should be identifiable through application telemetry.

---

# 26. API Observability

API telemetry should provide visibility into:

- Request rate
- Response duration
- HTTP status codes
- Validation failures
- Authentication failures
- Authorisation failures
- Rate limiting
- Server errors

API telemetry should distinguish between expected client errors and server-side failures.

A large number of invalid client requests may indicate abuse or an application usability problem and should be investigated appropriately.

---

# 27. Authentication Observability

Authentication events should be observable without exposing authentication secrets.

Important events include:

- Authentication success
- Authentication failure
- Magic-link request
- Expired authentication attempt
- Passkey registration
- Passkey authentication
- Session creation
- Session revocation
- Authentication-method changes

Telemetry should support investigation of suspicious authentication activity.

Authentication tokens and credentials must never be logged.

---

# 28. Authorisation Observability

Repeated authorisation failures may indicate:

- A legitimate application problem
- A configuration error
- An attempted privilege escalation
- Abuse

Important security events should be observable without exposing sensitive details.

Privileged operations should produce appropriate audit records as defined by the Security strategy.

---

# 29. Audit Logging

Observability and audit logging serve different purposes.

Observability answers:

> What is happening to the system?

Audit logging answers:

> What security-sensitive or governance action happened?

Security-sensitive actions should therefore be recorded in appropriate audit records.

Examples include:

- Role changes
- Editorial decisions
- Manufacturer verification
- Privileged administrative actions
- Authentication-method changes
- Account recovery
- Significant security configuration changes

Audit records should be protected against unauthorised modification.

---

# 30. Deployment Observability

Every production deployment should be observable.

The operational record should identify:

- Source revision
- Build artefact
- Environment
- Deployment time
- Deployment result
- Database migration state
- Application health after deployment

Where possible, telemetry should allow operators to correlate a change in platform behaviour with a specific deployment.

---

# 31. Performance Monitoring

Performance should be continuously monitored.

Important areas include:

- API response time
- Product retrieval
- Product search
- Atlas queries
- Location queries
- Observation submission
- Editorial processing
- Image upload
- Media processing
- Database queries
- Background jobs

Performance baselines should be established as the platform matures.

Unexpected degradation should trigger investigation.

---

# 32. Error Handling

Unexpected exceptions should:

- Be logged
- Include sufficient diagnostic context
- Be associated with the relevant trace where possible
- Return safe responses to clients

Internal implementation details must never be exposed through the API.

Repeated identical failures should be detectable through aggregation rather than requiring manual inspection of individual log entries.

---

# 33. Alerting

Alerts should be generated only for conditions requiring attention.

Examples include:

- Database unavailable
- Application unavailable
- Readiness failures
- Elevated error rates
- Sustained latency degradation
- Storage failures
- Failed deployments
- Authentication abuse
- Background queue backlog
- Outbox backlog
- Search indexing failure
- Insufficient storage capacity

Alerts should be based on meaningful thresholds rather than individual transient events wherever practical.

---

# 34. Alert Fatigue

Excessive alerting reduces operational effectiveness.

Alerts should therefore:

- Have a clear owner.
- Represent an actionable condition.
- Avoid duplicate notifications.
- Include useful diagnostic context.
- Provide enough information to begin investigation.

Warnings that require no action should generally remain telemetry rather than become alerts.

---

# 35. Dashboards

Production should provide operational dashboards appropriate to the platform's size.

A primary dashboard should provide visibility into:

- Availability
- Request rate
- Error rate
- Latency
- Database health
- Background processing
- Search
- Media processing
- Recent deployments

Additional dashboards may be introduced for:

- Authentication
- Editorial operations
- Community workflows
- Capacity
- Performance

Dashboards should remain focused on actionable information.

---

# 36. Incident Investigation

Observability should support a repeatable investigation process.

A typical investigation should be able to move from:

```text
Alert
 ↓
Affected Component
 ↓
Trace
 ↓
Related Logs
 ↓
Metrics
 ↓
Deployment / Configuration Change
 ↓
Root Cause
```

Operators should not need to manually correlate unrelated systems where the platform can provide correlation identifiers automatically.

---

# 37. Privacy

Telemetry must follow the same privacy principles as the rest of the application.

Observability data should:

- Minimise personal information.
- Avoid unnecessary User identifiers.
- Avoid authentication secrets.
- Have appropriate retention.
- Be access-controlled.

Telemetry should not become an uncontrolled secondary database of User behaviour.

---

# 38. Retention

Telemetry retention should be proportionate to its purpose.

Different categories may have different retention requirements:

- High-volume diagnostic logs
- Operational metrics
- Traces
- Security audit records

Long-term retention should be limited to information that has a genuine operational, security or compliance purpose.

---

# 39. Access Control

Observability systems contain operationally sensitive information.

Access should therefore be restricted to authorised personnel.

Production logs, metrics, traces and dashboards should not be publicly accessible.

Security-sensitive telemetry should have appropriate additional restrictions.

---

# 40. Development and Test Environments

Development and test environments should provide useful observability without producing unnecessary operational noise.

Local development should favour:

- Detailed logs
- Aspire dashboard visibility
- Tracing
- Debugging information

Production should favour:

- Actionable logs
- Meaningful metrics
- Controlled tracing
- Security-conscious diagnostics

Telemetry configuration should therefore be environment appropriate.

---

# 41. Testing Observability

Observability itself should be tested where practical.

Tests should verify:

- Health endpoints behave correctly.
- Critical failures produce appropriate telemetry.
- Sensitive information is not logged.
- Important background failures are visible.
- Deployment health checks detect failed dependencies.

Observability should not be considered reliable merely because the application compiles.

---

# 42. Operational Failure Modes

The platform should remain diagnosable when individual systems fail.

Examples include:

### Database unavailable

The application should expose an appropriate readiness failure and produce useful diagnostics without exposing database credentials.

### Object storage unavailable

Media operations should fail safely and remain observable.

### Search unavailable

The Atlas should remain authoritative and search failure should not corrupt published data.

### Background processor unavailable

Queued work should remain recoverable and backlog should be visible.

### External service unavailable

The application should fail gracefully where possible and expose provider failures through telemetry.

---

# 43. Future Evolution

Future observability improvements may include:

- Custom operational dashboards
- Advanced business metrics
- Automated anomaly detection
- Synthetic monitoring
- Distributed tracing across additional external services
- Automated incident correlation
- Capacity forecasting
- More advanced security analytics

Additional tooling should complement the existing observability strategy rather than replace it.

New telemetry should have a clear operational purpose.

---

# 44. Design Philosophy

A healthy platform should be understandable without guesswork.

Observability should make the internal behaviour of DiaperScout transparent to developers and operators while remaining largely invisible to Users.

When issues occur, the platform should provide sufficient information to identify the problem quickly and confidently.

Good observability reduces:

- Downtime
- Mean time to detect
- Mean time to diagnose
- Mean time to recover
- Developer frustration

The objective is not perfect visibility into everything.

The objective is **useful visibility into the things that matter**.

---

# 45. Relationship to Other Documents

This document defines how DiaperScout exposes operational information about its behaviour and health.

Related documents include:

- **Security** — defines security and privacy requirements for telemetry.
- **Deployment Strategy** — defines deployment verification and operational processes.
- **Testing Strategy** — defines testing of health and observability behaviour.
- **Data Access Strategy** — defines database access and persistence.
- **Coding Standards** — defines logging and implementation conventions.
- **Implementation Overview** — defines the overall production implementation.
- **Workflow Architecture** — defines important workflows whose state should be observable.

---

# 46. Summary

The DiaperScout observability model is:

```text
Logs
  +
Metrics
  +
Traces
  +
Health Checks
  +
Audit Records
      ↓
Operational Understanding
```

The platform should make it possible to understand:

- whether it is healthy;
- what is failing;
- where it is failing;
- why it is failing;
- whether a deployment caused the problem;
- whether background work is keeping up;
- whether security-sensitive activity requires investigation.

Observability must remain secure and privacy-conscious.

The goal is simple:

> **When something goes wrong, we should know what happened without having to guess.**