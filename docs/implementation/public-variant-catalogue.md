# Public variant catalogue and exact retail selection

Implemented 1 October 2026. Application and projection changes only; no database migration, model changes, reseeding, or canonical data updates.

## Root causes

1. Public catalogue cards rendered `CatalogueProductListItem.Name`, which the existing query populated with raw `Product.Name`. The canonical Brand was returned separately but was not composed into the title. The detail heading and page title separately concatenated Brand and Product without overlap handling, while breadcrumbs used raw Product.Name.
2. Search, filters, counts, and pagination operated on Products before loading their variants, returning one result per Product.
3. Product Details flattened every variant's sizes with `SelectMany`. Grouping by SizeVariant ID did not resolve this because identically named sizes from different variants are distinct canonical records.
4. The retail query selected all eligible listings under the Product, and the page rendered the entire offer collection. Selecting a size or pack changed selection IDs but never changed that collection.

## Architecture and public identity

`AtlasQueries.SearchPublicCatalogueAsync` projects a result per canonical ProductVariant joined to its Product, canonical Brand, and Manufacturer. Every result keeps the original Product ID and slug and adds `ProductVariantId`. Moderator management search retains its existing product-level projection and raw naming.

Search includes brand/product/variant composition and variant names. Variant-level filtering occurs in SQL before counting, sorting, and pagination. Facet counts use variant identities, including distinct variant counts for size and packaging options. A deterministic variant-ID tie breaker stabilizes pages. Card sizes, backings, packaging, and eligible canonical retailer listings belong to that result's variant. Legacy submission destinations are not promoted to public canonical offers.

Canonical ProductVariants have no separate publication/status field. Public eligibility follows the canonical Product's `Current` status. Draft catalogue-submission variants are a separate editorial stage and are not queried. No new variant publication lifecycle was invented.

The public name is canonical **Brand + Product + meaningful Variant**. A missing Brand falls back to Product. Manufacturer/Business is never substituted for Brand. `PublicProductIdentity` trims components and removes case-insensitive word overlap at component boundaries. It suppresses empty, Default, Single version, and Unnamed variant names. A sole variant labelled Current is also structural and suppressed; Current remains visible if it distinguishes multiple variants. Publication's existing structural variant named after its Product is naturally handled by overlap removal. Canonical names are never rewritten.

Examples:

- Crinklz / Original → Crinklz Original
- NorthShore / MEGAMAX / Black → NorthShore MEGAMAX Black
- NorthShore / NorthShore MEGAMAX / MEGAMAX Black → NorthShore MEGAMAX Black
- NorthShore / MEGAMAX / Default → NorthShore MEGAMAX

Cards, detail headings, breadcrumbs, gallery labels, and page titles consume the same projected public identity.

## Navigation and selection

Variant URLs reuse the product slug: `/products/{slug}?variantId={canonical-product-variant-guid}`. The page reads the query parameter on navigation, refresh, and browser back/forward. The existing product URL remains valid and selects the first variant in the existing name ordering. Explicit nonexistent, foreign, or malformed variant identifiers produce a friendly not-found state, never fallback to another variant.

The public detail API accepts optional `variantId` and `packTypeId`. It returns one active variant and its own sizes/packs, plus public labels/IDs for the variant switcher. A supplied pack must belong to the selected variant. A pack-only request resolves its canonical owning variant; an unqualified request uses the first variant and its first size's first pack.

The Where to Buy SQL query restricts listings to the resolved exact PackType ID, keeping the existing verified-listing, verified-retailer, and Current-product rules. Ordinary URLs and preferred affiliate/Awin resolution use the existing resolver. Listing source/provenance is unchanged: Manual and future providers consume the same canonical PackType → RetailerProductListing association.

Size changes select that size's first pack; pack controls contain only that size's packs. Variant changes navigate to its canonical URL and reset downstream IDs. Every selection change clears offers before requesting the new exact pack. A monotonically increasing request revision prevents older asynchronous responses from restoring stale offers. Missing packs and no matching eligible listings show the friendly empty state.

## Moderator UX

Ordinary retailer creation exposes Name and Website. The backend generates an omitted slug with the existing `RetailerDiscovery.CreateRetailerSlug` helper and checks existing slug uniqueness, adding numeric suffixes for automatic name collisions. Explicit slug validation, the database unique index, existing records, editing, and identity verification remain intact. Manual slug editing remains in the retailer edit form. The search placeholder now reads “Name or website…”, while backend slug matching remains available.

The embedded manual listing field reads “Retailer product URL” with helper text for the exact product/pack. The standalone manual listing page already used that label and now also has the helper text. Its public product link carries the canonical variant ID. Underlying discovery/domain/database field names remain unchanged.

## Files changed

Application:

- `src/DiaperScout.Application/Contracts.cs`
- `src/DiaperScout.Application/PublicProductIdentity.cs`
- `src/DiaperScout.Infrastructure/PublicCatalogue.cs`
- `src/DiaperScout.Infrastructure/ServiceCollectionExtensions.cs`
- `src/DiaperScout.Api/Program.cs`
- `src/DiaperScout.Web/Services/ProductCatalogueClient.cs`
- `src/DiaperScout.Web/Components/Pages/Products.razor`
- `src/DiaperScout.Web/Components/Pages/ProductDetail.razor`
- `src/DiaperScout.Web/Components/Pages/RetailerManagement.razor`
- `src/DiaperScout.Web/Components/Pages/RetailListings.razor`

Tests:

- `tests/DiaperScout.Domain.Tests/PublicProductIdentityTests.cs`
- `tests/DiaperScout.Api.IntegrationTests/PublicVariantCatalogueApiTests.cs`
- `tests/DiaperScout.Api.IntegrationTests/PublicVariantCatalogueBrowserTests.cs`
- `tests/DiaperScout.Api.IntegrationTests/CatalogueApiTests.cs`
- `tests/DiaperScout.Api.IntegrationTests/RetailListingApiTests.cs`
- `tests/DiaperScout.Api.IntegrationTests/RetailListingBrowserTests.cs`
- `tests/DiaperScout.Api.IntegrationTests/PasskeyWebFactory.cs`

Report: `docs/implementation/public-variant-catalogue.md`.

## Tests and exact results

New API tests use canonical creation, management variant/size APIs, manual listing creation/verification, and retailer creation APIs. They cover shared Product identity, named/default variants, public naming, canonical-name preservation, variant-scoped facets/filtering/pagination/counts, direct links, invalid/cross-variant packs, exact-pack offers, unverified listings/retailers, and automatic slug collisions. A second pack is added through the domain fixture because no existing management API adds another pack to an existing size.

New browser tests cover real catalogue search and variant navigation, size and pack changes, stale offer removal, variant switching, refresh, back/forward, malformed/nonexistent links, and retailer creation without a slug field. Existing affiliate/Awin, provider idempotency, retailer editing/verification, and manual listing tests remain in the complete suite. Public projection expectations in older tests were updated; the no-GTIN offer test now requests its exact pack explicitly. Browser test hosts use ephemeral ports to avoid cross-test binding conflicts.

Commands:

```text
dotnet restore DiaperScout.slnx --disable-parallel
dotnet build DiaperScout.slnx --no-restore -c Release -m:1 -nodeReuse:false
dotnet test DiaperScout.slnx --no-restore -m:1 -nodeReuse:false
git diff --check
```

Final Release build: succeeded, 0 errors and 1 pre-existing nullable warning, CS8600 in the affiliate-management code in `ServiceCollectionExtensions.cs`.

Final complete test run: **160 API/integration/browser tests passed; 18 domain tests passed; 0 failed; 0 skipped**. The integration count includes six Playwright browser tests. Disposable PostgreSQL 18.3 containers were used; production data was not used for automated tests. `git diff --check` passed.

During validation, the new browser test caught omitted optional GUIDs being sent as empty query parameters; the client now omits absent parameters. One older waist-editor browser test timed out in an earlier combined run, passed alone, and passed in the subsequent complete runs. This timing sensitivity remains a test-maintenance consideration.

## Migration, deployment, and production checks

No migration is required or introduced. Persistence models, migrations, and model snapshots are unchanged. No production canonical records are modified by the implementation. No TLS, Private Link, NAT, static egress, Container App networking, or unrelated Azure resources are changed.

Production baseline (read-only public HTML): one MEGAMAX card titled “MEGAMAX”, 27 size buttons on its product page, and the NRU destination `https://nru.co.uk/products/northshore-megamax-black?variant=50456472912205` labelled Medium · 10 pieces · Bag.

Initial image tag: `variant-scope-20261001-0127766c39`. API build run `dbr` and Web build run `dbs` both succeeded in the existing `diaperscoutprod` registry. Production smoke testing found the single Crinklz structural variant named Current, so an API-only naming correction was added with unit/API regression coverage and a fresh complete test run. The Web implementation is unchanged by this correction. Final API build run `dbt` succeeded. Final API tag: `variant-scope-20261001-ff89f1544a`. Final Web tag: `variant-scope-20261001-0127766c39`. Previous production image tag: `96b41ff`.

Production deployment completed on 1 October 2026 in the existing `rg-diaperscout-prod` Container Apps:

- API: `diaperscout-api-vnet--varscope-ff89f1544a` — active, Healthy, Provisioned, Running; latest revision equals latest ready revision.
- Web: `diaperscout-web-vnet--varscope-0127766c39` — active, Healthy, Provisioned, Running; latest revision equals latest ready revision.

Only image references and revision suffixes were updated. The image-only updates preserved existing application configuration, networking, and production data. No migration runner or catalogue mutation was invoked.

Final read-only Chromium production smoke test: **29 checks passed, 0 failed**. The test navigated public pages and changed public selection controls; it did not create/edit catalogue records, retailers, or listings, or follow the external retailer purchase link.

Observed catalogue titles:

- Crinklz Original
- NorthShore MEGAMAX Black
- NorthShore MEGAMAX Blue
- NorthShore MEGAMAX Pink
- NorthShore MEGAMAX Purple
- NorthShore MEGAMAX Tie-Dye
- NorthShore MEGAMAX White

All six MEGAMAX links reuse `/products/megamax` and carry distinct existing canonical ProductVariant IDs. Their detail headings match their card identities. Black, Blue, Pink, Purple, and Tie-Dye each show only Extra Large, Large, Medium, and Small (four size records). White shows only its seven recorded sizes: 2XL, 3XL, Extra Large, Large, Medium, Small, and X-Small. The former flattened wall of 27 sizes is gone.

The existing NRU listing was observed for:

- Variant: Black, canonical ID `bb3ad9fc-9dea-4f07-b179-5bc3f66b7d7e`.
- Size: Medium.
- Pack: 10 pieces · Bag, canonical PackType ID `a17f3fa3-a690-4224-9fad-8c84902ebe90`.
- Destination: the same pre-deployment NRU URL recorded above.

Changing to Extra Large removes that destination and selects a different canonical pack. Selecting Medium on each other MEGAMAX variant does not show Black's NRU destination. Changing variant removes the previous offer; refresh preserves the requested variant, and back/forward restores the corresponding variant headings. Malformed variant links show Product not found. Crinklz's detail heading is Crinklz Original, and its canonical record/structural Current variant was not renamed.

The other-pack exclusion and unverified-retailer/listing cases are covered by the isolated automated suites; no fake production records were created to exercise those cases.

## Practical limits

- Legacy product-only links choose the first name-ordered canonical variant. Variant-specific links retain the requested identity across reload/history.
- Size and pack selection are in-page state and reset on a reload; variant identity is preserved in the URL.
- Eligibility follows Product.Current because canonical ProductVariant has no independent publication status.
- No Amazon/eBay integration, automated discovery, analytics, or unrelated redesign was added.
