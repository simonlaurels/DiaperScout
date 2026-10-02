# Full-screen PWA barcode capture — 2 October 2026

## Starting state and scope

Clean fix/product-submission-recovery checkout at 0cc4936c97f8026b728bf823445db814335d4220. Fetched origin and safely pulled with --ff-only: already up to date. Search, Product Details and Available In were present before implementation. No local work was discarded, reset or stashed.

Only /scan's initial camera-capture presentation and lifecycle change. The desktop retains its explicit Scan barcode button/manual form. The existing BarcodeDetector retail-format recognition (EAN-13, EAN-8, UPC-A, UPC-E), UPC-E expansion and locally hosted ZXing BrowserMultiFormatOneDReader are retained. No new decoder package or external runtime dependency. The bundled decoder/license are unchanged.

## Presentation

A PWA-only fixed camera viewport fills the space above the existing bottom navigation. A ResizeObserver measures the actual existing nav height including its safe-area padding, while safe-area env values keep overlay controls away from protected edges. Video uses playsinline/muted and cover sizing. Controls use existing Inter/Lucide colours and corner treatment; minimal reticle/instructions float over the feed. The opening/permission/error state replaces a blank video without artificial delays.

The global navigation component is unchanged. Scan remains selected. Its exact existing nav CSS treatment is reused on /scan in wide standalone landscape because the inherited shell only displays mobile navigation below 701px. Other pages' wide-layout behaviour is not changed. The pending update banner is deferred during camera capture so it cannot cover scanner controls; it reappears normally afterwards. No service-worker behaviour or cache policy changes.

## Lifecycle, permissions and transitions

PWA capture opens the camera automatically after interactivity. Desktop camera startup remains explicit. Rear-facing camera preference and existing barcode formats remain. Generation guards cancel stale starts/detections in JS and the component. Pending getUserMedia calls are serialized, and a stream granted after cancellation is immediately released before another acquisition. Tracks, animation frames, decoder controls and video source are cleared on capture, manual entry, background/page hide, navigation/removal and disposal. Event/resize/mutation observers are removed on disposal. Resume uses the clear Try camera again action to avoid silently reopening a camera after backgrounding.

Torch appears only when the real active video track advertises support; applyConstraints changes the track, with failure handled without pretending success. Permission denial explains external browser/device settings. Unavailable/unsupported/initialisation and detector failures offer retry and manual entry. Manual entry receives focus after rendering.

Capture is accepted once per active session, immediately stops camera resources, displays finding-pack feedback and calls the unchanged ProductLookupClient. Found results preserve exact variant/pack links and observation route. Not-found results retain the existing Add product route and original barcode representation. Invalid/ambiguous/failure results keep existing validation messages and manual recovery. Scan another item starts a fresh capture session.

No Product/Pack/Review wizard, authentication/account/circuit identity, draft storage, proposal submission/idempotency, moderation, catalogue/variant/size/pack, observation or retailer logic changes. Existing authenticated and reauthentication/draft/Review regressions remain in the complete suite. Existing observation browser journeys now explicitly select manual entry; their downstream assertions remain intact.

## Verification

Scanner tests cover native permission/lifecycle states in deterministic Chromium/WebKit fixtures, 320/390/430 portrait and 844×390 landscape geometry, nav selection/height, torch capability, denial/unavailability/startup failure recovery, manual focus, known exact pack/variant, unknown contribution transition, duplicate detection, background/resume/navigation/re-entry and stale camera cancellation. Independent Chromium tests use actual browser MediaStreams with its fake-camera device, the unchanged bundled ZXing decoder and generated valid EAN-8 Y4M video frames. Those test frames are local fixtures, not production contributions or physical-device evidence. Desktop/manual and rapid permission-switch/detector-error/check-digit checks are included.

Initial focused iterations caught error rendering and an update banner obscuring a scanner control; both fixed. WebKit's deterministic camera fixture uses portable stream objects and media-element stubs; actual decoding is covered independently. Scanner/contribution/authentication focused run: 21 passed, 0 failed, 0 skipped (1 minute 54 seconds), before the final additional guards/checks. Final full suite/build results and any deployment will be added after completion.

## Outstanding physical iPhone acceptance

All checks below remain outstanding on the actual installed PWA; browser simulation does not prove them.

1. Open the installed DiaperScout PWA.
2. Tap Scan.
3. Confirm camera fills the usable screen above navigation, including portrait/landscape and safe areas.
4. Confirm navigation stays stable and central Scan is selected.
5. Confirm permission/startup transitions are clean and denied/unavailable recovery/manual entry work.
6. Scan a real barcode.
7. Confirm capture feedback/navigation occurs once and the camera indicator turns off afterwards.
8. Confirm the correct known-product/exact-pack or unknown-product journey opens.
9. For a genuine contribution, verify Product → Pack → Review.
10. Verify an already authenticated account is not unnecessarily asked to sign in and drafts/Review survive any needed authentication.
11. Background/reopen during pending permission, live scanning and after capture; resume deliberately.
12. Navigate away from Scan and return; test manual entry/camera switching and cancel as well.
13. Confirm camera reinitialises without stale/frozen or simultaneous streams; test torch only where the device advertises it.

## Test readiness adjustment

The first complete run passed 302 of 303 tests (Domain 51/51, integration 251/252). The sole failure was the existing WebKit Product Details test selecting a pack immediately after returning to the page, before restored interactive handlers were ready; the isolated recheck reproduced it. The test now waits for the existing gallery readiness marker before selecting the pack. All pack/observation assertions remain unchanged and Product runtime code was not modified. All three product-page cases then passed (20 seconds). The final complete run includes this test-only readiness adjustment.

## Evidence interpretation and deliberate boundaries

The local startup/ready screenshots use deterministic camera fixtures, not real camera imagery. Independent actual-browser MediaStream/ZXing video tests establish decoder wiring. Production validation will use read-only lookups and navigation, with simulated standalone/permission/native-detector signals explicitly distinguished from physical iOS evidence. No production contribution is submitted. Wide standalone navigation visibility is corrected only on Scan; inherited landscape navigation behaviour on other screens is outside this task. Backgrounding intentionally requires an explicit retry to resume camera capture.

## Final repository validation

Release build: `dotnet build DiaperScout.slnx --configuration Release --no-restore` — succeeded, 0 warnings, 0 errors, 7.03 seconds.

Complete suite: `dotnet test DiaperScout.slnx --configuration Release --no-build --settings docs/implementation/search-prototype-evidence/validation.runsettings --logger trx --results-directory ../diagnostics/scanner-validation/final-full` — Domain 51 passed (2 seconds); integration/browser 252 passed (9 minutes 45 seconds); total **303 passed, 0 failed, 0 skipped**. Existing serialized browser settings were reused; no assertions were weakened. All 12 scanner cases, Search/Product/Available In, authenticated contribution/draft/Review and proposal regressions passed. Earlier runs and final counters are recorded in scanner-pwa-evidence/test-runs.json.

## Production deployment and validation

Runtime commit **85f890c90132d963d9496dc31e532e434db1d88d** was pushed to the existing `fix/product-submission-recovery` branch. ACR build **db1k** succeeded using the unchanged Dockerfile and an archive of that commit's required source. Immutable Web image: `diaperscoutprod-dsg9bgg6dkgkcwbs.azurecr.io/diaperscout-web@sha256:c0e873051e572bfbfdee9e09a195bafab4903b4df7495c8aba5ff583bb35afe9`. Revision **diaperscout-web-vnet--scanner-85f890c** is Healthy / Running / Provisioned, 100% traffic, latest ready revision.

Only Web image and revision suffix changed. Full private Azure GET snapshots were compared; configuration, template excluding image/suffix, identity, environment, scaling and secret references all matched for both applications. API image/revision remained unchanged. Web remains min 0 / max 2, cooldown 600 / polling 30 with sticky affinity; API min 0 / max 10, cooldown 300 / polling 30. Shared Data Protection references, networking, Private Link, TLS and database settings were preserved. No migration or production data write. See scanner-pwa-evidence/config-preservation.json (booleans/metadata only; private snapshots are outside Git).

Final read-only live validation completed **2026-10-02 22:31:07 UTC**: Chromium 390×844 and WebKit 430×932 / 844×390 — **3/3 passed**, no browser errors, no fake contributions. Camera spans the viewport above the unchanged nav; central Scan is selected. Chromium uses an actual browser MediaStream from its fake-camera device with injected native-detector results; it does not prove physical camera recognition. Public GTIN **193183016187** resolves to MEGAMAX variant **bb3ad9fc-9dea-4f07-b179-5bc3f66b7d7e**, exact pack **d1862959-c74b-4c81-93d2-2f62058d1a59**. Camera tracks release after known/unknown capture, background and navigation; explicit resume/re-entry works. WebKit denied-permission/manual lookup and the unknown **96385074** contribution route open the normal existing wizard. Screenshots were inspected. Physical iPhone checks listed above remain entirely outstanding.

Initial smoke harness runs failed due to premature interaction with restored Product controls, an inappropriate gallery readiness marker on an image-free public product, exact-case mismatch in the contribution heading, and a non-null-safe enhanced-navigation wait. Those validation-only mistakes were corrected; deployed source did not change. Previous reports are retained as evidence; production-smoke.json is the final all-green run.

Initial full-repository deployment packaging exhausted local C: space before a remote build started. Incomplete task-generated packaging was removed safely; only required source was packaged. At the user's request, the generated build context and subsequent temporary/private deployment files were moved to **D:\Codex\DiaperScout-scanner-85f890c** and **D:\Codex\DiaperScout-scanner-temp-85f890c**. Existing user files were not removed.

Rollback, if needed, uses the previous unchanged Web image `diaperscoutprod-dsg9bgg6dkgkcwbs.azurecr.io/diaperscout-web@sha256:5c109ca8c9a72477c87d0f3168fb907f64e5891f0165f56cfc08857b0c7db68f` through the same image-only deployment procedure. No rollback was needed.

## Files changed

Exact repository paths, including validation evidence and screenshots, are recorded in scanner-pwa-evidence/changed-files.txt. Runtime changes are limited to Scan.razor, Scan.razor.js, scanner-pwa.css, App.razor stylesheet registration and the shared zap icon. Tests update scanner coverage, explicit manual entry in observation journeys, and Product Details readiness only. Other changes are handover/validation evidence. The final follow-up commit adds deployment documentation/evidence only; the deployed runtime remains 85f890c.
