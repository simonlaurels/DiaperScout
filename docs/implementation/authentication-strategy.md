# Authentication Strategy

**Document Status:** Production Architecture
**Version:** 2.0
**Owner:** DiaperScout Project

---

# 1. Purpose

This document defines the authentication strategy for the DiaperScout platform.

Authentication answers a single question:

> **Who is this User?**

Authentication establishes identity.

Authorisation determines what that identity is permitted to do.

These concerns remain deliberately separate.

---

# 2. Design Goals

The authentication experience should be:

* Secure
* Simple
* Passwordless
* Mobile-friendly
* Low friction
* Accessible
* Future-proof

The objective is to protect the community and the Atlas without making authentication an unnecessary barrier to exploration.

---

# 3. Authentication Philosophy

DiaperScout is an exploration-first community platform.

People should be able to discover the Atlas before deciding whether they want to participate.

Authentication should therefore interrupt the experience as little as possible.

The fundamental principle is:

> **Accounts enable contribution, not exploration.**

Anonymous users can browse the published Atlas.

Authentication becomes necessary when a User wishes to perform an action that requires an identified account.

---

# 4. Anonymous Access

Anonymous users may access public Atlas information.

Examples include:

* Search Products
* Browse Products
* Explore Locations
* Browse Manufacturers
* Browse Brands
* View published Product Specifications
* View public availability information
* View appropriate published Observations
* Explore the Atlas

No account is required for ordinary read-only exploration.

The exact public/private boundary is defined by the relevant feature specification.

---

# 5. Authenticated Access

Authentication is required for actions that create or modify User-associated information or require an identified contributor.

Examples include:

* Creating an Observation
* Uploading Evidence
* Uploading Observation Media
* Saving Products
* Saving Locations
* Creating Collections
* Maintaining a Backpack
* Participating in Discovery Tasks
* Managing personal account information
* Managing personal drafts

Authentication is also required for privileged responsibilities such as:

* Editorial review
* Moderation
* Administration
* Verified Manufacturer submissions

Authorisation determines whether the authenticated User may perform each privileged action.

---

# 6. Canonical Product Creation

Ordinary Users do not directly create canonical Products.

An authenticated Explorer may report a previously unknown Product through an Observation.

The resulting information follows the normal editorial workflow:

```text
Explorer
   ↓
Observation
   ↓
Evidence
   ↓
Editorial Review
   ↓
Product Created or Matched
   ↓
Atlas
```

Authentication therefore enables the **submission of a Product discovery**, not direct creation of canonical Atlas data.

---

# 7. Initial Authentication Methods

The initial production release supports two authentication methods:

1. Email Magic Links
2. Passkeys (WebAuthn)

Both authenticate the same DiaperScout User identity.

---

# 8. Email Magic Links

Users may authenticate by entering their email address.

A secure, time-limited sign-in link is delivered to that address.

Selecting the link authenticates the User.

No password is required.

Magic links should:

* expire after a short period;
* be single-use;
* resist replay;
* use secure random tokens;
* avoid exposing whether an email address belongs to an existing account where appropriate.

---

# 9. Passkeys

Users may register one or more passkeys using WebAuthn.

Supported authenticators may include:

* Face ID
* Touch ID
* Windows Hello
* Android platform authenticators
* Hardware security keys

Passkeys provide phishing-resistant passwordless authentication.

A User may register multiple passkeys to support:

* multiple devices;
* device replacement;
* recovery;
* hardware security keys.

---

# 10. Password Policy

The initial production platform does not support passwords.

This removes the need for:

* Password creation
* Password resets
* Password complexity requirements
* Password expiry
* Password reuse controls
* Password breach monitoring

Passwordless authentication is the preferred experience.

If password authentication is ever introduced, it requires an explicit architectural decision.

---

# 11. Future Identity Providers

Additional identity providers may be introduced in future releases.

Potential providers include:

* Sign in with Apple
* Sign in with Google
* other standards-based identity providers

Additional providers must authenticate the same underlying DiaperScout User identity.

Introducing a provider must not require an existing User to create a duplicate account.

---

# 12. Linked Authentication Methods

A single DiaperScout User may have multiple authentication methods.

For example:

```text
User
 ├── Email Magic Link
 ├── Passkey — Phone
 ├── Passkey — Laptop
 └── Future Provider
```

Authentication methods are credentials for the same User.

They are not separate accounts.

---

# 13. Account Identity

The authenticated technical identity is represented by the `User`.

The User has an Explorer identity within the application.

```text
Authentication
      ↓
User
      ↓
Explorer
```

Explorer is a user-facing concept.

It is not an authentication role.

---

# 14. Contributor Status

Contributor is not an authentication role.

A User becomes a Contributor through submitting information to DiaperScout.

For example:

```text
User
  ↓
Observation
  ↓
Contributor history
```

Contribution history is domain information.

It does not change the User's authentication method or automatically grant additional permissions.

---

# 15. Community Trust

Community Trust is separate from authentication.

It is an internal signal representing demonstrated reliability as a Contributor.

It must not be stored as an authentication role or privileged claim.

For example:

```text
User
 ├── Authentication Methods
 ├── Explorer Profile
 ├── Contribution History
 └── Community Trust
```

Community Trust does not authenticate the User.

Community Trust does not grant Moderator or Administrator access.

---

# 16. Authorisation

Authentication and authorisation remain separate.

Authentication establishes:

> **Who are you?**

Authorisation establishes:

> **What are you allowed to do?**

The principal privileged responsibilities are:

| Responsibility        | Purpose                             |
| --------------------- | ----------------------------------- |
| Moderator             | Editorial responsibility            |
| Administrator         | Platform responsibility             |
| Verified Manufacturer | Authorised manufacturer submissions |

Explorer and Contributor are not authorisation roles.

Community Trust is not an authorisation mechanism.

The implementation details are defined in `docs/implementation/authorization.md`.

---

# 17. Sessions

Authenticated sessions should:

* use secure transport;
* use secure, appropriately configured cookies or tokens;
* support sign out;
* support multiple devices;
* expire appropriately;
* support revocation where required;
* minimise unnecessary session lifetime.

The application should balance security with the convenience expected from a mobile-first experience.

---

# 18. Session Revocation

The system must support invalidating authentication where required.

Examples include:

* User-initiated sign out;
* compromised authentication method;
* removal of a passkey;
* account suspension;
* security incident;
* administrative intervention.

Privileged access should not remain active indefinitely after authorisation is revoked.

---

# 19. Authentication Events

Security-relevant authentication events should be auditable.

Examples include:

* authentication succeeded;
* authentication failed;
* magic link requested;
* magic link consumed;
* passkey registered;
* passkey removed;
* session created;
* session revoked;
* account recovery initiated.

Audit records should contain sufficient information for security investigation without unnecessarily retaining personal information.

---

# 20. Account Recovery

Users must have a secure means of regaining access.

The primary recovery mechanism is email-based authentication.

A User with an existing registered passkey may also authenticate through another registered passkey.

The recovery design should avoid creating a weaker security path than the normal authentication mechanism.

---

# 21. Account Linking

When a User adds an additional authentication method, the system must verify control of that authentication method before linking it.

The system must prevent an attacker from attaching their own credential to another User's account.

Account linking is therefore an authenticated and security-sensitive operation.

---

# 22. Account Deletion

Account deletion must distinguish between:

* personal account information;
* authentication credentials;
* personal Backpack data;
* contribution provenance;
* historical Atlas information.

Deleting an account must not silently destroy the provenance required to understand published Atlas knowledge.

Where historical contribution records must remain, personal identity should be anonymised appropriately.

---

# 23. Privacy

Authentication systems should minimise personal information collection.

The application should not expose:

* email addresses;
* authentication credentials;
* passkey identifiers;
* recovery information;
* security events;

to other Explorers unless explicitly required.

Public Explorer identity should use the User's chosen public profile information.

---

# 24. Authentication UX

Authentication should occur at the point where it becomes necessary.

Typical journey:

```text
Browse
   ↓
Search
   ↓
Explore
   ↓
Discover something
   ↓
Want to contribute?
   ↓
Sign In
   ↓
Continue immediately
```

The User should return to the action they were attempting after successful authentication wherever practical.

Authentication should not unnecessarily discard:

* search context;
* Product being viewed;
* Location being viewed;
* Observation draft;
* Discovery Task context.

---

# 25. Authentication and Offline Work

The PWA may allow unauthenticated Users to prepare certain local actions, such as drafting an Observation.

However:

> **Local work is not authenticated server-side work.**

When the User attempts to synchronise a protected action:

1. The application verifies authentication.
2. The server establishes the User identity.
3. The action is submitted.
4. Normal validation and workflow processing occurs.

The client must never treat locally stored identity state as proof of server authentication.

---

# 26. Security Principles

Authentication must follow modern security practices.

The implementation should include:

* HTTPS everywhere;
* secure authentication cookies or tokens;
* CSRF protection where applicable;
* secure random authentication tokens;
* short-lived magic links;
* single-use magic links;
* WebAuthn verification;
* replay protection;
* rate limiting;
* account protection;
* secure session handling;
* appropriate audit logging.

Sensitive implementation details remain internal to the platform.

---

# 27. Rate Limiting

Authentication endpoints must be protected against abuse.

Rate limiting should apply to operations such as:

* magic link requests;
* magic link verification;
* passkey registration;
* authentication attempts;
* account recovery;
* account linking.

Rate limits should balance abuse prevention with legitimate use.

---

# 28. Enumeration Protection

Authentication endpoints should avoid unnecessarily revealing whether an account exists.

For example, requesting a magic link for an unknown email address should not provide a materially different public response from requesting one for a registered address.

This reduces account enumeration risk.

---

# 29. Authentication and Authorisation Boundaries

The architecture is:

```text
Authentication
      │
      ▼
     User
      │
      ▼
   Explorer
      │
      ├── Contribution
      │       │
      │       ▼
      │   Contributor
      │
      └── Community Trust
```

Privileged access exists separately:

```text
User
 ├── Moderator
 ├── Administrator
 └── Verified Manufacturer
```

These responsibilities are explicitly authorised.

They are not earned automatically through authentication, contribution volume or Community Trust.

---

# 30. Implementation Direction

The initial production implementation should use the platform's established ASP.NET Core authentication infrastructure rather than inventing a custom identity system.

The authentication implementation should provide:

* persistent User identity;
* secure session handling;
* passwordless authentication;
* WebAuthn/passkey support;
* external provider extensibility;
* role/policy integration;
* account lifecycle management.

The implementation must remain behind the application's authentication boundary.

Domain code should not depend directly on HTTP authentication mechanisms.

---

# 31. Testing

Authentication tests should cover:

### Magic Links

* valid link;
* expired link;
* already-used link;
* invalid link;
* replay attempt;
* rate limiting.

### Passkeys

* registration;
* successful authentication;
* invalid assertion;
* removed credential;
* multiple credentials;
* recovery.

### Sessions

* creation;
* expiry;
* sign out;
* revocation;
* multiple devices.

### Account Linking

* valid linking;
* unauthorised linking;
* duplicate credential;
* credential removal.

### Privacy

* account enumeration;
* private identity exposure;
* inappropriate authentication metadata exposure.

### Integration

* authentication → User;
* User → Explorer;
* authentication → authorisation policy;
* authentication → protected contribution workflow.

---

# 32. Anti-Patterns

The following are explicitly prohibited unless the architecture is formally changed.

## Scout Authentication Role

There is no `Scout` authentication role.

## Contributor Authentication Role

Contributor is not an authentication role.

## Trusted Contributor Authentication

Community Trust must not become an authentication or authorisation credential.

## Password Fallback

Do not introduce passwords as an incidental fallback to passwordless authentication.

## Client-Side Authentication

Client state must never be treated as authoritative proof of identity.

## Shared Accounts

Authentication credentials should belong to individual Users rather than shared community accounts.

## Direct Domain Authentication Dependencies

Domain logic must not depend directly on ASP.NET Core authentication objects.

---

# 33. Relationship to Other Documents

This document defines how DiaperScout establishes User identity.

Related documents include:

* **Authentication & Roles** — defines identity and responsibility boundaries.
* **Authorization** — defines implementation-level permissions and policies.
* **Domain Model** — defines User, Explorer and contribution concepts.
* **API Architecture** — defines protected API capabilities.
* **Security** — defines broader platform security requirements.
* **Workflow Architecture** — defines how authenticated actions progress through the system.

Together these documents establish a passwordless, low-friction authentication system that protects the community while allowing anonymous exploration of the Atlas.

---

# 34. Summary

DiaperScout authentication is intentionally simple:

```text
Anonymous
   │
   └── Explore the Atlas
          │
          ▼
     Want to contribute?
          │
          ▼
      Authenticate
          │
          ▼
         User
          │
          ▼
       Explorer
          │
          └── Contribute
```

Authentication establishes identity.

Authorisation establishes permission.

Contributor describes participation.

Community Trust describes internal contribution reliability.

None of these concepts should be conflated.

The result is an authentication system that protects the Atlas without turning exploration into an account-management exercise.
