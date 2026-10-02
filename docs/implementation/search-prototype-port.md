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
