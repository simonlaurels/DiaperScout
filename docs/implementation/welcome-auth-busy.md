# Welcome authentication feedback — 3 October 2026

## Repository and existing flow

Started on clean `fix/product-submission-recovery` at `c81258a0f577fd9566dc07a085d4a09852ef2603`. Fetched origin and pulled with `--ff-only`: already up to date. Welcome, current Sign In, Search, Product Details, Available In and the deployed full-screen Scan were present. No local work was discarded, reset or stashed.

Welcome Sign in is a button handled by the existing `js/passkeys.js` client event handler: POST options, native credential selection, POST verification and the server-provided redirect. It is not a simple link to the Sign In page. Existing failure/cancellation handling reveals the `/signin` email fallback. Create account is the existing normal `/join` link and can encounter the same delayed navigation.

## Small implementation

Welcome's existing client module synchronously acknowledges an unmodified first activation in the capture phase, before the original passkey/link handlers. Only the activated action receives a decorative spinner and temporary text: **Signing in…** or **Getting ready…**. The latter does not claim that an account has been created. Native link navigation, keyboard activation, modified/new-tab links and all passkey requests remain intact. There is no artificial delay, loading screen or extra navigation request.

The existing full-width/minimum-height button styling is retained. Busy-only CSS adds a small spinner and gap; reduced-motion preferences disable its rotation. `aria-busy`, `aria-disabled`, visible text and a decorative `aria-hidden` spinner expose state. Client activation guards prevent repeated/conflicting Sign in/Create account actions while pending; the existing passkey handler still owns its native disabled state. Continue exploring remains unchanged.

A Welcome-only completion notification from the existing passkey handler differentiates errors/cancellation from successful navigation. Errors restore original contents immediately and use the existing error/fallback handling. Successful redirects keep feedback visible during destination loading even though the original handler's `finally` clears its internal busy flag. The notification changes no authentication operation or outcome. Enhanced navigation and `pageshow` reset feedback for revisit/back-cache restoration. Normal link/redirect navigation cannot reliably report every cancellation/failure to this page; no timeout or invented authentication error is added. Fresh navigation/render/revisit restores normal labels and state.

## Preserved scope

Authentication requests, credentials, return URLs, cookies, session/circuit/account semantics, passkeys and contribution/draft behaviour are unchanged. Sign In/registration screens and other PWA screens are unchanged. Welcome markup, artwork, positioning, typography and normal styling are unchanged. Canonical artwork SHA-256: `83A1AC0B2301F1F4AACE5937A75FA5919001F705ECF33F125B4FC552434E4674`; its tracked object is unchanged. No iOS status-bar, background, metadata, safe-area, cache/service-worker, startup/warm-up or infrastructure changes.

Runtime files: `wwwroot/pwa/welcome.js`, `wwwroot/css/pwa-welcome.css`, and the Welcome-only notification in `wwwroot/js/passkeys.js`. Automated coverage: `PwaWelcomeBrowserTests.cs`. All remaining changed files are this report and validation evidence; an exact path list is included in `welcome-auth-busy-evidence/changed-files.txt`.

## Validation

The focused cases gate local requests to demonstrate feedback before any response, duplicate/conflicting activation prevention, numerical button/card geometry stability, keyboard Sign in, options failure recovery, existing cancellation/fallback handling, native `/join`, successful passkey redirect while the destination is delayed, revisit/reset and guest persistence. Deterministic request-gating contexts block service-worker interception; existing service-worker/PWA tests retain their normal behaviour. Original test assertions remain intact.

Final automated/build/deployment evidence is added after verification completes. Physical iPhone acceptance has not been performed.

## Physical installed-iPhone checks still outstanding

1. Allow DiaperScout/authentication services to become cold if practical.
2. Open the installed PWA to Welcome.
3. Tap Sign in once.
4. Confirm the button reacts immediately.
5. Confirm the small animation and “Signing in…” appear while waiting.
6. Confirm repeated taps are prevented.
7. Confirm the Welcome layout does not jump.
8. Confirm the existing sign-in flow opens normally (native passkey prompt/redirect; existing Sign In screen when using email fallback).
9. Repeat with Create account; expect “Getting ready…” and the existing `/join` destination.
10. Confirm Continue exploring remains unchanged.
11. Confirm warm Sign in remains fast without awkward flashing/flicker.

Desktop/browser standalone simulation does not prove physical iPhone behaviour or real scale-to-zero latency.

Focused final run: **11 passed, 0 failed, 0 skipped**, 47 seconds. Release build: `dotnet build DiaperScout.slnx --configuration Release --no-restore` — **0 warnings, 0 errors**, 7.03 seconds. Complete suite uses the existing serialized browser settings, unchanged assertions, and D: test/temporary storage. Earlier focused iterations exposed test-side reference-versus-numerical box comparison, service-worker request interception and pending full-document automation waits; those fixtures were corrected without weakening assertions. One pending-navigation inspection run was interrupted; the replacement captures the exact busy DOM state at the completion event before releasing the destination response.

Complete suite: `dotnet test DiaperScout.slnx --configuration Release --no-build --settings docs/implementation/search-prototype-evidence/validation.runsettings --logger trx --results-directory D:\Codex\DiaperScout-welcome-validation/full` — Domain **51 passed** (3 seconds); integration/browser **258 passed** (10 minutes 31 seconds); **309 passed, 0 failed, 0 skipped** in total. Existing authentication, contribution/draft, Search/Product/Available In, full-screen Scan and PWA regressions all passed. Exact counters are in welcome-auth-busy-evidence/test-runs.json.

## Production deployment and live evidence

Validated runtime commit **f805125364d6e4e0b37bb53daab685df2aeba468** was pushed to the existing `fix/product-submission-recovery` branch. ACR build **db1m** succeeded from an immutable archive of that commit's required source and the unchanged Dockerfile. Image: `diaperscoutprod-dsg9bgg6dkgkcwbs.azurecr.io/diaperscout-web@sha256:550d4874ddccdf688d465346228651a619c043f7fc97c4feb57058c11bcfce0a`. Revision **diaperscout-web-vnet--welcome-f805125** is Healthy / Running / Provisioned, latest ready revision, serving 100% traffic. Build context and private snapshots/temporary files use D:.

Before/after Azure GET comparison confirms an **image/revision-suffix-only Web deployment**: configuration, remaining template, identity, environment, scaling and secret references match for both apps. API image/revision are unchanged. Web remains min 0 / max 2, cooldown 600 / polling 30, sticky affinity; API remains min 0 / max 10, cooldown 300 / polling 30. Data Protection references, network/Private Link, database and TLS configuration remain intact. No API deployment, migration, cache/service-worker or warm-up change. Private snapshots are outside Git; config-preservation.json contains only preservation booleans and nonsecret metadata.

Final live standalone-simulated Chromium 390×844 and WebKit 375×667 validation: **2/2 passed**, completed **3 October 2026 09:40:07 UTC**, no browser errors. Deterministically delayed locally intercepted passkey responses prove feedback before a response, duplicate/conflicting action prevention, unchanged button/card geometry, error reset and the existing `/signin` email fallback. Create account similarly acknowledges the tap before the real `/join` GET completes, preserves that destination and resets on Back. Guest entry/persistence remain unchanged. No real authentication POST, account creation or sign-in was performed: the options response was supplied locally by the browser harness, and only real GET destinations were opened. This does not prove physical iPhone latency/passkey interaction. Existing authentication behaviour is additionally protected by the complete automated suite.

Busy screenshots for both actions and engines were inspected. Initial live checks read the returned label before enhanced-navigation reset; the final harness waits for the same reset condition as automated assertions. Both engines then passed without any runtime change. The initial report is retained beside the final production-smoke.json. The source tested/deployed remains f805125; the follow-up commit records deployment evidence only.

Rollback, if required, uses the previous Web image `diaperscoutprod-dsg9bgg6dkgkcwbs.azurecr.io/diaperscout-web@sha256:c0e873051e572bfbfdee9e09a195bafab4903b4df7495c8aba5ff583bb35afe9` through the same image-only process. No rollback was required.
