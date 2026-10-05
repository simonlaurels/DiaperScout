# Geoapify Scan / Record a Discovery implementation

Implementation and production release record, 5 October 2026. Final deployment evidence is linked below.

Starting checkout: `/Users/sporter/Documents/Projects/DS`, branch `fix/product-submission-recovery`, HEAD `1bf717d099de160c98d784e0b78af1c9eff07583`. HTTPS read verified the branch matches GitHub; `main` is an older ancestor. Original untracked Google assessment/ADR preserved and ADR updated under this task. No unfinished importer work was present or included.

## Storyboard comparison and changes

The approved exact-pack result already has left-side image/right-side details, supporting Product Details/Where to Buy and primary Record a discovery; leave its recognition/camera code intact. Existing discovery forms lost the short nearby picker, exact-pack review imagery and bespoke completion scene, and exposed manual shop entry. Replace the picker with nearby provider results and a quiet missing-shop explanation/radius retry. Use exact-pack imagery, optional price, review and immediate success. No availability-status field or observation moderation added.

Unknown product's working evidence/photos/double-check/review/moderation architecture remains. Its optional discovery uses the same place picker. Bespoke completion artwork distinguishes public known-pack discovery from privately submitted packaging evidence. Fox is a stationary plush, not a live animal. Guide/van designs follow supplied character sheets. Functional forms remain uncluttered.

## Data and privacy

One additive nullable `ProviderSnapshotJson` JSONB column on Locations; existing rows/keys unchanged. Server-side provider mapping accepts OSM/ODbL provenance, stores immutable selected place snapshots, and does not retain raw device coordinates. Snapshot identity uses content hash; exact retries reuse it, changed historical meaning uses a new Location. Existing native places coexist and administrator maintenance remains available behind persisted editorial authorization.

Nearby provider requests use POST to a constant URL and `x-api-key`, avoiding secret/location query strings. Provider and contribution HTTP loggers are removed; request bodies are not logged. Selection payloads contain place metadata and expiry, authenticated with a key derived for this purpose from server-side provider configuration. Key rotation invalidates unsaved tokens; users reselect. Browser drafts contain selected place data only, never device positions. Tokens expire after 30 minutes.

Five single-category queries with limit 20 (five provider requests per search, nominally five credits at the researched Places rate; dashboard billing is not independently measured), overall 12-second deadline; 1 km initial radius and explicit 3 km expansion. Merge by datasource/source identity, preferring complete address, preserve distinct neighbouring places. Incomplete/unsupported-source records are excluded. Result caps, stale data and missing specialists are known limitations. Geoapify unavailable means friendly selection failure, not manual creation or historical-view failure.

Public licensed-place export at `/places/open-data` contains only sourced snapshots attached to qualifying public observations. Accounts, product data, observation details and pending private evidence are excluded. OSM/Geoapify attribution and ODbL source links appear alongside Atlas/picker. See ADR-0012 for boundary and rollback policy.

## Automated and live integration validation

The full serialized suite passed **389/389**: 64 domain and 325 integration/browser, no failures or skips. The initial concurrent suite had six browser hydration/navigation timeouts; assertions were preserved and the complete serialized rerun passed. Subsequent checks passed: 26 provider/browser tests; four final snapshot-history/mobile checks; two action-spacing checks; nine permanent-attribution/observation browser checks. These are overlapping verification runs, not extra unique test counts. Release build: zero warnings/errors. [Summary](geoapify-discovery-evidence/test-summary.json).

Coverage includes individual queries, source-identity deduplication, distinct neighbours, unsupported/malformed records, safe provider failure/cancellation, expired/forged selections, read-only nearby search, immutable historical snapshots when a provider place moves, unpublished snapshot export exclusion, existing native places/admin management, authenticated observation/retry/optional price/immediate Atlas, denied-location UX, camera/manual capture and unknown evidence/moderation/approval/merge/rejection/idempotence. Existing auth/catalogue/Atlas tests remain intact.

A protocol check of POST/header requests succeeded for all five categories around public Yate coordinates. A second bounded integration check ran the actual committed mapper/selection code: **30 mapped candidates**, five requests, **2,105 ms**, ODbL provenance and all signed selections verified. Neither check wrote database data or used a private device position. This was implementation validation, not another provider benchmark. [Evidence](geoapify-discovery-evidence/live-client-validation.json).

## Release and production

Implementation commit `f5ae02e`; compact mobile completion correction `6307fdc`; permanent Atlas attribution correction `abbb333`. Built from immutable archives of those commits. API/migration use `f5ae02e`; final Web uses `abbb333`. ACR builds: API `db2h`, migrations `db2j`, final Web `db2p`, temporary read-only verification `db2m`. An earlier Web build `db2k` was superseded; the spacing-only deployment `db2n` was superseded by the permanent-attribution correction.

Only application image/revision updates and API-only Geoapify secret/reference were added. Public Web/internal API, existing credentials, identity, networking, Npgsql VerifyFull, shared Web Data Protection, sticky affinity, resources and scaling were preserved. No provider key enters Web configuration. [Configuration comparison](geoapify-discovery-evidence/configuration-comparison.json) and [revision health](geoapify-discovery-evidence/revision-health.json) confirm preservation and healthy, provisioned revisions: API `diaperscout-api-vnet--geo-f5ae02e`, Web `diaperscout-web-vnet--geo-abbb333`, both receiving latest-revision traffic. [Image digests](geoapify-discovery-evidence/images.json).

Migration execution `ds-bootstrap-admin-hmcvphz` succeeded at 15:55:00–15:55:34 UTC. Logs show only `20261005150341_ProviderPlaceSnapshots` applied and `Done.`. One nullable JSONB column was added; no historical Location or Observation was rewritten. The execution override preserved the persistent job configuration. [Migration](geoapify-discovery-evidence/migration-summary.log), [job preservation](geoapify-discovery-evidence/job-preservation.json).

Read-only execution `ds-bootstrap-admin-f2uicl5` succeeded at 15:56:49–15:57:24 UTC: fresh authenticated connection, **VerifyFull**, **TLS 1.3**, new migration confirmed, initially zero provider snapshots. [Database evidence](geoapify-discovery-evidence/database-verification.log). Catalogue/product requests after API restart also exercise the new API's database path.

Simon confirmed on his physical iPhone that the production nearby-shop list works. This verifies the authenticated production provider route/configuration. No production observation, product proposal, photo upload or account was fabricated by automated smoke checks. Native camera permission behaviour, native passkey prompts, a genuine final observation submission and real unknown-product moderation remain separate human acceptance checks; automated isolated tests cover their application semantics.

Screenshot review caught stretched completion buttons and corrected them before final acceptance. The first live smoke caught provider attribution incorrectly limited to the map-error paragraph; the final Web correction moves it to the permanent footer and nine browser tests verify its visibility. These findings are retained rather than treating the initial checks as clean. [Known success](geoapify-discovery-evidence/discovery-success-430.png), [private product evidence success](geoapify-discovery-evidence/proposal-success-430.png), [review](geoapify-discovery-evidence/discovery-review-430.png).

Final read-only [production checks](geoapify-discovery-evidence/production-smoke.json): **21 passed** across Chromium 390×844 and WebKit 430×844, including public catalogue/product access, Atlas/attribution, sign-in and Backpack, manual unknown-barcode/photo-entry, licensed-place export and exact artwork hashes. No browser errors or horizontal overflow were observed. Production Atlas returned an empty discovery set during automated checks; existing native/history compatibility was verified in isolated tests rather than claiming real historical pins were observed. [Sampled logs](geoapify-discovery-evidence/log-summary.json) contained zero detected DNS/TLS/database failures.

Commits are local; HTTPS push lacks credentials and SSH needs Simon’s passphrase. GitHub still pointed to starting HEAD at final verification. No source or deployment change was withheld because of that independent GitHub-authentication limitation. The working tree contains only the earlier untracked Google assessment material after the evidence commit; it was preserved and excluded from release.

## Limits and rollback

Geoapify/OSM can miss specialist shops and retain stale POIs, including the corrected historical Bush Healthcare Yate record. The bounded nearby list is not exhaustive; availability claims are dated Explorer evidence, not guaranteed inventory. An outage blocks choosing new places, while historical Atlas and catalogue views remain stored-data backed. Five category requests are a deliberate low-usage trade-off; monitor credits before increasing request volume.

Rollback restores prior API/Web image revisions recorded in [rollback images](geoapify-discovery-evidence/rollback-images.json); retain the additive snapshot column/data and secret. Do not run Down, remove historical snapshots, change networking/TLS or alter authentication/affinity/Data Protection to roll back this application release.

## Approved indoor artwork style correction — 5 October 2026

Simon approved a revised indoor Wanderer illustration that matches the outdoor Guide's anime facial proportions, linework, hair and pullover hoodie. The stationary plush Fox and private packaging-evidence scene remain. Runtime commit `fa1da98` changes only `product-evidence-recorded.webp`; no application logic, API image/configuration or migration changed. ACR build `db2q` succeeded; Web revision `diaperscout-web-vnet--art-fa1da98` is Healthy/Provisioned.

[Release](geoapify-discovery-evidence/guide-art-release.json), [configuration preservation](geoapify-discovery-evidence/guide-art-configuration-comparison.json), and [five live checks](geoapify-discovery-evidence/guide-art-live-checks.json): public Explore, Products, sign-in and place-data export returned 200, and the production WebP exactly matched the approved release. The PWA service worker does not cache this illustration. The original outdoor artwork is unchanged. GitHub push still requires local SSH authentication.
