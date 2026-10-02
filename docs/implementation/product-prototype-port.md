# Approved Product Details → Available In port

The visual specification is the exact [Product Details](https://diaperscout-prototype.pages.dev/screens/product) and [Available In](https://diaperscout-prototype.pages.dev/screens/retailers) pages, inspected on 2026-10-02. The HTML, stylesheet, computed typography/geometry/icon inspection and 390px reference screenshots are preserved in [reference evidence](prototype-port-evidence/reference/). This change ports those two screens into the existing production catalogue, without changing the scanner, authentication, contribution flow or infrastructure.

## Visual fidelity

The product/retailer sections of the original `coastal-explorer.css` are scoped under `.pwa-prototype`, retaining their original values, media queries, single-column layout, colours, borders, shadows, spacing and transitions. Inter weights 400/500/600/700/800 are served locally. The prototype's Lucide SVG paths and 1.75px strokes are rendered locally, and its Tesco, Boots, Sainsbury's, Morrisons and Asda logo assets are copied directly. Other real retailers receive a name initial, rather than an unrelated brand logo.

The port retains the product navigation, title/rating slot, full-width swipe gallery/counter, Observed, Available In, Product Details, Community Photos, Observation History and Add Observation cards. Available In retains the centred header, compact pack summary, Nearby/All Stores control, location/filter bar, retailer rows, observation badges and contribution card, including the original changes at 600px. The old website header and two-column product styling are suppressed only for these two screens. The existing five-item application navigation and iOS safe-area accommodation remain.

The gallery uses the approved prototype's local pointer/keyboard controller and 40px gesture threshold. Native image dragging is disabled so Chromium drag gestures reach that controller. A browser MutationObserver releases listeners when enhanced navigation removes the gallery. A regression exposed a race when server component disposal tried to dispose an already-released gallery interop reference; browser-owned DOM cleanup avoids that race. Enhanced navigation can also remove a pending element before module import completes; the controller rejects absent/detached elements and only confirms attachment for a live gallery, with a parameter-revision guard. This does not add reconnect/reload behaviour or conceal circuit failures.

## Production data adaptations

- `/products/{Slug}` remains the product route. `/products/{Slug}/retailers` is a separate interactive route using the shared screen component; the explicit screen parameter avoids stale route detection during enhanced navigation.
- The existing catalogue client and variant/size/pack selection logic remain. `variantId` and `packTypeId` travel through View all and Back. Changing a size/pack invalidates old offers immediately and retains the existing asynchronous selection guard. Real controls use the prototype's detail rows; expanded details retain brand/manufacturer, measurements, GTINs, recorded variant features, description and official website.
- The existing public Atlas client supplies real dated physical observations. Observed/history/retailer rows are scoped to the exact selected pack, not merely the product family. A failed observations request is reported separately; catalogue details/listings can still load.
- Nearby requests geolocation only after an explicit Choose location action and shows observed shops within 25km. All Stores also includes the selected pack's verified catalogue destinations. Filter supports retailer/place text and the last 14 days. A dated observation is not a stock guarantee; verified catalogue destinations retain their separate Catalogue listing label and affiliate disclosure. Outbound links use the existing resolved `DestinationUrl`, with `noopener noreferrer`. Shop links open the existing Atlas location. No prices, observer names, stock statuses or observations are invented.
- Add Observation retains the selected `packTypeId` and the existing authenticated contribution gate. No authentication policy or API contract changes.
- Production has no ratings aggregate or community-photo provenance in this catalogue contract. Those original visual sections show truthful empty states rather than fabricated stars/counts or manufacturer images labelled community photos. History only shows real dates. The heart remains unavailable because saved products/Backpack implementation was explicitly deferred.

## Files and assets

Web: `ProductDetail.razor` route wrapper; new `ProductAvailability.razor`; shared `ProductCatalogueScreen.razor`, `PrototypeIcon.razor`, `PrototypeRetailerMark.razor`; scoped `product-prototype.css`; browser `product-prototype.js`; stylesheet registration in `App.razor`. The obsolete isolated website CSS is replaced by a comment. No API/domain/database files change.

Asset sources: retailer SVGs at the prototype’s `assets/retailers/Tesco_Logo.svg`, `Boots_logo.svg`, `Sainsbury's_Logo.svg`, `Wm_Morrison_Supermarkets_logo.svg` and `Asda_logo.svg` (local filenames shortened); Inter font files from the Google Fonts CSS for the approved weights (`https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700;800&display=swap`). The fonts' SIL Open Font License and Lucide ISC/MIT notices are retained locally. Retailer marks remain their respective owners' trademarks. The approved `assets/products/tena1.avif` is converted to PNG **only in the isolated visual test fixture**, because the desktop WebKit build does not decode that AVIF; production continues to use real catalogue image URLs. No dummy product assets are added to production.

## Automated validation

`PrototypeProductBrowserTests` runs the real local API/catalogue with isolated PostgreSQL data in Chromium at 390/768px and WebKit at 430px, with standalone detection enabled. The local visual fixture only substitutes the approved pack image/title. Actual product, variants, sizes, packs, listings, shop observations and authentication routing remain real.

Coverage checks approved heading colour/weight, card radius, gallery sizing/top spacing, retailer mobile/desktop logo sizing, Inter loading and no horizontal overflow; pointer/keyboard gallery interaction; exact-pack observations/history; catalogue destinations and outbound link protection; Nearby geolocation/filtering; enhanced navigation and Back; pack switching without stale destinations; and the anonymous observation authentication landing. Browser page errors fail the test; explicit absent/detached-element cases cover the enhanced-navigation gallery race. Existing public variant, listing and known-scan regressions are also run.

[Full regression suite](prototype-port-evidence/full-suite.log): **281 passed, 0 failed, 0 skipped** (230 integration/browser + 51 domain). [Release build](prototype-port-evidence/release.log): **0 warnings, 0 errors**. The final suite used the checked-in [serial runner settings](prototype-port-evidence/serial.runsettings); parallel runs exposed an existing product-editor timing test, which also [passed independently](prototype-port-evidence/editor-check.log), without changing its application code or assertions. [Local rendered screenshots](prototype-port-evidence/local/) cover all three widths and both engines; they use real local data with longer retailer names, honest empty states and the retained application navigation. Production deployment evidence follows below. Desktop WebKit/standalone emulation cannot prove physical iOS Home Screen safe-area rendering, native share sheet, location permission UI, OS suspension or installed-app storage behaviour. Physical iPhone acceptance remains required; the previous authenticated-contribution issue also retains its separate physical-device acceptance requirement.

## Physical iPhone acceptance

1. Relaunch the installed PWA and open a real verified product from Products or a known barcode. Compare the title, gallery, cards, type, colours and spacing with the approved Product Details page. Check portrait widths and safe areas on the physical iPhone.
2. Swipe the real image gallery and test the native share sheet. Select another recorded size/pack and confirm its measurements/GTIN and retailer destinations change appropriately.
3. Open Available In → All Stores. Confirm the pack summary and real listings/dated observations, filters and outbound destinations. Switch to Nearby, grant location access explicitly, and check actual nearby observations; a lack of observations should produce an honest empty state.
4. Use Back and confirm the same variant/pack remains selected. Add Observation must retain that exact pack and the existing authentication requirement. Use only a genuine field observation if completing this flow.
5. The prior physical authenticated unknown-barcode acceptance remains governed by [its separate recovery report](authenticated-contribution-recovery.md); this visual port does not establish that acceptance.

## Application rollback

Restore the Web image captured before this port, then check ACA readiness, application HTTP200 and a live circuit:

```bash
az containerapp update -g rg-diaperscout-prod -n diaperscout-web-vnet --image diaperscoutprod-dsg9bgg6dkgkcwbs.azurecr.io/diaperscout-web@sha256:8b1baa2246cbc0df3cb7bcf7e2137efada42c45d33520eb09d526119fe997efd --revision-suffix prototype-rollback-web
```

This is a Web image-only rollback. No API, schema, migration or scaling rollback is required. Keep sticky affinity, min0/max2, cooldown600 and the existing shared Data Protection settings. The previous contribution recovery fix is retained by this rollback image.
