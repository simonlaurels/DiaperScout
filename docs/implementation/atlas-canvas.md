# Atlas canvas and place taxonomy checkpoint

## Repository baseline

Work started on `fix/product-submission-recovery` at `8731125661a861b0617daf0b34be3494cc96ea1c`, with a clean working tree. Fetch and fast-forward pull confirmed the checkout was current with GitHub. Temporary files, test results and browser captures are on D:.

## Existing architecture and preserved evidence rule

`Atlas.razor` is an InteractiveServer page. `PlaceObservationClient` reads the anonymous `/api/v1/places/atlas` endpoint, whose projection is built by `Infrastructure/PlaceObservations.cs`. `PlaceMap` uses the repository's locally hosted Leaflet and configured tile URL. The same map component supports selecting a shop's exact position during contribution; that picker retains its existing behaviour.

Previously the page did not render the map while loading, on failure or when the projection was empty. It also auto-selected the first location. The revised page renders the map before fetching discoveries and keeps it mounted through loading, failure and retry. Selection is explicit, with existing `locationId` links supported.

`Observation.LocationId` references an actual physical `Location`; `Observation.PackTypeId` references the exact pack, whose size and variant identify the canonical product. `Location.RetailerId` is optional. A physical shop can exist without a retailer or an observation. Shop search and retail listings do not earn Atlas markers.

The unchanged public query requires all of the following:

* Location is a public commercial place and has both latitude and longitude.
* Observation type is `RetailAvailability`.
* Observation state is `Submitted` **or** `Accepted`.
* The exact pack joins successfully through size, variant and product.
* That product's status is `Current`.

It orders by observed date descending, then observation ID, and takes at most 2,000 observations before grouping by location. `AtlasPlace.ProductCount` is the count of distinct pack IDs, not observations or distinct products. This behaviour differs from older editorial-publication wording in some documentation; this presentation change does not change moderation/publication policy. Author identifiers are not exposed by the projection.

## Safe presentation

Mobile/standalone Atlas occupies the map canvas down to the measured existing bottom navigation. Its usual global header is hidden only on Atlas in this mode. Desktop retains the normal shell with a large usable map. Scoped CSS avoids changes to Welcome, Scan, Search, Products, authentication or the contribution picker.

Small floating Atlas/discovery controls and 44px map zoom controls sit above Leaflet. A discoveries list provides an alternative to tapping markers. Selecting an actual observed place opens a scrollable sheet containing its address, observation and distinct-pack counts, exact product/variant/pack links, pack quantity, observed date and optional reported shelf price. The sheet explicitly states that dated reports do not guarantee current stock; at most 50 loaded observations are shown, with a disclosure when truncated.

The marker is a reusable inline SVG/CSS teardrop with a neutral pack pictogram for unclassified places and saved-category pictograms for classified places. A separate numeric observation badge appears for counts greater than one (visual cap `99+`; accessible name retains the full count). It does not infer pharmacy/supermarket status from names or retailers. Marker content containing place names is constructed using DOM textContent. Selected markers have a stronger outline/fill. Enter and Space open the actual place sheet; the discoveries list is an additional keyboard-accessible route. New map movement respects reduced motion.

Both the page and marker layer reject entries without observations. The actual API supplies qualification; this extra defensive check cannot turn shop search into discovery data.

The small `Uncharted territory` field note appears when no **loaded qualifying locations are visible in the current Leaflet bounds**, rather than simply checking the global array length. It disappears when a loaded location enters view. This is not a fresh geographic API query and cannot claim completeness outside the existing 2,000-record projection. During loading/error, the map instead has a compact status/retry overlay. With zero observations there are zero product markers and the map can still pan and zoom.

The existing production `/pwa/guide-map.webp` appears decoratively at 68×64 beside the field note (48px in short landscape layouts). It is the canonical young Guide holding a map, with the canonical plush Fox tucked in her backpack. Provenance is recorded in `pwa-app-experience.md`: a production derivative of `docs/branding/pwa/guide-map-source.png`, generated using canonical Guide sheet `7ABC2EE7`. No artwork was generated or changed here.

## Place taxonomy checkpoint and approval

The brief required approval before a consequential taxonomy/schema change. The following nine-point proposal was presented after the safe Atlas presentation passed its targeted tests. On 3 October 2026 the user explicitly approved: **“Approve the taxonomy proposal and implement it before deployment.”** Only then was taxonomy implemented. The numbered points record the inspected baseline and agreed change.

1. **Current model:** Location has physical name/address/country, optional coordinates/retailer, public-commercial status and creator metadata. Neither Location nor Observation has a place category. Category describes the location; it does not qualify an observation.
2. **Current captured data:** contributors choose an existing shop or enter name, address, town, postcode, country and exact coordinates, explicitly confirming a public shop and its position. They record exact pack, observed time and optional price/currency. No pharmacy/supermarket category is captured. Existing names are not reliable classification evidence.
3. **Smallest proposed change:** add a nullable `PlaceCategory` to Location, with stable explicit values. Keep null as unspecified/unknown. Do not attach the category to each Observation, add retailer coupling, or change qualification/publication states.
4. **Initial taxonomy:** Pharmacy; Supermarket; Specialist retailer; General retailer; Convenience store; Other. `Other` is an intentional positive selection for a known place outside these categories; null means no classification is known. Keep the initial list small. Existing neutral markers remain for null; approved category pictograms can then be added without changing pin qualification.
5. **Migration implications:** one additive nullable column on locations, no guessed backfill or mandatory existing-row update. Existing location IDs, coordinates, observation links and exact pack relationships remain intact. Migration/rollback must follow the repository's existing EF process and be reviewed before production application.
6. **Submission implications:** an optional place-type selector when creating a physical shop, with an explicit unspecified option and Other. Existing-shop selection must use its saved category without silently overwriting it. Authority to edit/classify an existing or deduplicated location needs a bounded moderator/admin operation or separate agreed policy; do not let a subsequent observation overwrite shared metadata. This policy is part of the approval decision.
7. **API implications:** extend place read/search/Atlas DTOs with an optional category and shop creation with an optional input; reject unsupported values server-side. Old request payloads remain valid. Old clients must tolerate an additive response property; new clients default missing/null to the neutral marker. Anonymous reading and authenticated contribution boundaries stay unchanged.
8. **Existing-data compatibility:** existing locations stay unclassified. No inferred categories, changed retailers, new observation records or altered visibility. Category alone never creates a product pin. The data/API rollout must be backward compatible before the web begins consuming category values.
9. **Test implications:** model/input validation, unknown versus Other, additive migration against existing rows, old JSON request/response compatibility, authorization for shared-location edits, deduplication preservation, exact observation qualification unchanged, category icons/accessible names, and zero-observation locations excluded irrespective of category.

## Future Changing Places extension

Atlas owns a concrete `discoveryLayer` Leaflet layer group for the product projection, distinct from base tiles and the contribution picker. A later licensed Changing Places dataset can have its own layer group, marker factory and facility interaction, alongside the discovery group, without routing through the product observation sheet. No generic GIS framework, inactive toggle, Changing Places records, scraping, requests, icons, logos or branding have been introduced. Shared geographic concepts can be considered later without collapsing the two datasets' semantics.

Deferred: licensed Changing Places integration; server-side viewport/paging; dense geographic clustering. Count badges represent repeated evidence at one place and are not geographical clusters.

## Implemented approved taxonomy

`Location.Category` is nullable `PlaceCategory`. Stable codes are Pharmacy=1, Supermarket=2, SpecialistRetailer=3, GeneralRetailer=4, ConvenienceStore=5, Other=6. Null is unclassified; zero is not a valid category. `20261003102616_AddPlaceCategory` adds only nullable integer `Category` on `diaperscout.locations`. No backfill, observation mutation, index or foreign-key change is involved. The reviewed SQL is [migration-reviewed.sql](atlas-canvas-evidence/migration-reviewed.sql).

The optional trailing Category field on `CreatePublicShopRequest` and `PlaceItem` preserves older request/response payload compatibility. Search and Atlas return saved metadata. A new shop can include an optional category; deduplication always returns the saved shared place without overwriting its category, even when the incoming category differs. Selecting an existing shop for an observation does not edit the shop.

`POST /api/v1/places/{id}/category` uses the existing `PublishAtlas` policy, active `ICurrentUser` and database-backed `IEditorialAuthorisation.CanManageCatalogueAsync`. A role claim alone cannot grant permission. Only moderators/admins with active stored privileges can set or clear category on an existing public commercial shop. Private/invalid locations are not exposed through this editor. The handler saves only Category and returns the place DTO; observation IDs, exact packs, quantities, dates, prices, provenance, retailer links and location identity remain intact.

A shared `PlaceCategorySelect` uses the same six labels and Unspecified for optional creation and the moderator/admin editor in the Atlas place sheet. Anonymous browsing remains unchanged. Existing category metadata appears as text in the sheet and accessible marker name. Pictograms are capsule (pharmacy), basket (supermarket), shelves (specialist), storefront (general), shopping bag (convenience), and ellipsis (Other), with a neutral exact-pack box for null. Muted teal/sage/ochre/clay/slate tones support the established cream/green palette; text/pictograms carry meaning independently of colour. No official pharmacy cross or external facility branding is copied.

## Validation and delivery

Targeted final run: **32 passed, 0 failed, 0 skipped**, duration **2m40s**. This includes new Atlas browser tests, place observation API tests, the existing authenticated scan → exact-pack shop observation → Atlas workflow, existing PWA shell/search/detail/cache/startup tests, and pending-API initial document reliability checks. Atlas uses the map/loading status after interactivity; Products retains its original startup assertions. Anonymous empty/populated discovery fixtures are separate from the real authenticated contribution flow.

New qualification coverage includes every ObservationState, repeated evidence for a single exact pack, multiple locations, an unqualified candidate shop, inexact observations, private locations and a discontinued exact-pack product. Browser coverage includes loading, API failure/retry, zero markers, observed markers, repeated observations and counts, exact links/pack/date/price, explicit selection and deep links, list access, keyboard panning to uncharted bounds, map zoom and refresh. Portrait Chromium/WebKit, short landscape WebKit, tablet and desktop are covered. Existing map-tile/script failures remain covered by the contribution browser regression.

The first targeted run was 18 passed / 2 failed because the desktop header overlaid zoom controls. Atlas-only header spacing fixed that; the next run passed 20/20. An initial full run was stopped to update obsolete Atlas empty-state/loading expectations before the 32-test targeted run. The complete safe-presentation suite then passed **318/318** (51 Domain, 267 integration/browser; integration duration 11m15s).

After taxonomy approval, the first combined targeted run passed 10 Domain and 43/46 integration checks. Two failures exposed keyboard Enter opening only Leaflet's popup; explicit Enter/Space handling now opens the actual sheet. The third was fixture interference between two migration rollback tests; the category migration now owns a separate disposable database fixture. The replacement targeted run passed **56/56** (10 Domain, 46 integration/browser; integration duration 3m14s). It covers all six categories, null versus Other, unsupported-value rejection, old JSON without Category, deduplication, unauthorized/forged-role denial, moderator/admin edits, exact evidence preservation, migration compatibility, and real browser creation/save/clear/reload. The final full suite also checks Space activation. Final full-suite/Release results will be recorded after completion.

Final combined full regression: **341 passed, 0 failed, 0 skipped** (61 Domain; 280 integration/browser, integration duration **11m58s**). A supplemental browser check then passed **3/3**, duration **16s**, verifying settled selected markers above the sheet, Enter/Space, category graphics and real moderator save/clear. That supplementary rebuild initially encountered the full runner's Windows test-assembly file lock and was rerun only after the full runner exited; no test failed in that attempt because tests did not start. Final Release build: **0 warnings, 0 errors**, **6.39s**. `git diff --check` is clean. Results/temporary storage are on D:; [Release log](atlas-canvas-evidence/release-build.log) is retained in the repository.

Reproduction: `dotnet test DiaperScout.slnx --configuration Release --no-restore --settings docs/implementation/search-prototype-evidence/validation.runsettings`; `dotnet build DiaperScout.slnx --configuration Release --no-restore`. Browser tests require the existing Docker/PostgreSQL and Playwright runtimes. Environment variables TEMP/TMP use D:; `DIAPERSCOUT_BROWSER_EVIDENCE` optionally captures screenshots.

Browser fixtures are strictly test-only. Automated map tests intercept external tile requests with synthetic tiles, so captures verify layout/interaction, not production cartography. No production seed data is added. Inspected visual evidence:

* [Empty installed-iPhone-sized canvas, 390px](atlas-canvas-evidence/atlas-empty-390.png)
* [Short landscape canvas, 844px](atlas-canvas-evidence/atlas-empty-844.png)
* [Selected place and repeated observations, WebKit 430px](atlas-canvas-evidence/atlas-selected-430.png)
* [Selected place, desktop 1280px](atlas-canvas-evidence/atlas-selected-1280.png)
* [Saved-category markers and dated details, WebKit](atlas-canvas-evidence/atlas-categories-webkit.png)
* [Saved-category markers and dated details, Chromium](atlas-canvas-evidence/atlas-categories-chromium.png)

Files changed: Domain `Model.cs`; Application `PlaceObservations.cs`; Infrastructure `PlaceObservations.cs`, additive migration/designer/model snapshot; API `PlaceObservationEndpoints.cs`; Web `Components/App.razor`, `Pages/Atlas.razor`, `Pages/CreateObservation.razor`, `Shared/PlaceMap.razor` and `.razor.js`, `Shared/PlaceCategorySelect.razor`, `Services/PlaceObservationClient.cs`, `Services/PlaceCategoryPresentation.cs`, `wwwroot/css/atlas-pwa.css`, `wwwroot/js/atlas-marker.js`; tests `PlaceCategoryTests.cs`, `PlaceCategoryMigrationTests.cs`, `AtlasBrowserTests.cs`, `AtlasCategoryBrowserTests.cs`, `PlaceObservationApiTests.cs`, `PlaceObservationBrowserTests.cs`, `PwaBrowserTests.cs`, `PwaReliabilityTests.cs`; this report and evidence artifacts. No Welcome/Scan component, canonical artwork, authentication configuration or infrastructure definition was changed.

The taxonomy checkpoint is approved. Deployment follows final combined validation: existing migration job execution override, then API and Web image updates using the unchanged deployment configuration. The copied execution template follows [Azure's Job Start API](https://learn.microsoft.com/en-us/rest/api/resource-manager/containerapps/jobs/start?view=rest-resource-manager-containerapps-2025-07-01), without updating the stored job configuration. Rollback uses the prior API/Web images while retaining the additive nullable column, rather than running Down against production. Private before/after snapshots are on D:; evidence must contain no secret/environment values. There are no infrastructure, scaling, networking or authentication configuration changes.
