# Configuration

**Document Status:** Draft  
**Version:** 1.1  
**Owner:** DiaperScout Project  
**Last Updated:** 2026-08-13

---

# 1. Purpose

This document defines how configuration is managed throughout the DiaperScout platform.

Configuration allows the behaviour of the application and its infrastructure to vary between environments without requiring code changes.

The objective is to provide a configuration strategy that is:

- Consistent
- Secure
- Maintainable
- Predictable
- Environment-aware
- Easy to validate

---

# 2. Philosophy

Configuration should describe **how the application operates**, not **how the application is implemented**.

Business logic must not depend directly on configuration files or environment variables.

Configuration should be supplied through the .NET configuration system and exposed to application components through Dependency Injection.

Infrastructure-specific configuration should remain within the appropriate Infrastructure or hosting boundary.

---

# 3. Guiding Principles

Configuration should follow these principles:

- Keep configuration external to application code.
- Never hardcode environment-specific values.
- Never store secrets in source control.
- Prefer strongly typed configuration.
- Validate critical configuration during startup.
- Fail fast when required configuration is missing or invalid.
- Keep configuration grouped by responsibility.
- Avoid unnecessary configuration.
- Keep environment differences explicit.
- Do not allow configuration to become a hidden business-rule mechanism.

---

# 4. Configuration Sources

The platform follows the standard ASP.NET Core configuration hierarchy.

Typical sources include:

1. Application defaults
2. `appsettings.json`
3. Environment-specific configuration
4. Environment variables
5. Secret storage
6. Command-line overrides where appropriate

Later sources override earlier sources according to the standard .NET configuration rules.

Production deployments should favour environment-provided configuration and secure secret storage rather than modifying application files.

---

# 5. Environments

The platform recognises multiple execution environments.

Typical environments include:

- Development
- Test
- Staging
- Production

Configuration should adapt to the current environment.

Application code should not require modification when moving between environments.

Production configuration must remain isolated from development and test configuration.

---

# 6. Strongly Typed Configuration

Configuration should normally be represented by strongly typed C# classes using the .NET Options pattern.

Examples include:

```text
DatabaseOptions
StorageOptions
AuthenticationOptions
EmailOptions
SearchOptions
ObservabilityOptions
```

Strongly typed options provide:

- Discoverability
- Validation
- Consistent dependency injection
- Clear ownership
- Reduced configuration-string usage

Developers should avoid repeatedly reading arbitrary configuration keys throughout the application.

---

# 7. Configuration Ownership

Configuration should belong to the layer that owns the relevant behaviour.

For example:

```text
Database
   ↓
Infrastructure

Authentication
   ↓
Authentication / Infrastructure

Storage
   ↓
Infrastructure

Observability
   ↓
Application / Hosting

Business Rules
   ↓
Domain
```

The Domain must not become dependent on infrastructure configuration.

The Application layer should only consume configuration where the use case genuinely requires it.

---

# 8. Validation

Critical configuration should be validated during application startup.

Invalid configuration should normally prevent the affected service from becoming ready.

Examples include:

- Missing database connection information
- Invalid database settings
- Missing storage credentials
- Invalid authentication settings
- Invalid external-service configuration

Failing early is preferable to discovering configuration problems during a User request.

---

# 9. Configuration Errors

Configuration failures should be:

- Clearly reported in server-side diagnostics
- Safe for Users
- Observable through application telemetry
- Prevented from exposing secrets

Configuration errors must not be returned to clients with:

- Connection strings
- Credentials
- Secret values
- Internal file paths
- Infrastructure details

---

# 10. Secrets

Sensitive information must never be committed to source control.

Examples include:

- Database passwords
- API keys
- SMTP credentials
- R2 credentials
- Authentication secrets
- Encryption keys
- External service credentials

Secrets should be supplied through secure mechanisms appropriate to each environment.

Development may use local developer secret storage.

Production should use the hosting environment or an appropriate secret-management service.

---

# 11. Secret Separation

Secrets should remain separate from ordinary application configuration wherever practical.

For example:

```text
Configuration
    ↓
Database host
Database name
Timeouts
Feature settings

Secrets
    ↓
Database password
Storage credentials
Authentication secrets
API keys
```

Configuration files may safely contain non-sensitive defaults.

Secret values must not be embedded in committed configuration files.

---

# 12. Environment Variables

Environment variables may be used to override configuration in deployment environments.

They are particularly appropriate for:

- Deployment-specific settings
- Container configuration
- Hosting configuration
- Secret injection where supported

Environment variables must not be treated as inherently secure.

Sensitive values supplied through environment variables must still be protected by the hosting environment.

---

# 13. Database Configuration

Database configuration should include only values required to establish and operate the database connection.

Typical settings include:

- Connection string or connection components
- Command timeout
- Retry configuration
- Connection-pool configuration where required

Entity Framework Core configuration remains within the Infrastructure layer.

Database credentials must be supplied through secure secret management.

---

# 14. PostgreSQL

PostgreSQL is the authoritative relational database for the platform.

Configuration should support:

- Development PostgreSQL
- Test PostgreSQL
- Staging PostgreSQL
- Production PostgreSQL

The application must not require PostgreSQL configuration to be embedded in Domain or Application business logic.

Database-specific configuration belongs within the persistence/infrastructure boundary.

---

# 15. Object Storage Configuration

Cloudflare R2 is the initial object-storage target for production media.

Storage configuration may include:

- Endpoint
- Bucket
- Region where applicable
- Access credentials
- Public delivery configuration where applicable

Credentials must remain secret.

The Domain and Application layers should not depend directly on Cloudflare-specific configuration.

---

# 16. CDN and Edge Configuration

Cloudflare may provide:

- DNS
- HTTPS
- CDN
- Edge caching
- Public media delivery

These are primarily deployment/infrastructure concerns rather than application configuration.

Where application behaviour genuinely depends on an edge setting, that dependency should be explicitly represented rather than inferred from deployment behaviour.

---

# 17. Authentication Configuration

Authentication configuration should be grouped separately from general application configuration.

Examples include:

- Magic-link expiry
- Session settings
- Passkey/WebAuthn settings
- Authentication provider configuration
- Email delivery configuration
- Account recovery settings

Authentication secrets must be provided through secure secret management.

Authentication configuration must not be exposed to the client unless a value is explicitly intended to be public.

---

# 18. Email Configuration

Where email delivery is required, configuration should include the non-secret settings necessary to operate the service.

Examples include:

- Provider
- Sender address
- Sender name
- Endpoint
- Timeout

Credentials and API keys must be supplied through secure secret management.

Development environments should use an appropriate test or development mail mechanism rather than accidentally sending production email.

---

# 19. Search Configuration

Search configuration should be grouped under the search responsibility.

Examples include:

- Search provider
- Index configuration
- Query limits
- Search tuning
- Indexing behaviour

The initial search implementation is based on PostgreSQL capabilities.

If dedicated search infrastructure is introduced later, the configuration boundary should allow that change without requiring Domain changes.

---

# 20. Media Processing Configuration

Media-processing settings may include:

- Supported formats
- Maximum upload size
- Thumbnail dimensions
- Derived image formats
- Processing limits
- Queue configuration

Limits should be enforced server-side.

Media configuration must not allow a User to bypass security or resource protections.

---

# 21. Observability Configuration

Observability configuration may include:

- Log levels
- Telemetry exporters
- Trace sampling
- Metrics configuration
- Health-check configuration

Development environments may use more verbose diagnostics.

Production should favour useful, privacy-conscious telemetry.

Sensitive data must never be enabled merely for diagnostic convenience.

---

# 22. Rate-Limiting Configuration

Where rate limiting is configurable, limits should be grouped by responsibility.

Examples include:

- Authentication requests
- Magic-link requests
- API requests
- File uploads
- Other abuse-sensitive operations

Rate limits should have safe defaults.

Changes to production limits should be deliberate and observable.

---

# 23. Feature Configuration

Feature-specific configuration should remain grouped by responsibility.

Examples include:

- Authentication
- Search
- Media processing
- Email
- Observability
- Background processing

Avoid creating a single large configuration section containing unrelated settings.

---

# 24. Feature Flags

Feature flags may be introduced where they provide a genuine deployment or rollout benefit.

They should not become a substitute for normal configuration.

Feature flags should have:

- Clear ownership
- Defined purpose
- Safe defaults
- Appropriate environment behaviour
- A plan for eventual removal

Temporary flags should not become permanent hidden configuration.

---

# 25. Local Development

.NET Aspire provides the primary orchestration experience for local development.

Aspire may provide or coordinate:

- PostgreSQL
- Application services
- Supporting infrastructure
- Service discovery
- Local observability

Aspire is an orchestration and development experience, not a replacement for the application's normal configuration model.

Developers should be able to clone the repository, configure required local secrets and run the platform with minimal manual setup.

---

# 26. Test Configuration

Test environments should use isolated configuration.

Tests must not depend on:

- Developer-specific configuration
- Production credentials
- Production databases
- Production object storage
- Developer machine state

Integration tests should provision or reference appropriate isolated infrastructure.

Test configuration should be deterministic wherever practical.

---

# 27. Staging Configuration

Staging should resemble production where practical.

Important differences should be limited to environment-specific values such as:

- Database
- Storage
- Authentication providers
- External service credentials
- Resource sizing
- Telemetry destinations

Staging must never accidentally connect to production databases or storage.

---

# 28. Production Configuration

Production configuration should be managed through controlled deployment processes.

Production configuration should include:

- Database connection
- Object-storage configuration
- Authentication configuration
- Email configuration
- Observability configuration
- Rate limits
- External service configuration

Production secrets must be supplied through secure secret-management mechanisms.

Production configuration changes should be auditable.

---

# 29. Configuration and Deployment

Configuration changes should normally be deployed through the same controlled processes as application changes.

Where a configuration change can materially affect:

- Security
- Availability
- Performance
- Data integrity

it should be reviewed and tested appropriately.

Configuration should not become a hidden mechanism for making undocumented production changes.

---

# 30. Configuration and Security

Configuration must follow the Security strategy.

In particular:

- Secrets must remain protected.
- Production configuration must be access-controlled.
- Sensitive configuration must not be logged.
- Client applications must not receive server secrets.
- Configuration must not bypass authorisation.
- Configuration must not weaken domain invariants.

Configuration should be considered part of the production security boundary.

---

# 31. Configuration and Observability

Configuration should support the observability strategy without exposing secrets.

Useful operational information may include:

- Environment
- Application version
- Enabled non-sensitive capabilities
- Telemetry configuration
- Service endpoints

Secrets and private credentials must never appear in telemetry.

Where configuration materially affects performance or behaviour, the effective non-sensitive configuration should be identifiable during operational diagnosis.

---

# 32. Configuration and Testing

Important configuration behaviour should be tested.

Tests should verify:

- Required configuration is validated.
- Invalid configuration fails appropriately.
- Environment-specific configuration is loaded correctly.
- Secrets are not exposed.
- Default values behave as expected.
- Configuration changes do not silently bypass security.

Configuration tests should focus on behaviour rather than testing the .NET configuration framework itself.

---

# 33. Defaults

Safe defaults should be provided wherever practical.

Defaults should:

- Be suitable for development where appropriate.
- Avoid unsafe production assumptions.
- Reduce unnecessary setup.
- Remain explicit.

Security-sensitive settings should fail closed rather than silently selecting an unsafe default.

---

# 34. Configuration Naming

Configuration names should be:

- Descriptive
- Consistent
- Grouped by responsibility
- Stable

Prefer:

```text
Authentication:MagicLink:ExpiryMinutes
```

over:

```text
MagicTime
```

Names should communicate what the setting controls.

---

# 35. Avoiding Configuration Sprawl

Not every value should become configuration.

A value should normally become configurable when:

- It legitimately varies between environments.
- Operations may need to tune it.
- It represents an external dependency.
- It represents a deployment concern.
- It represents a safe operational policy.

Values that are intrinsic to the Domain model should remain in code.

Configuration should not become a way to avoid making architectural decisions.

---

# 36. Runtime Configuration Changes

The initial platform should favour configuration changes that take effect through deployment/restart rather than requiring a complex dynamic configuration system.

Dynamic configuration may be introduced later where a genuine operational requirement exists.

Any future dynamic configuration system must consider:

- Validation
- Auditability
- Concurrency
- Security
- Rollback
- Observability

---

# 37. Configuration Documentation

Important configuration sections should be documented close to their implementation.

Documentation should identify:

- Purpose
- Expected value
- Whether the setting is secret
- Default behaviour
- Environment differences
- Operational impact

Secrets themselves must never be documented as literal values.

---

# 38. Configuration Recovery

Production configuration must be recoverable following infrastructure failure.

Recovery must include access to:

- Infrastructure configuration
- Application configuration
- Secret-management systems
- Database configuration
- Object-storage configuration
- Authentication configuration
- Deployment configuration

Recovery procedures are defined in the Backup and Recovery and Deployment Strategy documents.

---

# 39. Future Evolution

Future configuration may include:

- Feature flags
- Operational tuning
- Regional configuration
- Additional external service providers
- Advanced scaling settings
- Runtime service configuration

New configuration should remain consistent with the existing strongly typed and environment-aware approach.

Additional configuration systems should only be introduced where their operational value justifies the added complexity.

---

# 40. Design Philosophy

Configuration should enable flexibility without becoming hidden complexity.

Developers should rarely need to ask:

> "Where does this value come from?"

The answer should be clear from:

- The configuration structure
- The strongly typed options
- The environment
- The deployment configuration
- The secret-management boundary

The guiding principle is:

> **Configuration changes how the platform operates; it must never secretly change what the platform means.**