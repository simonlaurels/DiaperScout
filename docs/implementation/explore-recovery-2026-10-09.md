# Approved Explore recovery — 9 October 2026

Fresh Mac checkout from remote `fix/product-submission-recovery`, verified baseline `b8e9af1625826b391ddf7ea7c1f956f55d7cf176`. The older Mac checkout and its two untracked Google Places assessment entries were preserved. Existing recovery receipts/caches were not removed.

Historical reference fetched and verified: `6e11844e0b54685aad9479aafc2c4058e6f6dc13`, remote `recovery/explore-approved-2026-10-05-6e11844`. Inspected with read-only git show/diff; no historical commit cherry-picked. Recovered artwork is the exact 139,708-byte historical blob.

Restored installed-only plain heading, panoramic Guide artwork, compact cards/panels, contained pack images with failure placeholders, and Explore-only navigation divider style. Brand-first recent names use the current public identity helper without changing stored names. Recent products still use canonical creation evidence, current status, one card per product and deterministic variant deep links. View all opens newest-added catalogue results; newest ordering uses creation audit evidence and retains undated products afterward. Current variant count/name handling and reviewed grouping implementation remain intact. The search results sort control remains available without filters.

Desktop Product Details recovery is unchanged. No Home, discovery/Geoapify, auth, scanner, schema, production configuration or infrastructure implementation was replaced.

## Validation

- Domain tests: 67 passed, 0 failed.
- Targeted Explore, public variant catalogue and Product Details recovery: 62 passed, one retailer-creation browser timeout. Isolated retailer timeout rerun and updated ordinary-Home comparison: 2 passed, 0 failed. The initial transient failure is retained in the log.
- Protected-function regression selection: 44 passed, 0 failed (reviewed grouping, Geoapify, scanner/discovery, observations, onboarding and contribution identity, plus installed Explore screenshots).
- Chromium/WebKit installed Explore checks at 375, 390, 430 and 844 landscape: plain heading/artwork, no horizontal page overflow, contained pack images, long names, product deep links, newest navigation/back, nearby location opt-in, denied/unavailable/empty/error/retry states.
- Ordinary browser Home compared at 1280 desktop and 390 mobile; existing homepage retained. Product Details recovery suite checked desktop 1280/1440, mobile and installed 375/390/430 and installed 1280, gallery, variant/exact-pack selection, scoped offers and empty states.
- Screenshots manually reviewed against the approved October 5 reference, including 375/430 and landscape Explore, desktop Home, desktop Product Details and installed mobile Product Details. Evidence in `explore-recovery-evidence/`.
- Final diff reviewed for focused scope; `git diff --check` passed.

Physical iPhone/native installed validation remains outstanding; installed mode here is browser emulation. No production deployment or data/infrastructure changes performed. Deployment requires separate user approval.
