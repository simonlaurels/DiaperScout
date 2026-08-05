# Authentication Strategy

**Document Status:** Draft  
**Version:** 1.0  
**Owner:** DiaperScout Project  
**Last Updated:** 2026-08-02

---

# 1. Purpose

This document defines the authentication strategy for the DiaperScout platform.

Authentication answers a single question:

> **Who is this user?**

Authentication is distinct from authorisation.

Authentication establishes identity.

Authorisation determines what that identity is permitted to do.

---

# 2. Design Goals

The authentication experience should be:

- Secure
- Simple
- Passwordless
- Mobile-friendly
- Low friction
- Future-proof

The objective is to reduce barriers to community participation while maintaining appropriate security.

---

# 3. Authentication Philosophy

DiaperScout is a community platform.

Most users will discover the platform while:

- Shopping
- Scanning products
- Searching for information

Authentication should therefore interrupt the user journey as little as possible.

Browsing should remain anonymous.

Authentication should only become necessary when a user wishes to contribute.

---

# 4. Anonymous Access

Anonymous users may:

- Search products
- Browse Atlas
- Read reviews
- View observations
- Explore manufacturers
- Browse brands

No account is required for read-only access.

---

# 5. Authenticated Access

Authentication is required for activities including:

- Creating products
- Submitting reviews
- Creating observations
- Uploading photographs
- Voting
- Editing contributions
- Moderation
- Administration

---

# 6. Initial Authentication Methods

The initial release supports two authentication methods.

## Email Magic Links

Users enter their email address.

A secure, time-limited sign-in link is delivered via email.

Selecting the link authenticates the user.

No password is required.

---

## Passkeys (WebAuthn)

Users may register one or more passkeys.

Supported authenticators include:

- Face ID
- Touch ID
- Windows Hello
- Android Biometrics
- Hardware security keys

Passkeys provide a fast and secure sign-in experience without passwords.

---

# 7. Password Policy

The platform intentionally does **not** support passwords during the initial implementation.

This removes the need for:

- Password creation
- Password resets
- Password complexity requirements
- Password reuse concerns

Passwordless authentication is considered the preferred user experience.

---

# 8. Future Identity Providers

Additional authentication providers may be introduced in the future.

Potential providers include:

- Sign in with Apple
- Sign in with Google

These providers should complement the existing authentication model rather than replace it.

---

# 9. Linked Identities

A single DiaperScout account may support multiple authentication methods.

Examples include:

- Magic Link
- Passkey
- Apple
- Google

Users should be able to add or remove authentication methods without creating separate accounts.

Identity providers authenticate the same DiaperScout identity.

---

# 10. Sessions

Authenticated sessions should:

- Be secure
- Expire appropriately
- Support sign out
- Support multiple devices

Long-lived sessions should balance convenience with security.

---

# 11. Authorisation

Authentication establishes identity.

Authorisation determines permissions.

Typical roles include:

| Role | Purpose |
|------|---------|
| Anonymous | Read-only access |
| Scout | Community participation |
| Moderator | Community management |
| Administrator | Platform administration |

Roles should remain separate from authentication methods.

---

# 12. Security Principles

Authentication should follow modern security practices.

Examples include:

- HTTPS everywhere
- Secure cookies
- Short-lived authentication tokens
- Protection against replay attacks
- Protection against phishing
- Multi-device support

Sensitive implementation details should remain internal to the platform.

---

# 13. User Experience

Authentication should support the natural flow of using DiaperScout.

Typical journey:

```text
Browse

↓

Search

↓

Scan Barcode

↓

Want to contribute?

↓

Sign In

↓

Continue immediately
```

Authentication should never become an unnecessary obstacle.

---

# 14. Recovery

Users should always have a means of regaining access to their account.

Possible recovery methods include:

- Email magic links
- Additional registered passkeys

Recovery procedures should prioritise both usability and account security.

---

# 15. Future Evolution

Potential future enhancements include:

- Additional identity providers
- Enterprise authentication (if required)
- Trusted device management
- Session management improvements

Authentication methods should evolve without requiring users to recreate their accounts.

---

# 16. Design Philosophy

Authentication exists to establish trust—not create friction.

The preferred experience is one in which users spend their time contributing to the DiaperScout community rather than managing credentials.

Passwordless authentication aligns with the platform's mobile-first philosophy while reducing support overhead and improving security.

As the platform evolves, authentication should remain simple, secure and centred around the user experience.