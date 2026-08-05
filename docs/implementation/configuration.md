# Configuration

**Document Status:** Draft  
**Version:** 1.0  
**Owner:** DiaperScout Project  
**Last Updated:** 2026-08-02

---

# 1. Purpose

This document defines how configuration is managed throughout the DiaperScout platform.

Configuration allows the behaviour of the application to vary between environments without requiring code changes.

The objective is to provide a configuration strategy that is:

- Consistent
- Secure
- Maintainable
- Predictable
- Environment-aware

---

# 2. Philosophy

Configuration should describe **how the application operates**, not **how the application is implemented**.

Business logic should never depend upon configuration files directly.

Configuration should be supplied through the .NET configuration system using Dependency Injection.

---

# 3. Guiding Principles

Configuration should follow these principles:

- Keep configuration external to the application.
- Never hardcode environment-specific values.
- Never store secrets in source control.
- Validate configuration during application startup.
- Fail fast when required configuration is missing.
- Keep configuration simple and discoverable.

---

# 4. Configuration Sources

The platform follows the standard ASP.NET Core configuration hierarchy.

Typical sources include:

1. Application defaults
2. Environment-specific configuration
3. Environment variables
4. Secret storage
5. Command-line overrides (where appropriate)

Later sources override earlier ones.

---

# 5. Environments

The platform recognises multiple execution environments.

Typical environments include:

- Development
- Test
- Staging
- Production

Configuration should adapt automatically to the current environment.

Application code should not require modification when moving between environments.

---

# 6. Strongly Typed Configuration

Configuration should be represented by strongly typed C# classes.

Examples include:

- DatabaseSettings
- StorageSettings
- EmailSettings
- AuthenticationSettings

This improves discoverability, validation and compile-time safety.

Developers should avoid reading arbitrary configuration values throughout the application.

---

# 7. Validation

Configuration should be validated during application startup.

Critical configuration should prevent application startup if invalid.

Examples include:

- Missing database connection strings
- Missing storage credentials
- Invalid authentication settings

Early failure is preferred over runtime surprises.

---

# 8. Secrets

Sensitive information must never be committed to source control.

Examples include:

- Database passwords
- API keys
- SMTP credentials
- Cloudflare credentials
- Authentication secrets

Secrets should be provided through secure mechanisms appropriate to each environment.

---

# 9. Database Configuration

Database configuration should include:

- Connection string
- Command timeout
- Retry behaviour

Entity Framework Core configuration should remain within the Infrastructure layer.

---

# 10. Cloudflare Configuration

Configuration for Cloudflare services should include:

- R2 credentials
- CDN configuration
- DNS settings (where applicable)

Application code should remain independent of deployment-specific values.

---

# 11. Feature Configuration

Feature-specific configuration should remain grouped by responsibility.

Examples include:

- Authentication
- Search
- Image processing
- Email
- Observability

Avoid large, monolithic configuration sections.

---

# 12. Local Development

.NET Aspire provides local configuration for development.

Developers should be able to clone the repository, configure local secrets and run the platform with minimal manual setup.

The development environment should closely resemble production where practical.

---

# 13. Future Evolution

Future configuration may include:

- Feature flags
- Operational tuning
- Regional configuration
- External service providers

New configuration should remain consistent with the existing structure.

---

# 14. Design Philosophy

Configuration should enable flexibility without introducing complexity.

Developers should rarely need to ask:

> "Where does this value come from?"

Configuration should be organised, validated and documented so that environments behave predictably while allowing the platform to evolve safely over time.