# Security

**Document Status:** Draft  
**Version:** 1.1  
**Owner:** DiaperScout Project  
**Last Updated:** 2026-08-13

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
- Data protection
- Secure media handling
- Rate limiting
- Observability
- Auditability
- Dependency management
- Operational practices

No individual layer should be considered sufficient on its own.

The platform should assume that individual controls can fail and provide additional protections around them.

---

# 3. Authentication

Authentication follows the passwordless strategy defined in the Authentication Strategy document.

Initial methods include:

- Email magic links
- Passkeys (WebAuthn)

Passwords are intentionally excluded from the initial implementation.

Authentication establishes the identity of a User.

It does not determine what that User is allowed to do.

Authentication credentials must be protected throughout their lifecycle.

---

# 4. Authorisation

Authentication identifies a User.

Authorisation determines what that User may do.

Authorisation must be enforced server-side through explicit policies and resource-level checks where appropriate.

Initial privileged responsibilities include:

- Moderator
- Administrator
- Verified Manufacturer

Explorer and Contributor are not authorisation roles.

Community Trust is not an authorisation mechanism.

In particular:

- Community Trust must not automatically grant permissions.
- Contribution volume must not automatically grant privileged access.
- A Verified Manufacturer must not bypass editorial controls.
- An Administrator must not automatically become a Moderator.
- A User must not approve their own submission.

Client-side checks are useful for user experience but are never security controls.

---

# 5. Principle of Least Privilege

Every component should operate with the minimum permissions necessary.

Examples include:

- Database accounts with only required privileges.
- Object storage access limited to required operations.
- Services granted only the permissions they require.
- Administrative capabilities restricted to authorised Users.
- Background processes operating with only the permissions required for their task.

Reducing privileges reduces the potential impact of security issues.

---

# 6. Secure Communication

All communication must occur over HTTPS.

Unencrypted communication is not supported for production application traffic.

TLS termination may be provided by Cloudflare or the selected production hosting infrastructure.

The application must still be configured to operate securely behind the deployment infrastructure.

Communication between platform components should also use secure channels where appropriate.

Certificates and TLS configuration should be maintained through the hosting/deployment environment rather than embedded in application code.

---

# 7. Input Validation

All external input is considered untrusted.

Validation should occur before application workflows execute.

Validation includes:

- Required values
- Data formats
- Length limits
- Range checks
- File types
- File sizes
- Business rule validation

Validation should occur at the appropriate application boundary.

Domain invariants remain the responsibility of the Domain layer.

Validation failures should produce safe, user-friendly responses without exposing internal implementation details.

---

# 8. Output Encoding

User-provided and externally sourced content must be treated as untrusted when rendered.

The application should rely on the framework's normal output encoding mechanisms.

Raw HTML should not be rendered from untrusted content unless it has been deliberately sanitised.

This protects the platform against cross-site scripting and related injection attacks.

---

# 9. Cross-Site Request Forgery

State-changing browser operations must be protected against Cross-Site Request Forgery where the authentication mechanism and request architecture make CSRF applicable.

The application should use the appropriate ASP.NET Core protection mechanisms rather than implementing custom CSRF protection.

API endpoints using bearer-style authentication have different CSRF characteristics from cookie-authenticated browser requests and should be designed accordingly.

---

# 10. Data Protection

Sensitive data should be protected throughout its lifecycle.

Examples include:

- Authentication tokens
- Magic-link tokens
- Session identifiers
- Email addresses
- Account recovery information
- Private User data
- Security/audit information

Only data required by the platform should be collected.

Data retention should be proportionate to operational requirements.

Private data must not accidentally appear in:

- Public API responses
- Search indexes
- Logs
- Analytics
- Client-side state
- Error messages

---

# 11. Personal Data

Personal information should be handled according to the platform's privacy requirements.

The application should minimise collection and exposure of personal information.

Examples include:

- Email addresses
- Account information
- Private Backpack information
- Contribution identity
- Authentication information

Public Explorer identity should be limited to information intentionally exposed by the User and permitted by the product design.

Deleting an account must not automatically destroy historical provenance where that provenance is required to preserve the integrity of the Atlas.

Where historical information must remain, personal identity should be anonymised appropriately.

---

# 12. Secrets Management

Secrets must never be committed to source control.

Examples include:

- API keys
- Database credentials
- Storage credentials
- SMTP credentials
- Authentication secrets
- Encryption keys
- External service credentials

Secrets should be provided through secure configuration mechanisms appropriate to the deployment environment.

Development secrets should use appropriate local secret-management facilities.

Production secrets should be supplied through the hosting/deployment environment or an appropriate secret-management service.

Secrets must not be embedded in:

- source code;
- configuration committed to Git;
- client-side JavaScript;
- PWA assets;
- API responses;
- logs.

---

# 13. Database Security

PostgreSQL should be configured using secure defaults.

Practices include:

- Least-privilege database accounts
- Secure connection configuration
- Entity Framework Core parameterised queries
- Controlled migrations
- Appropriate indexing
- Audited schema changes
- Restricted network access

Direct database access should be restricted to authorised operational personnel.

The application should never expose PostgreSQL directly to clients.

Database credentials must not be available to the Web application or end users.

---

# 14. Database Integrity

Security also depends on maintaining data integrity.

Important invariants should be protected through appropriate combinations of:

- Domain rules
- Application validation
- Database constraints
- Foreign keys
- Unique constraints
- Concurrency controls

Database constraints provide a final protection against invalid persistence.

They do not replace domain-level security or business rules.

---

# 15. Object Storage

Cloudflare R2 is the initial preferred object-storage target for binary assets where appropriate.

Access should be controlled through the application rather than exposing unrestricted storage credentials.

Public media should only be exposed where the Product and media workflow explicitly permits it.

User-submitted media must not automatically become publicly accessible merely because it has been uploaded successfully.

Upload validation should verify:

- File type
- File size
- Supported formats
- Expected content where practical

Where appropriate, uploads should be processed into safe derived representations before public delivery.

Future enhancements may include malware scanning if operational requirements justify it.

---

# 16. File Upload Security

File uploads are treated as untrusted input.

The application must not assume that a file is safe merely because its filename or declared MIME type appears valid.

Where practical, the application should:

- Validate allowed file types.
- Enforce size limits.
- Generate server-side object names.
- Avoid using user-controlled filenames as storage paths.
- Store uploads outside executable application paths.
- Prevent path traversal.
- Process media asynchronously where appropriate.
- Preserve the original Evidence separately from derived representations.

Uploaded files must never be allowed to execute as application code.

---

# 17. Server-Side Request Security

The application should protect against unsafe requests to external resources.

Where the platform retrieves URLs or external resources supplied by Users or third parties, the implementation must consider Server-Side Request Forgery risks.

External resource access should:

- Restrict allowed protocols.
- Validate destinations.
- Avoid unrestricted access to internal network addresses.
- Apply appropriate timeouts.
- Limit response sizes.
- Avoid following arbitrary redirects where unsafe.

External URL retrieval should only exist where a genuine product requirement requires it.

---

# 18. API Security

The API is a security boundary.

The API must:

- Authenticate protected requests.
- Enforce authorisation.
- Validate input.
- Apply rate limits where appropriate.
- Avoid exposing internal exceptions.
- Avoid exposing database models directly.
- Protect private resources.
- Return only data the requesting User is permitted to access.

The API must remain secure even when requests are made outside the normal Web application.

A malicious client must not be able to bypass security by directly calling an endpoint.

---

# 19. Resource Ownership

User-owned resources must be protected through server-side ownership checks.

Examples include:

- Backpack data
- Collections
- Scrapbook content
- Private Observation drafts
- Account information
- Private media

The server must establish ownership from the authenticated User identity.

A User-supplied identifier must never be treated as proof of ownership.

---

# 20. Rate Limiting

Security-sensitive and abuse-prone operations should be rate limited.

Examples include:

- Magic-link requests
- Authentication attempts
- Account recovery
- Account linking
- API requests
- File uploads
- Search where abuse is possible
- Privileged operations

Rate limits should balance abuse prevention with legitimate use.

Limits should be applied at the appropriate infrastructure and application boundaries.

---

# 21. Account Enumeration

Authentication and account-related endpoints should minimise information disclosure.

The platform should avoid revealing whether a particular email address belongs to an existing User where doing so is unnecessary.

For example, requesting a magic link for an unknown email address should not provide a materially different public response from requesting one for a registered address.

This reduces account enumeration risk.

---

# 22. Session Security

Authenticated sessions should:

- Use secure transport.
- Use appropriately protected cookies or tokens.
- Use appropriate expiration.
- Support sign out.
- Support revocation where required.
- Avoid unnecessary session lifetime.
- Protect against session fixation.
- Avoid storing sensitive authentication state in client-controlled storage.

Privileged access must not remain active indefinitely after authorisation has been revoked.

---

# 23. Authentication Credential Security

Authentication credentials must never be exposed through:

- Logs
- API responses
- Client-side diagnostics
- Error messages
- Analytics
- Support tooling

Magic-link tokens should:

- Be cryptographically random.
- Be time-limited.
- Be single-use.
- Resist replay.

Passkeys must use the security guarantees provided by WebAuthn and the selected authentication framework.

---

# 24. Error Handling

Errors should fail safely.

Users should receive useful information without receiving:

- Stack traces
- Database details
- Connection strings
- Internal paths
- Provider credentials
- Authentication details
- Implementation-specific diagnostics

Detailed diagnostic information should remain available through controlled server-side logging.

---

# 25. Logging

Logs should support operational diagnosis without exposing sensitive information.

Logs must never include:

- Authentication tokens
- Passkeys
- Magic links
- Secrets
- Passwords
- Database credentials
- Private authentication material

Personally identifiable information should be logged only where operationally necessary.

Logs should use appropriate severity levels and provide enough context to investigate failures.

---

# 26. Audit Logging

Security-sensitive actions should be auditable.

Examples include:

- Role granted
- Role revoked
- Manufacturer verification
- Editorial decisions
- Privileged administrative actions
- Authentication events
- Account recovery
- Authentication-method changes
- Significant security configuration changes

Audit records should preserve sufficient information for investigation without unnecessarily storing personal information.

Audit data must itself be protected from unauthorised modification.

---

# 27. Dependency Security

Third-party libraries should be selected carefully.

Preference should be given to:

- Mature libraries
- Actively maintained projects
- Well-documented software
- Libraries with appropriate security practices

Unused dependencies should be removed.

Dependencies should be kept reasonably up to date to receive security fixes.

Security advisories should be reviewed as part of normal maintenance.

---

# 28. Secure Development

Security should be considered during everyday development.

Developers should:

- Review changes.
- Validate input.
- Handle errors safely.
- Follow established coding standards.
- Keep dependencies updated.
- Avoid unnecessary exposure of personal information.
- Consider abuse cases as well as normal use cases.
- Test security boundaries.

Security reviews should form part of significant architectural changes.

---

# 29. Source Control Security

Source control must not contain:

- Secrets
- Production credentials
- Private authentication keys
- User-submitted media
- Production database dumps
- Personal information

Repository access should be restricted to authorised contributors.

Changes should be reviewed before being merged into protected production branches.

---

# 30. Client Security

The Web application and PWA are considered untrusted clients.

The client may:

- Request information.
- Present information.
- Maintain temporary UI state.
- Prepare offline drafts.

The client must not be trusted to:

- Authorise operations.
- Validate permissions.
- Protect canonical data.
- Establish User identity.
- Guarantee submission integrity.

All important security decisions occur server-side.

---

# 31. Offline Security

Offline functionality must not bypass server-side security.

Local application state is not proof of authentication.

When protected work is synchronised:

1. The server establishes the User identity.
2. The server validates the request.
3. The server performs authorisation.
4. The server validates the domain operation.
5. The server persists the result.

The client must not claim that protected work has been submitted until server confirmation is received.

---

# 32. Community Content Security

Community-submitted information must be treated as untrusted until reviewed.

An Observation does not automatically become canonical Atlas information.

The security and integrity boundary is:

```text
Explorer
   ↓
Observation
   ↓
Evidence
   ↓
Editorial Review
   ↓
Editorial Decision
   ↓
Atlas
```

A Contributor cannot directly modify canonical Atlas data through the contribution system.

---

# 33. Editorial Security

Editorial operations require explicit Moderator authority.

The system must prevent:

- Unauthorised editorial actions.
- Self-approval.
- Bypassing the editorial workflow.
- Direct modification of canonical data through community endpoints.
- Concurrent decisions silently overwriting one another.

Editorial actions should be auditable.

---

# 34. Administrator Security

Administrator access represents a high-privilege security boundary.

Administrators should receive only the capabilities required for their operational responsibilities.

Administrator access does not automatically grant editorial authority.

Where a User legitimately requires both responsibilities, both should be explicitly authorised.

Administrative actions should be auditable.

---

# 35. Verified Manufacturer Security

Verified Manufacturer access must be explicitly established and revocable.

Verification should establish sufficient confidence in:

- The organisation.
- The representative.
- The verification process.
- The current verification state.

Verified Manufacturers may submit official information but must not:

- Directly modify canonical Atlas data.
- Bypass editorial review.
- Approve their own submissions.
- Modify community observations.

---

# 36. Background Processing Security

Background services must operate using appropriate service identities and permissions.

They must not assume that a User who initiated a job remains authorised indefinitely.

Background processing should:

- Validate queued work.
- Use least privilege.
- Avoid exposing sensitive data in logs.
- Be resilient to duplicate execution.
- Maintain appropriate audit information.

Long-running operations should not retain unnecessary User credentials.

---

# 37. External Services

External services should be treated as separate trust boundaries.

Examples include:

- Object storage
- Email providers
- Search services
- Geolocation providers
- Other third-party APIs

The application should:

- Minimise information sent externally.
- Validate responses.
- Apply timeouts.
- Handle provider failures safely.
- Avoid trusting external data without validation.
- Keep provider credentials secret.

External provider failures must not compromise the integrity of the Atlas.

---

# 38. Security Headers

The production Web application should use appropriate HTTP security headers.

Depending on the deployment architecture, these may include:

- Content-Security-Policy
- Strict-Transport-Security
- X-Content-Type-Options
- Referrer-Policy
- Permissions-Policy
- Appropriate framing protection

Headers should be configured deliberately rather than copied blindly from generic templates.

The policy must remain compatible with the actual Web application and PWA requirements.

---

# 39. Content Security Policy

Where practical, a Content Security Policy should restrict the sources from which the browser may load:

- Scripts
- Styles
- Images
- Fonts
- Frames
- Connections

The policy should be developed alongside the Web application rather than added after the application has accumulated uncontrolled external dependencies.

---

# 40. Security Monitoring

The production environment should provide sufficient monitoring to identify unusual or potentially malicious activity.

Examples include:

- Authentication failures
- Excessive magic-link requests
- Unusual API activity
- Repeated authorisation failures
- Large or unusual upload activity
- Privileged actions
- Application errors

Monitoring should avoid creating unnecessary personal-data retention.

---

# 41. Incident Response

The platform should support rapid investigation of security issues.

Important capabilities include:

- Structured logging
- Audit trails
- Health monitoring
- Deployment history
- Authentication event history
- Role-change history
- Relevant infrastructure logs

The objective is to minimise the time required to detect, understand and resolve incidents.

Security incidents should be assessed for:

- Scope
- Affected Users
- Affected data
- Persistence
- Required containment
- Required remediation
- Required notification

---

# 42. Backup and Recovery Security

Backups should be protected with the same seriousness as production data.

Backups should:

- Be access-controlled.
- Use appropriate encryption.
- Have defined retention.
- Be protected from accidental deletion.
- Be tested periodically for restoration.

Backup credentials must not be shared with ordinary application processes unless required.

A backup that cannot be restored should not be considered a reliable recovery mechanism.

---

# 43. Deployment Security

Production deployment should follow secure operational practices.

These include:

- Protected deployment credentials.
- Restricted production access.
- Controlled configuration.
- Secure secret injection.
- Auditable deployments.
- Dependency scanning.
- Reproducible builds where practical.
- Appropriate environment separation.

Development and test environments must not have unnecessary access to production resources.

---

# 44. Security Testing

Security-sensitive functionality should receive dedicated automated testing.

Testing should cover:

- Authentication
- Authorisation
- Session management
- Account recovery
- Resource ownership
- Input validation
- File uploads
- API access
- Rate limiting
- Enumeration protection
- Privileged operations
- Self-approval prevention

Tests should verify that security boundaries hold when normal UI behaviour is bypassed.

The testing strategy is defined in the Testing Strategy document.

---

# 45. Security Reviews

Security review should be performed when introducing significant changes such as:

- New authentication mechanisms
- New privileged roles
- New external services
- New file types
- New media-processing pipelines
- New public APIs
- New personal-data processing
- Significant database changes
- New client-side capabilities

The level of review should be proportional to the risk introduced.

---

# 46. Future Evolution

Future security enhancements may include:

- Additional identity providers
- Enhanced device management
- Automated security scanning
- Malware scanning
- More advanced abuse detection
- Periodic penetration testing
- Automated dependency monitoring
- Enhanced security analytics

Security practices should evolve alongside the platform without introducing unnecessary complexity.

---

# 47. Design Philosophy

Security should enable trust without creating unnecessary friction.

The platform should remain approachable for Explorers while maintaining appropriate protections for:

- Community content
- Personal data
- Authentication
- Editorial integrity
- Canonical Atlas information
- Operational infrastructure

Well-designed security is largely invisible to Users, allowing them to focus on exploring and contributing with confidence.

---

# 48. Relationship to Other Documents

This document defines the cross-cutting security principles for the production platform.

Related documents include:

- **Authentication Strategy** — defines identity and authentication mechanisms.
- **Authorization** — defines permission and policy implementation.
- **Data Access Strategy** — defines database access and persistence boundaries.
- **Testing Strategy** — defines security testing requirements.
- **Coding Standards** — defines secure implementation conventions.
- **Solution Structure** — defines architectural boundaries.
- **Project Layout** — defines the physical implementation structure.
- **Workflow Architecture** — defines workflow and editorial state transitions.
- **Privacy documentation** — defines personal-data handling requirements where applicable.

---

# 49. Summary

DiaperScout security is based on defence in depth:

```text
Authentication
      ↓
Authorisation
      ↓
Input Validation
      ↓
Domain Integrity
      ↓
Secure Persistence
      ↓
Protected Media
      ↓
Auditability
      ↓
Monitoring
      ↓
Operational Security
```

The platform assumes that clients, submitted content and external services are untrusted.

Security decisions remain server-side.

Community Trust does not grant permissions.

Contributors do not directly modify canonical Atlas information.

Administrators and Moderators remain separate responsibilities.

Verified Manufacturers remain subject to editorial controls.

The objective is a platform that is secure by design while remaining approachable and enjoyable to use.