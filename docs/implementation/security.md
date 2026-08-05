# Security

**Document Status:** Draft  
**Version:** 1.0  
**Owner:** DiaperScout Project  
**Last Updated:** 2026-08-02

---

# 1. Purpose

This document defines the security principles and practices used throughout the DiaperScout platform.

Security is considered a fundamental quality of the platform rather than a feature added after implementation.

Every component should be designed with security in mind from the outset.

---

# 2. Security Philosophy

DiaperScout adopts a defence-in-depth approach.

Security is achieved through multiple complementary layers rather than relying on any single mechanism.

These layers include:

- Secure authentication
- Strong authorisation
- Input validation
- Secure communication
- Safe defaults
- Observability
- Operational practices

No individual layer should be considered sufficient on its own.

---

# 3. Authentication

Authentication follows the passwordless strategy defined in the Authentication Strategy document.

Initial methods include:

- Email magic links
- Passkeys (WebAuthn)

Passwords are intentionally excluded from the initial implementation.

---

# 4. Authorisation

Authentication identifies a user.

Authorisation determines what that user may do.

Access to privileged operations should be based upon roles and permissions rather than client-side checks.

Typical roles include:

- Anonymous
- Scout
- Moderator
- Administrator

Authorisation decisions belong within the application layer.

---

# 5. Principle of Least Privilege

Every component should operate with the minimum permissions necessary.

Examples include:

- Database accounts with only required privileges.
- Object storage access limited to required operations.
- Services granted only the permissions they require.

Reducing privileges reduces the potential impact of security issues.

---

# 6. Secure Communication

All communication must occur over HTTPS.

Unencrypted communication is not supported.

Cloudflare is responsible for providing TLS termination and secure edge connectivity.

Communication between platform components should also use secure channels where appropriate.

---

# 7. Input Validation

All external input is considered untrusted.

Validation should occur before application workflows execute.

Validation includes:

- Required values
- Data formats
- Length limits
- Range checks
- Business rule validation

Validation failures should produce safe, user-friendly responses.

---

# 8. Data Protection

Sensitive data should be protected throughout its lifecycle.

Examples include:

- Authentication tokens
- Email addresses
- Session identifiers

Only data required by the platform should be collected.

Data retention should be proportionate to operational requirements.

---

# 9. Secrets Management

Secrets must never be committed to source control.

Examples include:

- API keys
- Database credentials
- Storage credentials
- SMTP credentials

Secrets should be provided through secure configuration mechanisms appropriate to the deployment environment.

---

# 10. Database Security

PostgreSQL should be configured using secure defaults.

Practices include:

- Least-privilege database accounts
- Entity Framework Core parameterised queries
- Controlled migrations
- Appropriate indexing
- Audited schema changes

Direct database access should be restricted to authorised operational personnel.

---

# 11. Object Storage

Cloudflare R2 stores binary assets.

Access should be controlled through the application rather than exposing unrestricted storage access.

Public image delivery should be limited to approved assets.

Upload validation should verify:

- File type
- File size
- Supported formats

Future enhancements may include malware scanning if operational requirements justify it.

---

# 12. Logging

Logs should support operational diagnosis without exposing sensitive information.

Logs must never include:

- Authentication tokens
- Passkeys
- Magic links
- Secrets
- Credentials

Personally identifiable information should be logged only where operationally necessary.

---

# 13. Dependencies

Third-party libraries should be selected carefully.

Preference should be given to:

- Mature libraries
- Actively maintained projects
- Well-documented software

Unused dependencies should be removed.

Dependencies should be kept reasonably up to date to receive security fixes.

---

# 14. Secure Development

Security should be considered during everyday development.

Developers should:

- Review changes
- Validate input
- Handle errors safely
- Follow established coding standards
- Keep dependencies updated

Security reviews should form part of significant architectural changes.

---

# 15. Incident Response

The platform should support rapid investigation of security issues.

Important capabilities include:

- Structured logging
- Audit trails
- Health monitoring
- Deployment history

The objective is to minimise the time required to detect, understand and resolve incidents.

---

# 16. Future Evolution

Future security enhancements may include:

- Additional identity providers
- Trusted device management
- Enhanced moderation tooling
- Automated security scanning
- Periodic security reviews

Security practices should evolve alongside the platform without introducing unnecessary complexity.

---

# 17. Design Philosophy

Security should enable trust without creating unnecessary friction.

The platform should remain approachable for users while maintaining appropriate protections for community content, personal data and operational infrastructure.

Well-designed security is largely invisible to users, allowing them to focus on contributing to the DiaperScout community with confidence.