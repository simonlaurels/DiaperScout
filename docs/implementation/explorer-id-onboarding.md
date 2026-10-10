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

## Production release — 5 October 2026

Runtime commit `3f7fd05` was pushed to `fix/product-submission-recovery`. Release build completed with zero warnings/errors. The complete suite passed **380/380 tests** (64 domain, 316 integration/browser), with no skips. ACR builds `db2e` (API) and `db2f` (Web) succeeded from the committed source archive.

API was deployed first for compatibility, followed by Web. Both `diaperscout-api-vnet--onboard-3f7fd05` and `diaperscout-web-vnet--onboard-3f7fd05` are Healthy, Provisioned and receive 100% traffic. No migrations or jobs were run.

- API image: `sha256:3d2528b9f57b4b51ce54cd48bd0c585e2d1d54fa28de2e32a226ce3077bc19af`
- Web image: `sha256:191755436b06b3c7418c25a0fa5cd825c4923ca4190d3e6ed44cdbcc5f27ae2c`

[Configuration comparison](explorer-id-onboarding-evidence/config-comparison.json) confirms preservation of identities, environment, configuration and container template apart from image/revision suffix. API ingress remains internal and Web remains public. No networking, credentials, configuration or scale settings were changed.

[Live checks](explorer-id-onboarding-evidence/production-smoke.json) used Chromium and WebKit at 390×844 against `https://diaperscout.app`: onboarding intro/details and required inputs, catalogue and real product detail, Explore, Atlas, Scan, sign-in and Backpack; anonymous continuation/state/ready access was rejected. No synthetic accounts, email deliveries or production records were created. The authenticated email/passkey journeys were exercised in isolated tests; real-device authenticated production onboarding remains a manual check.

Recent console logs showed no TLS/DNS/database connection failures. Web logged two Explore module-import exceptions during the smoke-check period; subsequent browser checks completed without page errors. These logs are recorded as a limitation, not evidence of a completely error-free deployment window.

Rollback is image-only using the previous immutable images:

- API: `diaperscoutprod-dsg9bgg6dkgkcwbs.azurecr.io/diaperscout-api@sha256:cc5150c6a53d3802eac293715006ea177c907f53bc97623121d3246328a0049b`
- Web: `diaperscoutprod-dsg9bgg6dkgkcwbs.azurecr.io/diaperscout-web@sha256:a1d9f2da01185d0526f2c7f3262cbe06d4db3ec07f63a39b53397159312566f2`
