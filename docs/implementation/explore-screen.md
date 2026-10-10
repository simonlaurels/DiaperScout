# Installed-PWA Explore

## Scope and baseline

Work began from clean `fix/product-submission-recovery` commit `2d5d788`, following the Atlas production delivery. GitHub fetch confirmed HEAD and the remote branch agree. The reference is the user-supplied `FCF4C8AA-FFE0-45C7-9FE6-E6F4CE65619B.PNG`; the accompanying Explore brief defines this task. Parent `sources/` remains read-only. Temporary/test/evidence storage is on D:.

The existing Home screen is a static search-and-destination landing page, with a separate anonymous Welcome overlay. Catalogue browsing is variant-oriented; current canonical Products are the public eligibility boundary. Canonical Product has no creation/release timestamp. Existing search's `newest` ordering uses random Product IDs and cannot establish chronology. Canonical publication/creation already writes `CatalogueAuditAction.ProductCreated` with `OccurredAtUtc`. Submission creation/update dates are not publication dates. This implementation preserves catalogue semantics and does not change Search's ordering.

## What changed

An InteractiveServer `PwaExplore` component presents a compact paper introduction, horizontal recent-products strip and nearby-discoveries panel. It appears only in installed-PWA mode. The existing standard browser/desktop Home markup remains present and is shown outside standalone mode. Existing anonymous Welcome, fonts, bottom navigation labels/icons/layout/active-state handling and shared startup/authentication logic remain unchanged. A scoped standalone selector hides the conventional application header on Explore. No separate navigation was created.

The hero is approximately 205px tall at the tested portrait sizes, with a paper title and canonical `/pwa/guide-map.webp`. This asset is the existing young Guide and plush Fox map pose, whose provenance is documented in [PWA experience](pwa-app-experience.md) and [Atlas](atlas-canvas.md): `docs/branding/pwa/guide-map-source.png`, canonical Guide sheet `7ABC2EE7`. The repository's supermarket illustration depicts an older Guide and was deliberately not substituted for the canonical character. No Guide or product artwork was generated, changed or extracted from the reference.

Panels use established cream surfaces, green/teal tokens, Inter font, rounded borders, restrained shadows and 44px controls. Product images use fixed square presentation areas and `object-fit: contain`, preserving intrinsic aspect ratio. A missing approved image gets an honest neutral “Image to come” placeholder, rather than invented pack artwork. The strip supports horizontal touch/keyboard scrolling and remains well formed with one product or no products. Nearby rows use place pictograms because the existing observation projection has no approved image URL.

## Data additions and meaning of “new”

The only API addition is anonymous read-only `GET /api/v1/explore/recent-products`, implemented on existing `IAtlasQueries`, with an additive `RecentCatalogueProduct` DTO and `ProductCatalogueClient.RecentAsync`. No schema/migration, activity-feed architecture, write endpoint or release-date model was added.

The endpoint returns at most **eight distinct current canonical products**, with at least one canonical variant and an existing ProductCreated audit. The earliest creation-audit timestamp for each product supplies `AddedAtUtc`; results order by that timestamp descending, with Product ID as a deterministic tie-breaker. A later ProductChanged audit does not make an old product new again. A deterministic first canonical variant (name, then ID) supplies the existing Product Details deep link. Draft/review submissions have no canonical creation audit and are not enumerated. Prototype/discontinued products and legacy products without trustworthy creation evidence are excluded from this section; the full catalogue remains accessible.

This is **most recently added to DiaperScout**, not recently released in the real world. There is no invented date or age window. The public image query uses existing public catalogue eligibility and precedence: published-to-product image association, Public visibility, primary first, role, creation time. Existing model metadata derives Public visibility from permission granted/not required and known source type. Private/unapproved image records do not become public through Explore. Audit actors, payloads and editorial metadata are never returned.

## Nearby selection and location

Explore calls the existing `PlaceObservationClient.AtlasAsync` only after a user explicitly taps **Use my location** and gets usable coordinates. It does not display another map or seed activity. The existing Atlas projection and qualification rules are unchanged: public commercial place with coordinates, exact pack/size/variant/current product joins, RetailAvailability observations in Submitted or Accepted state. Shop search, retail listings and unobserved candidates do not supply discoveries. The existing global cap of 2,000 observations is preserved, so Explore is a view of current loaded reports, not a completeness guarantee for every area.

The presentation selects reports within **25 miles**, computes approximate straight-line miles with the Haversine formula, then ranks by observed time descending, distance ascending and observation ID. At most three rows appear; future-dated reports are omitted from this dated-recent presentation. Each row displays exact-pack product identity, actual place, computed approximate distance and an absolute observation date. Rows link to existing `/atlas?locationId=…` details; product-strip links use `/products/{slug}?variantId=…`. Dated-report wording explicitly avoids implying current stock.

Geolocation uses one `getCurrentPosition` call per explicit action, a 10-second timeout and up to 60-second browser position cache. There is no automatic permission prompt, watcher, permission polling or saved location preference. Nonfinite/out-of-range coordinates or accuracy worse than 5km are rejected rather than showing invented distances. The position is transient component state and is not written to application storage/database or sent to the Atlas API as a query. Because this is InteractiveServer, the chosen position travels over the existing authenticated/anonymous Blazor circuit for distance calculation; it is not claimed to remain solely on-device.

Denied, unavailable or imprecise location gives a nonblocking message and explicit retry. Recent products keep working. Product and observation loading/failure states are independent, with explicit retries. Zero nearby reports produce “It’s quiet around here…” encouragement, the current-report/radius limitation, and the existing Scan route. The Guide appears only in the hero.

## Visual validation and differences from the reference

Screenshots are generated from isolated test fixtures, including a deliberately labelled synthetic TEST PACK image; they are not production data or real product artwork. Portrait Chromium 390×844 and WebKit 430×932 and short WebKit landscape 844×390 exercise the installed-mode rendering. The supplied concept was compared against actual rendered captures for hero height, first-screen density, product contain sizing, horizontal overflow, nearby hierarchy and the unchanged bottom navigation. WebKit viewport emulation is not physical iPhone installation verification.

Remaining differences are deliberate consequences of available assets/data: canonical map-pose Guide replaces the reference's pharmacy scene; there are no speculative Community signposts; no fake product names/pack imagery; a neutral placeholder appears when approved images are absent; nearby rows use place icons rather than unprovided product images; dates are absolute and distances are approximate; location is requested through an explicit control before nearby rows appear. No new real-world product-release claim or community feature was introduced. The pre-existing standard-browser/desktop layout and navigation breakpoints are retained for later dedicated work.

## Files changed

* Application: `Explore.cs`, `Contracts.cs` (read contract).
* Infrastructure: `Explore.cs` (bounded recent-product projection).
* API: `Program.cs` (one GET route).
* Web: `Components/Pages/Home.razor`, `Components/Shared/PwaExplore.razor`, `Components/App.razor`, `Services/ProductCatalogueClient.cs`, `Services/ExploreDiscovery.cs`, `wwwroot/js/explore.js`, `wwwroot/css/explore-pwa.css`.
* Tests: `ExploreApiTests.cs`, `ExploreBrowserTests.cs`; installed-page selectors in `PwaWelcomeBrowserTests.cs` now target `.pwa-explore` instead of the shared `.explore-page` class on both responsive views.
* This report and Explore visual/validation evidence.

Search, Product Details, Available In, Scan, Atlas qualification, canonical assets, fonts and bottom-nav implementation were not modified. The implementation phase made no production writes or infrastructure changes; subsequent deployment was explicitly authorized and is recorded below.

## Verification

Final Release build: **0 warnings, 0 errors**, **6.92s**; [build log](explore-evidence/release-build.log). `git diff --check` is clean.

The first targeted attempt could not initialize Docker and failed all 45 selected cases before application checks. After Docker started, the rerun passed **42/45** in **2m14s**; three location-test mock assignments were inadvertently invoked by Playwright's evaluation API. Corrected installation of those mocks exposed a second test-only timing issue when replacing a mock before the preceding request finished; each permission scenario now starts from a fresh document with product loading completed.

Full solution regression ran **348 cases**: **61/61 Domain passed** (3s), integration/browser **279 passed / 8 failed / 0 skipped**, **287 total**, **15m10s**. Three failures were the location mock sequencing above; five were existing Welcome tests using the now-ambiguous `.explore-page` selector for both responsive views. Those assertions now target installed `.pwa-explore`. No runtime source was changed after that full run.

Final targeted rerun (`FullyQualifiedName~Explore|FullyQualifiedName~PwaWelcomeBrowserTests`) passed **31/31**, **0 failed / 0 skipped**, **2m4s**. It includes all seven new Explore cases, all eleven Welcome cases and thirteen other existing cases matched by the Explorer/name filter. Every full-run failure was checked against the successful final test names. **All 348 distinct cases have passing coverage across the full regression and corrected rerun**; the entire full suite was not repeated after test-only corrections. [Exact validation summary](explore-evidence/validation-summary.json).

Coverage includes trustworthy creation chronology rather than random IDs or later edits; one parent product despite multiple variants; excluded prototype/discontinued/undated products; public versus unapproved images; valid existing Product Details links; actual Atlas qualification regressions from the full suite; radius/time/distance ranking; rejection of bad coordinates/accuracy; no automatic location request; denied/unavailable location without broken catalogue; populated and quiet areas; independent API failures/retries; one/zero recent products; horizontal scrolling, contained images, headerless installed layout, single unchanged active navigation, portrait bottom-nav clearance; unchanged browser Home; real authenticated Welcome bypass, guest continuation and blocked storage.

Inspected fixture captures (synthetic TEST PACK is explicitly labelled and never production data):

* [Installed Chromium 390px, before location permission](explore-evidence/explore-location-390.png)
* [Installed WebKit 430px, qualifying discovery](explore-evidence/explore-populated-430.png)
* [Installed WebKit 430px, quiet area and Scan encouragement](explore-evidence/explore-empty-430.png)
* [Installed Chromium 390px, denied location](explore-evidence/explore-denied-390.png)
* [No recently dated products](explore-evidence/explore-no-recent-products.png)
* [Short WebKit landscape, retained responsive behavior](explore-evidence/explore-populated-844.png)

The implementation was initially delivered as tested local commit `6f530135e0a6943558a1374da0913d0aa9e60bdd`. The user then explicitly requested “commit and deploy please”, authorizing the GitHub push and deployment below.

## Production deployment

On 3 October 2026, runtime commit **6f53013** was pushed to `origin/fix/product-submission-recovery`. An immutable archive of that commit supplied both successful ACR builds: API `db1r`, Web `db1s`, tag `explore-6f53013`. No database migration was needed or run.

API was deployed and verified healthy before Web. Both revisions are Healthy/Provisioned with 100% traffic:

| App | Revision | Immutable digest |
| --- | --- | --- |
| API | `diaperscout-api-vnet--explore-6f53013` | `sha256:9ae4cd9a89cadfa619375f98e5c9470461178afaf6c137d544b35c7ba12c16e1` |
| Web | `diaperscout-web-vnet--explore-6f53013` | `sha256:b15219705e5f84a986d156538f8c79eb76dadfc340b3a49e3c396e0bb1f883ca` |

Images use `diaperscoutprod-dsg9bgg6dkgkcwbs.azurecr.io/diaperscout-api` and `/diaperscout-web`. [Before/after configuration comparison](explore-evidence/config-comparison.json) confirms all checks true: configuration, identity, environment, scaling, secret references and template fields other than image/revision suffix remain unchanged. Networking/authentication settings were preserved. Private baseline snapshots remain on D: and are not committed.

Read-only production smoke passed **2/2**, Chromium 390×844 and WebKit 430×932, from **23:29:24 to 23:29:40 BST**. Each loaded two real recent products, a 205px hero, canonical Guide, no conventional installed header, one unchanged navigation with Explore active, no page overflow, no automatic location call, and a healthy existing MEGAMAX variant destination. Explicit mocked permission denial preserved the product section without repeat prompts. Both had zero page errors. No accounts, observations, category changes or other production records were written. Populated/quiet nearby states remain covered by isolated fixtures; live location was not collected. WebKit is emulation, not physical iPhone verification.

* [Production results](explore-evidence/production-smoke.json) and [read-only smoke script](explore-evidence/production-smoke.cjs)
* [Production WebKit capture](explore-evidence/production-explore-webkit.png)
* [Production Chromium capture](explore-evidence/production-explore-chromium.png)

Rollback is an image-only restore to the prior Atlas digests recorded in the configuration comparison. There is no schema rollback. Deployment evidence is committed separately from the tested runtime source.

Reproduction: `dotnet test DiaperScout.slnx --configuration Release --no-restore --settings docs/implementation/search-prototype-evidence/validation.runsettings`; `dotnet build DiaperScout.slnx --configuration Release --no-restore`. Use existing Docker/PostgreSQL and Playwright runtime, TEMP/TMP on D:, Docker Linux-engine endpoint, and optional `DIAPERSCOUT_BROWSER_EVIDENCE` output directory.
