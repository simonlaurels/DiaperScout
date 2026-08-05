# Observability

**Document Status:** Draft  
**Version:** 1.0  
**Owner:** DiaperScout Project  
**Last Updated:** 2026-08-02

---

# 1. Purpose

This document defines the observability strategy for the DiaperScout platform.

Observability provides insight into the behaviour and health of the platform throughout development and production.

The objective is to make problems easy to detect, understand and resolve.

---

# 2. Philosophy

Observability exists to answer questions.

For example:

- Is the platform healthy?
- What failed?
- Why did it fail?
- How many users are affected?
- Has performance changed?
- When did the problem begin?

The goal is not to collect data for its own sake, but to provide actionable information.

---

# 3. Core Pillars

DiaperScout adopts the three recognised pillars of observability.

## Logs

Structured records of significant events.

Examples include:

- Authentication
- Product creation
- Review submission
- Errors
- Background processing

---

## Metrics

Numerical measurements describing platform behaviour.

Examples include:

- Request rate
- Response time
- Error rate
- Active users
- Image upload duration
- Database query duration

---

## Traces

Request tracing across the platform.

Tracing allows developers to follow a request through:

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

This makes performance bottlenecks significantly easier to diagnose.

---

# 4. .NET Aspire

.NET Aspire provides the foundation for local observability.

Expected capabilities include:

- Service dashboard
- Health status
- Request tracing
- Logs
- Metrics

Developers should use Aspire during normal development to understand application behaviour.

---

# 5. Logging

Logs should be:

- Structured
- Consistent
- Searchable
- Useful

Logs should explain:

- What happened
- When it happened
- Which component was involved

Sensitive information must never be written to logs.

---

# 6. Logging Levels

Standard logging levels should be used consistently.

| Level | Usage |
|--------|-------|
| Trace | Detailed diagnostics |
| Debug | Development diagnostics |
| Information | Normal platform behaviour |
| Warning | Unexpected but recoverable conditions |
| Error | Failed operations |
| Critical | Platform-threatening failures |

Logs should remain meaningful and proportional.

---

# 7. Health Checks

Health checks should verify critical platform dependencies.

Examples include:

- PostgreSQL connectivity
- Cloudflare R2 availability
- Authentication services
- Background workers

Health endpoints should be available to deployment pipelines and monitoring systems.

---

# 8. Performance Metrics

Performance should be continuously monitored.

Important metrics include:

- Search latency
- Product retrieval
- Review submission
- Observation processing
- Image upload time
- Database query duration

Unexpected performance degradation should be investigated.

---

# 9. Error Handling

Unexpected exceptions should:

- Be logged
- Include sufficient diagnostic context
- Return safe responses to clients

Internal implementation details must never be exposed through the API.

---

# 10. Security

Observability must never compromise security.

Logs should never contain:

- Passwords
- Magic links
- Passkeys
- Authentication tokens
- Personal secrets

Personally identifiable information should be logged only where operationally necessary and in accordance with applicable regulations.

---

# 11. Production Monitoring

Production monitoring should provide visibility into:

- Platform availability
- Failed requests
- Error trends
- Performance trends
- Resource usage

Monitoring should prioritise actionable alerts over excessive notification volume.

---

# 12. Alerting

Alerts should be generated only for conditions requiring attention.

Examples include:

- Database unavailable
- Elevated error rates
- Storage failures
- Failed deployments
- Authentication failures

Excessive alerting should be avoided to reduce alert fatigue.

---

# 13. Future Evolution

Future observability improvements may include:

- Distributed tracing
- Custom dashboards
- Business metrics
- Community analytics
- Operational reporting

Additional tooling should complement the existing observability strategy rather than replace it.

---

# 14. Design Philosophy

A healthy platform should be understandable without guesswork.

Observability should make the internal behaviour of DiaperScout transparent to developers and operators while remaining invisible to users.

When issues occur, the platform should provide sufficient information to identify the problem quickly and confidently.

Well-designed observability reduces downtime, improves developer productivity and supports the long-term reliability of the DiaperScout platform.