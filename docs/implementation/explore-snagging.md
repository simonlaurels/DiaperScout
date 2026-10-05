# Explore snagging — 5 October 2026

This focused pass starts at `344361e` on `fix/product-submission-recovery`. The only existing untracked work was the Google Places assessment and its evidence directory; neither is included. GitHub's remote branch matched that starting commit. No importer, Scan, Atlas, Geoapify, authentication, infrastructure or database configuration changes are included.

The earlier [Explore delivery report](explore-screen.md) describes the original release. This document supersedes its hero/paper-introduction, naming and newest-sort limitations only; recent-product eligibility and nearby-discovery behaviour are retained.

## Presentation and artwork

A plain Explore heading precedes a new panoramic Guide-with-product scene. The hero displays the whole 3:1 image, approximately 117–135 px tall across the tested portrait sizes, rather than clipping a tall illustration. The previous hero was 205 px. The Guide examines an unbranded sealed absorbent-product pack; her stationary plush Fox sits in the cream backpack. Canonical anime styling, hat, short brown hair and teal pullover remain intact. The scene is illustrative, not invented catalogue photography.

Built-in image generation produced the bespoke asset, using `pwa/discovery-recorded.webp` only as the character/style reference. The first composition clipped the hat; a second generation pulled framing back so the whole hat fits. The final source was exported using Pillow/Lanczos into `src/DiaperScout.Web/wwwroot/pwa/explore-products-v1.webp`, 1200 × 400, WebP quality 85/method 6, 139,708 bytes. No new image delivery infrastructure. Explicit dimensions reserve its aspect ratio; the hero has high fetch priority; product images remain lazy. Its versioned filename avoids stale artwork reuse.

Final generation prompt: “Make one composition correction to this approved-style panoramic DiaperScout hero: pull the scene framing back very slightly so the ENTIRE top of the Guide's tan hat fits comfortably inside the image with a small margin of sky above. Keep 3:1 wide aspect ratio and preserve the same character, curious closed-mouth expression, plush fox in cream backpack, hands examining the sealed cream/teal absorbent pack, detailed warm woodland store and lake setting, crisp anime drawing and watercolor textures. No typography. Face and pack remain prominent and legible. Do not clip hat or hands. Preserve everything else.”

Cream surfaces, 16px radii, subtle teal/grey borders and Product Details shadow treatment replace the heavier tan styling. Cards have more usable width, contain-sized images, defined light-image boundaries, wrapping names and honest missing/failed-image fallbacks. Nearby presentation and explicit location request/privacy controls retain their functionality.

## Names and View all

`RecentProductsAsync` previously returned raw `Product.Name`; Search and Product Details already used `PublicProductIdentity.DisplayName`. Explore now reuses that shared brand-first composition. Canonical storage, slugs and identifiers are unchanged. Existing overlap handling avoids repeated brand/name components. Explore remains a parent-product discovery strip, with its existing deterministic canonical-variant detail link; it does not invent pack/variant labels. Tests cover Crinklz Original, NorthShore MEGAMAX, ABENA Slip Premium Special, TENA Slip Plus, Seni Active Classic, overlap and structural variant-name cases.

View all links to `/products?sort=newest`, using the existing Search/results component and URL state. An explicit non-default sort displays results even with no query/filter, and the selected sort is visible beside results as **Newest added**. Back from Product Details restores the sort/results, then Explore.

The previous newest sort ordered random Product IDs. It now orders the earliest existing ProductCreated audit per product descending; later edit audits do not refresh it. Multiple variants share their parent product's date. Undated legacy current products remain in the full results, after dated products, without a fabricated chronology. Recent-product eligibility remains unchanged: current product, canonical variant, trustworthy creation audit, bounded to eight distinct parents. No schema/migration or new publication model.

## Navigation line root cause

Production computed-style inspection identified `.pwa-mobile-nav`'s `border-top: 1px solid #dfd7c7` from `css/pwa.css` at the navigation edge. This is the thin tan/brown line. A selector scoped to installed Explore removes the actual border (`border-top: 0`); no cover, clipping, colour matching or negative margin. Product Details/Search and other pages retain the shared separator. The shared navigation component, safe-area padding, background and shadow are unchanged.

## Verification

Release build: zero warnings/errors. Final focused test selection passed **38/38** (14 identity cases, 24 integration/browser cases). Coverage includes recent chronology/public eligibility/images, full newest chronology with undated products, anonymous browsing, exact links, empty/error/retry, no automatic geolocation, long/light-image cards, four viewport sizes, existing search/variant catalogue tests, real View all → product → back → Explore in Chromium and WebKit, and an assertion that Product Details retains its 1px navigation border. The first targeted run exposed a test assumption that a shared fixture contained only one recent product; the assertion now targets its own canonical variant. No assertions were weakened.

Rendered fixture captures are synthetic test data, never production records: [390px](explore-snagging-evidence/fixture-390.png), [375px short viewport](explore-snagging-evidence/fixture-375.png), [430px](explore-snagging-evidence/fixture-430.png), [wide](explore-snagging-evidence/fixture-wide.png). Portrait checks place the complete first card above navigation; landscape/wider rendering and unchanged ordinary-browser Home were checked. Scroll-end action clearance, contained images and absence of horizontal overflow are asserted. Native installed-iPhone safe-area rendering remains a human check; WebKit emulation is not a physical device test.

Additional catalogue/Product Details/shared-PWA regression results and production delivery evidence follow below after completion. No production records are created by the smoke checks.

Additional regression selection passed **87/87**: catalogue APIs, Product Details browser flows, variant catalogue APIs, shared PWA and Welcome/iOS browser checks. This selection overlaps some of the focused cases; the counts are separate runs, not a claimed unique total. [Focused log](explore-snagging-evidence/targeted-tests.log), [regression log](explore-snagging-evidence/regression-tests.log), [build log](explore-snagging-evidence/build.log).
