# Passkey authentication

Passkeys supplement email magic links. Users first sign in to an existing account, open **Passkeys**, and add a named passkey. A recent sign-in (15 minutes) is required to add or remove one. Email links remain available for registration, recovery, and devices without passkey support.

## WebAuthn implementation and review

Production uses the maintained [Fido2 .NET library, version 4.1.0](https://github.com/passwordless-lib/fido2-net-lib/tree/4.1.0). The browser uses the native `navigator.credentials.create()` and `navigator.credentials.get()` WebAuthn APIs. No application code implements signature verification, key generation, attestation parsing, or a replacement authentication protocol.

- `IFido2.RequestNewCredential` and `GetAssertionOptions` generate options and random 32-byte challenges.
- `IFido2.MakeNewCredentialAsync` validates registration against the stored creation options, configured origin, RP ID hash, user-presence and user-verification flags, credential type, and permitted algorithms. A database-wide uniqueness check and index prevent duplicate credential IDs.
- `IFido2.MakeAssertionAsync` validates the assertion challenge, origin, RP ID hash, signature using the stored public key, credential ID, user verification/presence, and signature counter. Every successful passkey sign-in passes through this method.
- Both ceremonies require user verification. Registration requires a discoverable credential, so sign-in needs no email address and exposes no account-specific credential list.
- The application binds each challenge to a short-lived HttpOnly, SameSite Strict browser cookie. PostgreSQL stores its hash, original options, purpose, owner (for registration), expiry, and consumed timestamp. An atomic update consumes a challenge once, including across replicas. Invalid verification consumes that attempt; users can start another.
- Cross-origin ceremonies are rejected. Origin and RP settings come from server configuration, never request headers or client input. User handles are opaque account IDs and must match the stored credential owner. The library also checks ownership via its callback.
- Current account status and current, unrevoked roles are read from the database at sign-in. A removed credential cannot complete a pending sign-in. A version check prevents concurrent assertions from overwriting a newer counter or removal.
- The library parses backup eligibility; the application compares it with the saved registration value. Synced passkeys and zero signature counters are supported according to library verification rules.
- ASP.NET Core supplies authentication cookies and antiforgery validation. Mutating web endpoints require CSRF tokens; passkey routes have per-user/IP rate limits. Limits are per web replica, not a distributed quota.
- Only credential IDs, public keys, counters, backup flags, names, and timestamps are stored. Private keys and biometrics stay with the user's authenticator. ECDSA/CBOR construction in `TestPasskey` is exclusively a synthetic test authenticator and is never included in the production application.

Attestation is requested as `none`: this accepts consumer passkeys without a hardware-vendor trust policy. This does not bypass possession-of-key or origin/RP verification during sign-in.

## Configuration

Production defaults in API appsettings:

```json
"Authentication": {
  "Passkeys": {
    "RpId": "diaperscout.app",
    "Origins": [ "https://diaperscout.app" ]
  }
}
```

The RP ID is the credential scope and must remain stable. Add an origin only when that exact HTTPS site is intended to authenticate users for this RP. Do not add wildcards or derive origins from incoming Host headers. Development uses `localhost` with the repository's HTTP/HTTPS web ports. LAN testing needs an explicitly configured secure origin and matching RP ID.

## Database and rollout

`AddPasskeyAuthentication` adds `diaperscout.passkey_credentials` and `diaperscout.passkey_challenges`. It does not change existing accounts, roles, magic-link tokens, or profiles. Apply the migration before deploying the API and web images. Both images are required for this feature. Challenges are stored in PostgreSQL, so no in-memory session affinity is required for ceremonies. Existing cookie encryption-key deployment requirements remain unchanged.

Old authentication cookies have no recent-sign-in timestamp. Those users must sign in again before adding/removing a passkey. Removing the final passkey is permitted because email-link sign-in remains available.

## Verification

API tests use real P-256 signatures against the actual Fido2 verifier and disposable PostgreSQL. They cover successful sign-in, account/role preservation, replay and concurrent replay, invalid challenge/origin/RP/signature, missing user verification/presence, wrong user handle, expired challenges, different browsers/accounts, duplicate credentials, suspended accounts, removal, and email-link compatibility.

Web tests exercise cookies, antiforgery, browser binding, recent-sign-in checks, and Administrator/Explorer authorization. A Chromium test uses its virtual CTAP2 authenticator through the real browser WebAuthn APIs to add, sign in, and remove a passkey. This does not replace final testing on physical Windows Hello, Apple, Android, and security-key devices.

See the integration-test README for browser installation and test commands.
