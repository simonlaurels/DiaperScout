# Installed PWA first-run welcome

The approved 2 October welcome composition is implemented as real accessible HTML over supplied artwork. This is onboarding, not an authentication gate. Only the ordinary standalone `/` launch offers it; direct catalogue, Scan and contribution URLs remain public and unchanged.

## Implementation

- `Components/Pages/Home.razor`: existing server `AuthorizeView` renders `Shared/PwaWelcome.razor` only for an unauthenticated principal. No client authentication state is stored.
- `Components/Shared/PwaWelcome.razor`: accessible heading, real `/join` registration link, real `/signin` link and guest Continue button.
- `Components/App.razor`: adds only welcome stylesheet and client module.
- `wwwroot/pwa/welcome.js`: waits for existing `diaperscout:ready` / genuine `pwaState=ready`; handles existing enhanced navigation. No timers, retries or artificial progress. Explicit guest Continue sets localStorage `ds-welcome-complete-v1=yes`, hides onboarding and focuses Explore. Storage denial still permits entry for the current document; future launches may offer onboarding again.
- `wwwroot/css/pwa-welcome.css`: illustrated fullscreen standalone composition, stitched parchment card, green primary and cream secondary actions, safe-area padding and short-height layout. Normal Explore and navigation are unchanged after Continue.
- `wwwroot/pwa/welcome-brand.webp`, `welcome-art.webp`: deterministic crops of the supplied reference (1536×1024, inner artwork rectangles `(509,72)-(1027,276)` and `(509,276)-(1027,708)`). No generated art or map. Splitting the brand from the scene preserves the full logo while the scene adapts to viewport height.
- `Components/Pages/SignIn.razor`: primary authentication button now says **Sign in**, as requested. Its existing handler still invokes passkey authentication first; the existing email-link form remains the fallback. No credential/security behavior changes.

The original static/pre-framework startup, slow/failure messaging, circuit readiness, service worker and caching policy are untouched. Welcome stays hidden during warmup or failure. Authenticated users receive no welcome markup; returning guests skip it using only the non-sensitive preference.

## Validation and acceptance

`PwaWelcomeBrowserTests` covers WebKit 375×667 and 390×844 plus Chromium 430×932: first-run readiness, all controls within the viewport with 44px targets, real registration/sign-in routes, passkey-first ordering and email fallback availability, guest entry and persistence, anonymous Scan access, authenticated bypass, pending-framework startup and blocked-storage guest entry. Existing passkey tests continue to exercise successful credential authentication and fallback paths using the updated button label.

Browser automation emulates standalone detection; it cannot establish installed physical iOS PWA storage behavior, actual safe-area insets, device passkey prompts or physical cold-start acceptance. On the real iPhone: launch a fresh unauthenticated installation, observe startup then welcome; verify the full logo and all actions above the home indicator; open Create account and Sign in; use Sign in with a device passkey or email fallback; verify authenticated launches skip welcome. On a separate guest installation choose Continue, browse and Scan anonymously, relaunch and verify welcome is skipped. Check both short and tall orientations / accessibility text sizes.

Deliberate visual adaptations: phone bezel, simulated status bar and home indicator are excluded (iOS supplies native chrome); artwork is cropped from the reference, with a real HTML card and repository typography rather than baked lettering for the controls. No map is introduced. Direct deep links bypass onboarding to preserve existing public and contribution navigation.

## Rollback

Revert the welcome implementation commit and deploy only the Web image using the established image-only process. The local preference can remain harmlessly stored. The pre-change known-good Web image is `diaperscoutprod-dsg9bgg6dkgkcwbs.azurecr.io/diaperscout-web@sha256:6090285e3fa879a6152cf1580e2227aa854105d6ec74436dddf7a653a2ccc600`. Preserve min0/max2, cooldown600, Single revision and sticky affinity, API configuration, shared Data Protection and every unrelated setting.

Validation results (2 October 2026): full serial solution suite passed **235 integration + 51 domain tests**, zero failures. After the requested Sign in label and focus-styling adjustment, all **10 welcome/passkey tests** passed. Final Release build succeeded with zero warnings/errors. WebKit/Chromium screenshots were reviewed; the full logo and controls are visible on 375×667, 390×844 and 430×932. Evidence is checked into `welcome-evidence/`. Physical-iPhone acceptance remains pending.

## Production deployment

Runtime commits `d8cbc02` and `f3cc354` contain the welcome implementation and background skip-link accessibility correction. A final full-suite run with the requested Sign in label again passed **235 integration + 51 domain tests**, zero failures (3m34s integration); final evidence is `welcome-evidence/final-full-suite.log`.

ACR run **db1e** succeeded at **2026-10-02 16:02:37 UTC**, producing `diaperscoutprod-dsg9bgg6dkgkcwbs.azurecr.io/diaperscout-web@sha256:bdfe52e0a85dea708f226e6c83c723a8eb3a17ced77f22d314a737cf7bf11624`. Image-only deployment created **diaperscout-web-vnet--welcome-f3cc354**, Healthy / Running / Provisioned, with 100% traffic. Fresh GET comparisons before/after confirm configuration, managed identity, environment, complete template except image/revision suffix, scaling and secret references are equal. API image/revision are unchanged. Web remains min0/max2, cooldown600, polling30, Single revision and sticky affinity. Shared Data Protection, networking, TLS and caching are preserved.

Production browser smoke passed at **16:05:54 UTC Chromium** and **16:05:58 UTC WebKit**: first-run readiness, visible actions, real Sign in page with primary **Sign in** and email fallback, Continue, guest persistence after reload and anonymous Scan with no error banner. No authentication attempts, catalogue writes, submissions or observations were made. Checked evidence includes safe ACR metadata, revision health, sanitized config comparison, smoke script/output and production WebKit screenshot. Raw Azure snapshots containing environment configuration remain outside source control.

Physical installed iPhone acceptance is still pending; no claim is made that desktop WebKit validates physical iOS cold start, actual passkey prompts or installation-local persistence.
