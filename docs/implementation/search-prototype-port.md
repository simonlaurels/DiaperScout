# Approved PWA Search port — 2 October 2026

## Source and scope

Started from a clean fix/product-submission-recovery checkout, fetched and fast-forwarded from 96b41ff to cee0a6786b3a217b432a8037e387cc46f26a2ebb before implementation. The existing Product Details → Available In implementation was present and retained.

Authoritative reference: https://diaperscout-prototype.pages.dev/screens/search. Captured live HTML/CSS, five category PNGs and a 390×844 browser screenshot in search-prototype-evidence/reference. Asset hashes record exact source URLs. Reused the local Inter font already installed by the product prototype port, the prototype spacing/surfaces/category illustrations and Lucide SVG paths. Search CSS is scoped to this component; the desktop catalogue remains available at desktop widths.

The reference is a browse screen: it has no product result cards or working filter panel. Those necessary production states use the existing PWA product presentation language. Dummy recent queries are replaced by local device history; dummy brand names are replaced by real brand facets. The label is Brands because production has no popularity metric. Category buttons map to existing Tape/PullUp/AllInOne/Booster/Pad types. Global Products/Scan navigation is preserved as required, although the reference labels those items Search and uses a different central icon.

## Behaviour and architecture

Products.razor remains the owner of the existing ProductCatalogueClient request and six existing facet selections. PwaSearch is a presentation component, not a second catalogue endpoint. Query, sort and repeated facet values persist in the URL, preserving browser back/forward. Results retain canonical product and variant IDs and link to the existing product route. Available In routes and pack/variant semantics remain unchanged. Pagination uses the existing 24-item offset API and deduplicates product/variant pairs.

Loading, retryable failure, no results, missing/broken image fallback and working filters are implemented. Buttons have labels, selected/expanded states and touch targets. Search history is localStorage only, capped at six entries, with malformed/blocked storage handled. Existing public ImageUrl data is used. The labelled missing-image fallback can later accept the user's generic pack image; no unapproved manufacturer image or fabricated asset was introduced.

No API, domain, database, retailer workflow, product details CSS, service worker or infrastructure changes. The user also authorized reversing cee0a67's unsuccessful blue startup colour experiment: startup root/panel and theme/manifest return to cream, while Welcome's own blue design remains. This does not claim control over the native iOS status area.

## Verification

New browser tests cover Chromium at 320/390 and WebKit at 430, real published catalogue/variant navigation through Product Details and Available In, browser back with query/filter retention, real local history, image fallback/success, loading/retry, 52 results across three pages, and global navigation. Existing mobile browser assertions were updated for the visible Search presentation without dropping their route/cache/variant checks.

Seven Search/public variant focused cases passed before the colour reversal. Five startup-colour failures were reproduced on untouched cee0a67 and traced to its conflicting early/later styles and outdated cream assertions; the user chose to reverse that experiment. An initial final run without local Docker access could not initialize database fixtures; it is not an application test result. Final complete-suite/build results and deployment evidence will be recorded below after completion.

## Acceptance limits

Browser standalone emulation and WebKit do not reproduce an installed physical iPhone container. Physical checks remain: fresh/existing installation, actual native top area, splash → Welcome → Search, home-indicator clearance, keyboard and touch, and Search → Product Details → Available In → Back. No physical-iPhone acceptance is claimed.

## Final repository validation

`dotnet build DiaperScout.slnx --configuration Release --no-restore`: succeeded, 0 warnings, 0 errors, 25.24 seconds.

`dotnet test DiaperScout.slnx --configuration Release --no-build --settings ../diagnostics/search-validation/serial.runsettings --logger trx --results-directory ../diagnostics/search-validation/serialized`: Domain 51 passed (2 seconds); Integration 240 passed (8 minutes 31 seconds); **291 passed, 0 failed, 0 skipped**. The run settings disable test-class parallelism only; no assertions are skipped or weakened.

Two parallel complete runs each passed 289/291, with different existing timing-sensitive browser failures (Welcome, passkey, product observation selection). All seven Welcome/passkey rechecks passed separately; the final serialized complete run passed every case. Exact earlier outcomes and final counts are in search-prototype-evidence/test-runs.json. `git diff --check` passed. ProductCatalogueScreen, product-prototype.css and service-worker source remain unchanged from cee0a67.

Changed paths are listed in search-prototype-evidence/changed-files.txt. Deployment evidence will be appended after the image-only rollout and read-only production smoke.

## Production rollout and handover

Runtime commit **c2f54f3** was pushed to the existing GitHub fix/product-submission-recovery branch. Registry build **db1j** succeeded from a clean Git archive of that commit. Web image **diaperscoutprod-dsg9bgg6dkgkcwbs.azurecr.io/diaperscout-web@sha256:5c109ca8c9a72477c87d0f3168fb907f64e5891f0165f56cfc08857b0c7db68f** is deployed as **diaperscout-web-vnet--search-c2f54f3**, Healthy / Running / Provisioned, 100% traffic; latest revision and latest ready revision match.

Final read-only production Chromium (390px) and WebKit (430px) smoke passed at **20:56:50 / 20:56:57 UTC on 2 October 2026**. Both showed six real MEGAMAX variants, exact matching prototype PNG hashes and Inter 27.2px headings. Both traversed Search → NorthShore MEGAMAX Black → Available In → Back, preserved variant bb3ad9fc-9dea-4f07-b179-5bc3f66b7d7e and pack d1862959-c74b-4c81-93d2-2f62058d1a59, retained the query, exercised category and real facets/no results/Scan, and reported zero browser errors. Production writes: **0**. Browse/results screenshots were visually inspected. The initial smoke used an incorrect accessible name for the existing Scan link; the final script uses its existing href. A temporary extra size-reselection probe read state before its async update completed; it was removed from this unchanged original-journey check. No product-selection code was changed.

Public production HTML/manifest also confirm the reverted cream theme/background and removal of the early standalone-blue override. Welcome retains its blue design. The existing service-worker update banner was visible during live result capture and is intentionally preserved.

Complete private Azure GET snapshots were compared using the existing config comparison script. Sanitized proof is in search-prototype-evidence/config-preservation.json: configuration, template except image/revision suffix, identity, environment, scaling and secret references all match for both applications. API image is also identical. Web remains min 0 / max 2 / cooldown 600 seconds; API remains min 0 / max 10 / cooldown 300 seconds. No networking, session-affinity, DataProtection, policy, authentication or migration changes.

Rollback is an image-only Web update to **diaperscoutprod-dsg9bgg6dkgkcwbs.azurecr.io/diaperscout-web@sha256:2e4d2e07ddd1be41d8bfa49ea0c1fa9e3c09f5242516323554f417e1709377c6** using a fresh revision suffix and preserving configuration. That is the exact pre-Search production image. Documentation/evidence additions after runtime c2f54f3 do not require rebuilding or redeploying the application.

Outstanding acceptance is the physical-iPhone checklist above. The generic pack image is future user-supplied artwork; current Search has a clearly labelled fallback.

A separate follow-up production check waited for Medium to become the current size and for the rendered pack selector/Available In href to agree, then navigated to Available In. Both Chromium and WebKit passed with Medium pack **a17f3fa3-a690-4224-9fad-8c84902ebe90**. This resolves the temporary extra probe's premature assertion: its earlier pack d1862959-c74b-4c81-93d2-2f62058d1a59 was the preceding selection. The follow-up script and results are saved as size-selection-smoke.cjs/json; no application changes were needed.
