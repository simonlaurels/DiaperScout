# Foursquare specialist coverage comparison

> Historical provider-selection evidence. Geoapify was subsequently selected for Year One; the current decision is ADR-0012. Earlier conditional recommendations below remain as the research record.

5 October 2026. Powered by Foursquare. Bounded local assessment; no application, schema, repository, production or provider-data changes. Compared with [Geoapify spike](geoapify-report.md), the existing report corresponding to the requested `geoapify-report.md`.

## Decision

**Recommend Geoapify as the initial provider, with explicitly accepted coverage gaps; do not add the hosted Foursquare API as a production fallback under the published terms.** Foursquare materially improves some specialist cases, and its omission of the obsolete Bush location is not a coverage failure. It still does not solve NRU/Aston. It introduces persistence restrictions and uncertain branch identities. Its open dataset is a plausible future supplement, independently sourced and tested; hosted results are not an Apache-licensed import.

This recommendation changes the earlier “not sole provider yet” gate only if the product accepts missing-place UX, potentially private saved discoveries and later confirmation. It is not a claim that Geoapify now provides comprehensive coverage. If immediate coverage of every specialist case remains mandatory, **neither passes**. No provider integration is approved or implemented by this assessment.

## Method and limitation

12 originally tested cases: nine specialist references and Boots Yate, Badham St George’s, CVS Raleigh controls. Bush is now excluded from current-specialist scoring following the user’s correction, leaving eight current specialist cases. Same public reference centres as Geoapify, including imperfect city/postcode centres. 48 successful requests: broad unfiltered search and name/content query, each at 1 km/3 km, limit 20, distance sorting. Current `places-api.foursquare.com/places/search`, API version 2025-06-17, Bearer authentication. No legacy v3 calls, detail/premium requests, retries, live device coordinates or Explorer data.

Unfiltered nearest-20 results are saturated in most areas, so absence there says little about database coverage; all specialist matches appeared through name queries, not the broad nearest-20 lists. Name queries match broader content, not exact business names: Bush returned unrelated healthcare services and Raleigh returned many unrelated medical businesses. Results were reviewed manually.

**Further calls stopped:** the [current AUP](https://foursquare.com/legal/terms/aup/) §VI requires advance written authorization for benchmarking or availability/performance testing. This clause was discovered after the 48 requests completed. No category-filter, alias or additional timing tests were subsequently run. Reported timings describe the completed requests, not a controlled performance benchmark. The bounded coverage evidence therefore has limitations, especially NRU/Napus aliases and category recall.

## Direct comparison

| Case | Geoapify | Foursquare | Assessment |
|---|---|---|---|
| Pearce, 2 Stover Road Yate | Inconclusive city-centred miss | Pearce Brothers Mobility, exact number/street/postcode, name search at 3 km only | Added branch; 1,586 m from reused city centre, not an accurately centred 1 km comparison. No categories supplied |
| Bush Healthcare Yate (obsolete tested location) | **Stale POI:** Bush has left; Greggs now occupies the unit | Not returned; unrelated healthcare results | Excluded from current-specialist scoring. No penalty for Foursquare; its omission avoids this stale identity, but does not prove Greggs coverage |
| Swindon mobility shop | Possible older Keep Able identity | The Swindon Mobility Store, 7 Clive Parade, Cricklade Rd, SN2 1AJ | Clearer current name/full address; 394 m from reused centre |
| Wroughton mobility shop | Correct 6 Devizes Rd, miscoded car shop | Correct 6 Devizes Rd, SN4 0RZ, plus 8 Devizes Rd and Ellendune identities | No extra branch coverage needed; possible older/duplicate identities require checking, not automatic merging |
| NRU/Napus Denton | Missing | NRU query missing | No improvement demonstrated; no subsequent Napus/Nappies alias calls |
| Medilink Aston | Missing | Missing | No improvement demonstrated |
| Medilink Edinburgh | Missing | Salts Medilink at 3 km only, no street/postcode; 1,130 m away | Insufficient evidence of current 97 McDonald Rd branch; not counted as verified hit |
| Medilink Plymouth | Missing | Salts Medilink, 53 Valley Rd, PL7 1RF | Clear addition; Medical Supply Store, 46 m from centre |
| Raleigh Medical Supply | Missing | Raleigh medical supply, 2601 Blue Ridge Rd, 27607 | Clear addition; Medical Supply Store, 2 m from centre |

Of eight current specialist cases after excluding Bush, five identities match reference addresses: Pearce, Swindon, Wroughton, Plymouth, Raleigh. Four are improvements in identity/coverage versus Geoapify, although Pearce’s previous test was inconclusive rather than a definitive miss and Swindon already had a possible older identity. Edinburgh remains unverified; NRU and Aston remain missing. No current specialist advantage for Geoapify over Foursquare was demonstrated in this completed subset. The corrected comparison strengthens Foursquare’s coverage advantage; it does not remove hosted retention/licensing obstacles. Small purposive sample, not nationwide recall.

Controls all returned expected branches: Boots 15–17 North Walk, Badham 84 St Georges Place/GL50 3QD, CVS 2411 Landmark Drive/27607. Another Boots listing at 11 West Walk had a separate ID and telephone; do not infer duplication solely from proximity. Nearby Badham branches are distinct; some lack addresses or contain malformed postcodes. Simon’s across-road distinction remains relevant.

## Categories, coordinates and identity

Observed target categories vary: Pearce none; Swindon Medical Center; Wroughton Retail; Plymouth/Raleigh Medical Supply Store; controls Pharmacy. A medical-supply-only picker would miss multiple useful shops. Category union/filter behaviour was not tested after the contractual finding; do not claim equivalence with Geoapify’s narrow queries.

366 unique place IDs across all returned rows. Fields present: coordinates 366/366; country 366; street-address string 315 (86.1%); formatted address 348 (95.1%); postcode 311 (85.0%); locality 344 (94.0%); categories 356 (97.3%). This pool differs from Geoapify’s 466-place baseline, so percentages are descriptive, not controlled provider rankings. Coordinates are finite and branch-plausible for verified targets, not independently surveyed entrances. Edinburgh’s displaced point and incomplete address are a material quality limit. Official [Edinburgh reference](https://www.saltsmedilink.co.uk/Medilink-Network/Dispensing-Care-Centres/edinburgh) remains 97 McDonald Road, EH7 4NS.

No repeated identical ID within one response. Distinct IDs can still describe historical or overlapping businesses: Wroughton has four related listings at three address presentations; no source verification proves which are closed. Refresh dates do not guarantee accuracy. Hosted rows did not provide an Apache licence grant or Geoapify-style OSM datasource attribution. `fsq_place_id` is a useful linkage key, not proof of correctness, permanence or storage rights.

## Request usage and observed timing

48/48 HTTP 200; median 445 ms, p95 578 ms, maximum 601 ms, including local network/TLS. Geoapify’s recorded baseline Places median was 774 ms, p95 1,385 ms; its follow-ups had much longer tails. Different endpoints/query samples/times mean this is not proof of comparative provider performance.

[Published June 2026 pricing](https://docs.foursquare.com/developer/reference/upcoming-changes): 500 free Pro calls, then $15 per 1,000 in the next tier. This run uses 48 search calls: nominally free if sufficient allowance remains, or $0.72 at that paid rate. Actual account consumption/charges were not dashboard-verified. Four searches per place-finding exercise would consume four calls, not one; production should use a smaller adaptive strategy. No billing configuration changed.

## Hosted API versus open dataset

The [self-service API EULA](https://foursquare.com/legal/terms/apilicenseagreement/) grants use during the agreement term, requires branded attribution where data appears and for external reports, restricts crawling, and delegates caching to its usage guidelines. The linked current guidelines page supplied no substantive caching text. Therefore no precise indefinite-ID exception or permanent name/address/coordinate retention permission was verified from current official documentation; do not substitute legacy Personalization API rules.

The [Master Terms §14](https://foursquare.com/legal/terms/enterprise-developermasterterms/) requires removing product data after account termination. The [AUP §IV](https://foursquare.com/legal/terms/aup/) restricts extracting/storing data to build a location database and dataset blending unless the agreement permits it; §I also restricts combinations subjecting Foursquare data to share-alike obligations. A combined hosted-Foursquare/OSM-derived place database needs express permission, not just separate attribution. Healthcare-sensitive user information must not be sent. Our spike used only public business references.

Consequently hosted API responses can support licensed display/use within the applicable agreement; **a permanent historical Location snapshot and provider-independent exit are not established**. Foursquare written clarification would be needed before that architecture. Existing local response files are evaluation evidence, not a production import or a perpetual archive entitlement; retention must follow the applicable agreement, including termination deletion.

Separately, the [FSQ OS Places NOTICE](https://opensource.foursquare.com/places-notice-txt/) licences the released dataset under Apache 2.0, with licence/notice preservation and change notices. Those independently obtained dataset records can supply durable names, addresses and coordinates and survive ending hosted API use. Preserve release/source provenance. A hosted ID may help locate a record, but obtain the durable fields from the licensed release itself; do not relabel API attributes. We have not verified that these particular added branches appear in a current open release, or costed hosting it. This is a separate option, not permission to copy hosted responses.

## Exact next action and safety

Prefer Geoapify’s simpler permitted-storage path for Year One if coverage gaps are accepted; first resolve its combined-category issue and define missing-place handling. Do not pay for a hosted Foursquare fallback on this evidence. If specialist gaps are unacceptable, the next bounded investigation is whether a current Apache-licensed FSQ OS Places release contains Pearce, Swindon, Plymouth and Raleigh and can be queried economically alongside a separately licensed OSM place dataset. No more hosted benchmarking without required authorization.

Secure key read from user-only temporary file; absent from scripts, results, console output and reports. Credential substring audit passed and temporary key deleted. All artifacts are in this task workspace outside the DiaperScout repository. No production/schema/deployment changes or commits.

## Evidence correction — 5 October 2026

Simon confirms Bush Healthcare left the tested Yate unit and Greggs moved into it from a previous smaller unit. Geoapify’s Bush listing is stale, not a successful current specialist match; Foursquare’s omission is not counted against coverage. No additional provider/API calls were made. Raw evidence and request/timing metrics are unchanged. The conditional Geoapify recommendation remains based on storage rights/cost simplicity and explicitly accepted gaps, not superior specialist coverage. If those gaps are unacceptable, neither hosted option meets the combined coverage-and-durable-storage requirement.

## Alias follow-up — 5 October 2026

Geoapify additionally returned zero results for `Nappies R Us` and `Incontinence Choice` at both 1 km and 3 km around the Denton reference centre (four successful requests). Foursquare was not retested; its NRU result remains limited to the tested name, not a confirmed all-alias absence. Coverage recommendation unchanged.
