# Explorer ID onboarding

The account journey now runs from `/join` through name/nickname and email, a secure verification link, optional passkey creation, and an Explorer ID ready screen. Welcome and global navigation retain their existing design. Existing Guide artwork introduces the journey; the physical key and luggage tag appear only when a real registered passkey is confirmed. Skipping leaves verified-email sign-in available and links to Backpack for later passkey management.

## Identity and security

The implementation extends the existing PendingRegistration, single-use hashed email tokens, authentication cookie, WebAuthn endpoints and Backpack APIs. Pending form details use a time-limited encrypted HttpOnly cookie; verified continuation and completion use protected authentication-cookie claims and server-side account/passkey reads. Refresh, fresh browser navigation and circuit loss do not depend on in-memory onboarding state. Passkey creation retains the existing recent-authentication requirement, CSRF protection and origin validation.

Email ownership is proved before creating or changing an Explorer profile. Existing profiles and roles are preserved. Per-email and per-user transaction locks serialize concurrent requests; resends reuse the existing pending record, invalidate older unused links and limit immediate repeat delivery. Failed delivery permits retry. Shared profile/Backpack creation also lets a verified profile-less account save its name through Backpack. Nickname conflicts require an authenticated choice instead of a fabricated replacement.

Web-to-API magic-link consumption sends the token in a request body rather than an HTTP client URL. The API retains the previous query contract for deployment compatibility. No new database fields or migration are required. Production VerifyFull, API ingress, networking, identities, secrets and scaling are unchanged.

## Verification

Coverage includes invalid details, email proof, replay/expiry, resend/concurrent consumption, failed delivery and retry, existing/blocked accounts, nickname conflicts, profile-less Backpack repair, optional passkey completion, returning email sign-in and pending-model detection. Mobile Chromium tests use a real virtual WebAuthn authenticator; WebKit covers email verification and explicit skip on an unsupported device. Tests exercise refresh and fresh-page continuation, busy states, cancellation and API failure recovery, real stored names, Backpack integration and horizontal overflow.

Two existing browser harness races were corrected without changing Scan or contribution runtime code: wait for review rendering before refresh; and wait for document parsing rather than DOMContentLoaded while deliberately blocking an imported module. Their existing refresh and hydration assertions remain.

Physical iPhone/PWA email handoff and native Face ID/passkey prompts still require a real-device check. Browser emulation and virtual authenticators do not establish those behaviours.

Production deployment evidence will be recorded after final checks and release.
