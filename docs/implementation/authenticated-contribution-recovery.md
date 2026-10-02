# Authenticated contribution recovery — 2026-10-02

Application fix and automated validation; **physical iPhone iOS 27.0.1 installed-PWA acceptance remains required**. This investigation covers unknown barcode → Add product → Product → Pack → Review → authenticated submission. Scanner and contribution layout/design are unchanged.

## Root causes and evidence

The user confirmed the banner appeared **after clicking Submit for review**, consistent with the API response mapping rather than an anonymous Review on entry. There are two user-visible defects, with an additional independently reproduced authentication-scope defect.

1. **An optional Explorer profile was incorrectly required for proposals.** The shell, Blazor component and passkey session correctly accept an authenticated account. The API proposal endpoint required an authenticated `Explorer` policy, then called `ICurrentExplorer.GetAsync`. That implementation joins active Users to ExplorerProfiles; a valid active account without a profile returns null and receives **403**, despite valid authentication. Passkey authentication requires an active User, not an ExplorerProfile. Existing privileged/legacy accounts legitimately lack profiles; User Management explicitly handles them with a left join. The proposal service only uses the actor's UserId, so this prerequisite was unnecessary. The Web client mapped both 401 and 403 to “Sign in again…”, incorrectly presenting authorization rejection as lost authentication. Reauthentication does not create an Explorer profile.

   Before changing runtime code, the profileless active-account regression returned Forbidden instead of OK: [baseline failure](contribution-recovery-evidence/profile-gate-before.log). A **read-only production aggregate probe** found two active accounts, one active account without a profile, and one passkey-backed privileged account without a profile: [counts](contribution-recovery-evidence/profile-counts.json). No user identity, passkey material, cookies or entered contribution fields were queried/logged. The probe ran an execution-only override of an existing private-network job, removed its bootstrap arguments, used an explicitly read-only PostgreSQL transaction and rolled back. The persistent job template/identity were unchanged: [execution evidence](contribution-recovery-evidence/probe-execution.json). Its small audited source is in `ops/ContributionAuthProbe`; it is not part of the deployed Web/API runtime.

   Production logs contain four `ProductionIdentity was forbidden` events: [events](contribution-recovery-evidence/api-forbidden-events.json). The existing logs omit route/user correlation, so these cannot individually be assigned to either physical reproduction. The account prerequisite and production aggregate establish an application defect consistent with the reported signed-in shell plus rejected submission; physical acceptance is still needed to confirm the fix on that device.

2. **Authenticated drafts were never persisted before a rejected submission or reauthentication.** Product/Pack/Review state lived only in the circuit. `SubmitAsync` did not save a recovery draft. Only the anonymous `SignInAsync` branch saved a draft and supplied the contribution return URL. Following the misleading banner via the shell's generic sign-in discarded the old circuit; generic sign-in returns home, with no draft/context to restore. The old mechanism used sessionStorage, which also cannot restore a draft in a different tab/PWA browsing context. The protected continuation cookie already correctly preserves an allowlisted return URL; its purpose/lifetime and shared Data Protection remain unchanged.

3. **The pooled identity handler cannot reliably read circuit authentication.** `ProductionIdentityForwardingHandler` receives AuthenticationStateProvider from IHttpClientFactory's handler scope. This is not the InteractiveServer circuit scope. When ambient HttpContext is absent, its fallback provider is uninitialized and no signed assertion is sent. A real ServerAuthenticationStateProvider in the application scope, with the actual client/handler factory, reproduced an unsigned/401 proposal while the authenticated request-context case passed: [baseline failure](contribution-recovery-evidence/identity-scope-before.log). This is independently proven in tests, **not established as the physical device's observed 403 cause**. Microsoft's [HttpClientFactory scope guidance](https://learn.microsoft.com/dotnet/core/extensions/httpclient-factory-troubleshooting) recommends passing per-request context in HttpRequestMessage.Options rather than relying on scoped authentication in pooled handlers.

## Authentication and state trace

| Layer | Finding / resulting behavior |
|---|---|
| Header/shell | AuthorizeView reads the authenticated Web cookie/principal; showing Passkeys/Sign out was consistent with successful authentication. |
| SSR / circuit / Review | AuthenticationStateProvider is supplied by the actual Web request/circuit. Review now refreshes it before submission. Prerendered inputs remain disabled until interactive draft restoration completes. |
| Web contribution client | Gets the actual application/circuit principal and supplies it in server-only HttpRequestMessage.Options. The forwarder prioritizes it over stale/absent ambient HttpContext, signs the existing 60-second HMAC assertion, and removes any prior identity header. Anonymous circuit state cannot inherit a stale authenticated request. |
| API | Existing ProductionIdentity signature/expiry validation, authenticated policy and rate limit remain. Proposals now use the existing `ICurrentUser` active-account check. Suspended/anonymised/anonymous users remain rejected. Actor ID remains server-derived. Shops/observations still use their existing Explorer requirements. |
| Passkey/session/cookies | Native WebAuthn registration, assertion verification, session creation and sign-out are unchanged. Production-identity API tests exercise real passkey-backed cookies and real signatures against an isolated database. No evidence required changing cookie expiry, credentials or shared keys. |
| Draft/continuation | Saves every bound field change, step transition, and before submission/sign-in. Retains exact form, GTIN key, current step and stable ContributionId in origin-local storage, expires after 30 minutes of inactivity, and separates authenticated owners when restoring. Legacy session-only drafts can still hydrate. Restored anonymous drafts are associated with the authenticated owner. No credentials, cookies, authentication secrets or draft fields are placed in URLs. |
| Failure/recovery | 401 offers the explicit protected sign-in continuation. 403 explains account permission rejection without asking for another sign-in. Save/restore failure is visible; storage-blocked sign-in is refused to avoid knowingly discarding the form. No blind reload/retry loop. |
| Success / duplicate prevention | Existing `(SubmittedByUserId, PublicContributionId)` uniqueness and payload consistency remain. A confirmed receipt clears only the matching draft ID. A lost response retains the stable ID so an explicit retry returns the same submission. No canonical product is automatically published. |

No evidence establishes scale-to-zero, cross-replica cookie decryption, service-worker caching or enhanced navigation as the cause. Sticky affinity and shared key readiness were already validated in [the current scaling report](web-scale-to-zero.md). This fix neither adds retry loops nor disguises connection failures. A circuit cannot be moved between replicas; local recovery now survives a new circuit within the draft lifetime. Browser storage can still be cleared/evicted by the user/OS; it is not server-side durable storage. Existing unsaved old-version circuit memory cannot be retroactively recovered, and arbitrary navigation after a field change may discard uncommitted keystrokes before its change event. Review/step transitions and submission explicitly save the complete bound form.

## Changed files and regression coverage

- API `PlaceObservationEndpoints.cs`, Application/Infrastructure `PublicProductContributions.cs`: proposal actor becomes AuthenticatedUser through the existing active-account lookup; no schema/migration/profile fabrication.
- Web `ProductionIdentityForwardingHandler.cs`, `PlaceObservationClient.cs`: application-scope contribution principal forwarding; distinct 401/403 behavior.
- Web `PublicProductSubmission.razor`, `contribution-draft.js`: recovery persistence, owner/step restoration, legacy compatibility, failure handling and matching-ID cleanup. Existing hydration guard/layout retained.
- `ContributionIdentityTests.cs`: authenticated request/circuit, stale ambient principal, anonymous circuit overriding stale authenticated context, real HMAC forwarding.
- `PlaceObservationApiTests.cs`: profileless active proposal + repeat receipt/one row/no manufactured profile; suspended/anonymised forbidden; existing anonymous writes/security/moderation tests retained.
- `PasskeyBrowserTests.cs`: real Chromium virtual WebAuthn registration/sign-in/remove, profileless active account, production identity verifier, direct authenticated Review/submission and anonymous new-tab sign-in/Review restoration, exact stored ID, one moderated submission/no publication.
- `PlaceObservationBrowserTests.cs`: Chromium/WebKit unknown journey and continuation; lost response **after durable API save**, reload and idempotent retry; exact storage fields/step/owner isolation/expiry/legacy/storage failure/matching-ID cleanup.
- Existing `PwaReliabilityTests.Proposal_does_not_accept_input_until_hydration_and_draft_restore_complete` retained and rerun for both engines.

The API test fixture uses Development hosting with development authentication **disabled**, selecting the real ProductionIdentity authentication scheme. The Web fixture uses local non-Secure HTTP cookies for browser automation; production authentication settings are not changed. Chromium's synthetic authenticator performs native navigator.credentials ceremonies and the server verifies real WebAuthn signatures. Desktop WebKit covers standalone detection, Blazor navigation and draft semantics, but cannot prove iOS Home Screen native credential UI, OS suspension, storage eviction or restoration across Safari/PWA containers. No automated production authentication or submission is attempted.

## Validation and deployment

[Full suite](contribution-recovery-evidence/full-suite.log): **278 passed, 0 failed, 0 skipped** (227 integration/browser + 51 domain). [Focused tests](contribution-recovery-evidence/targeted.log): 17 passed before the final additional security cases, which pass in the full suite. [Release build](contribution-recovery-evidence/release.log): **0 warnings, 0 errors**. Production deployment/smoke evidence follows after image readiness. Application rollback targets are the immutable images captured before this change:

- Web: `diaperscoutprod-dsg9bgg6dkgkcwbs.azurecr.io/diaperscout-web@sha256:7f97686bf116ed3601fc63112e6266b9bca2fd72e1dfb00e0c9733c652ebb5b9`
- API: `diaperscoutprod-dsg9bgg6dkgkcwbs.azurecr.io/diaperscout-api@sha256:0ee3d21f3f70308008ca3416b001d1482bf06f19624eae15850678ea6bdb05b7`

## Application rollback

Use image-only updates, with unique revision suffixes, then check readiness/health and a live interactive circuit:

```bash
az containerapp update -g rg-diaperscout-prod -n diaperscout-api-vnet --image diaperscoutprod-dsg9bgg6dkgkcwbs.azurecr.io/diaperscout-api@sha256:0ee3d21f3f70308008ca3416b001d1482bf06f19624eae15850678ea6bdb05b7 --revision-suffix contribution-rollback-api
az containerapp update -g rg-diaperscout-prod -n diaperscout-web-vnet --image diaperscoutprod-dsg9bgg6dkgkcwbs.azurecr.io/diaperscout-web@sha256:7f97686bf116ed3601fc63112e6266b9bca2fd72e1dfb00e0c9733c652ebb5b9 --revision-suffix contribution-rollback-web
```

No DB rollback/migration is needed. This application rollback preserves Web min0/max2/cooldown600/Single/sticky and independent API scaling/shared keys. It restores the old contribution defects; protect in-progress drafts before doing so. Do not use the separate warm scaling rollback as part of this application rollback.

## Required physical acceptance

1. Preserve the details of the existing second genuine draft before loading the new revision; it was created by the old memory-only code. If it survives, review it; otherwise restore from the details already provided. A lost first draft cannot be retrieved from unsaved old circuit memory.
2. Relaunch the installed iPhone PWA on iOS27.0.1, authenticate with the existing passkey if needed, and confirm Passkeys/Sign out. Scan the genuine unknown pack and complete Product/Pack/Review. Confirm all fields and barcode are exact, and **Submit for review** appears without an unnecessary sign-in gate.
3. Submit once. Confirm Sent for review/reference. Check the established moderation queue contains exactly one corresponding proposal, with correct fields and no automatic catalogue publication. Do not deliberately submit the same genuine pack as a new proposal again.
4. For a separate genuine unsent draft, test anonymous completion → Sign in to submit → native passkey → same Review with exact fields → submit once. Check background/resume and a full page reopen within 30 minutes before submission; resume the same barcode route when reopening elsewhere.
5. Record any failure stage and first non-secret console error; do not share cookies/passkey material. This physical issue is **not fully resolved until the real installed PWA completes the previously failing journey**.
