# Local-discovery place providers — bounded selection assessment

> Historical provider-selection evidence. Geoapify was subsequently selected for Year One; the current decision is ADR-0012. Earlier conditional recommendations below remain as the research record.

Assessed 5 October 2026. Analysis only; no credentials, accounts, runtime changes or production changes. Recommendation: test Geoapify first. Fallback: Foursquare Open Source Places, with hosted API retention rights clarified separately.

## Decision and comparison

All five offer nearby POI/business search from coordinates. None identifies whether a particular diaper pack is stocked: the Explorer supplies that observation. Global coverage makes US use technically viable for all five; UK/US category recall is unmeasured here.

| Provider | Durable selected name/address/coordinates | Existing Leaflet/OSM Atlas | Coverage assessment, not a measured ranking | Low-cost commercial use / exit |
|---|---|---|---|---|
| Geoapify | Explicit Places FAQ permits storage and redistribution; OSM-derived fields retain ODbL obligations | Yes | OSM-based; supermarkets/pharmacies have explicit categories. Specialist retailer tagging/completeness is the principal risk | Free commercial production expressly allowed within limits and attribution. Properly licensed OSM data can survive ending the API subscription |
| Google Places | Place IDs retainable; permanent business snapshots not established; coordinates limited to 30 days | Ordinary Places API cannot be displayed on non-Google maps; UI Kit has an exception | Strong general-business candidate, but no live evidence here for awkward retailers | Commercial use allowed with billing/quotas. Leaving does not grant rights to retain restricted content |
| Foursquare | Apache-2.0 OS Places dataset permits durable storage. Do not assume hosted API responses inherit that licence | Open dataset yes; API attribution and restrictions separately apply | Global business dataset; plausible complementary specialist coverage, untested | Open dataset commercially usable and survives provider exit. API licence is term-bound; obtain explicit modern API storage clarification |
| TomTom | No current permanent POI-storage grant verified; treat as unresolved, not permitted by default | Technically compatible REST points; current contractual map/storage permission unverified | General POI search; specialist recall untested | Current free monthly allowances published. Production use/exit rights must be checked against actual applicable agreement |
| HERE | Standard terms cap external result storage at 30 days; audit/evaluation exception does not cover public historical Atlas | Terms permit distinguishable attributed layering; avoid contaminating HERE data with ODbL | Global POI search; specialist recall untested | Standard application licence exists, but current applicable free entitlement/rates not reliably established. No permanent historical archive right |

[Geoapify storage permission](https://www.geoapify.com/places-api/), [Geoapify terms](https://www.geoapify.com/terms-and-conditions/), [Google service terms](https://cloud.google.com/maps-platform/terms/maps-service-terms), [Foursquare API EULA](https://foursquare.com/legal/terms/apilicenseagreement/), [FSQ OS Places notice](https://opensource.foursquare.com/places-notice-txt/), [HERE terms §§6.4(a),(b),(j)](https://legal.here.com/us-en/terms/here-platform-terms). TomTom's [current terms page](https://docs.tomtom.com/legal/terms-and-conditions) did not expose substantive contractual text in this assessment; no old traffic-specific licence was substituted.

## Geoapify and ODbL

Geoapify's Places FAQ explicitly allows caching, storage and redistribution without additional retention limits. Its terms require OSM attribution, Geoapify attribution on Free, and any API-specific credits. Preserve response datasource/licence metadata; do not assume every future source has identical rights. [Storage FAQ](https://www.geoapify.com/places-api/), [terms](https://www.geoapify.com/terms-and-conditions/), [source credits](https://www.geoapify.com/credits/).

Permanent retention is therefore a materially better fit than Google's expiring presentation approach, conditional on compliance with the underlying open-data licence. Stopping paid/free API access does not revoke the ODbL rights to already lawfully obtained OSM content; attribution and share-alike obligations continue. This conclusion concerns open data, not continued entitlement to Geoapify's service or proprietary enhancements.

Selected records accumulated into an OSM-derived place database may trigger ODbL share-alike when publicly used, including through Atlas. Do not rely on “only a few records per request”: repeated extraction and cumulative substantiality matter. A publicly used derivative database requires an offer of machine-readable database or qualifying alterations under §4.6. An attributed map alone is not necessarily sufficient. [ODbL §§4.4–4.6](https://opendatacommons.org/licenses/odbl/1-0/).

Use an explicitly licensed place dataset containing sourced name/address/geometry and their corrections. Keep product catalogue and independently collected pack/time/price observations as separate datasets. ODbL does not automatically require application source code or every database in a collective database to be open. However, separate SQL tables alone do not establish independence; copying OSM coordinates into observation records or using proprietary records to enrich the place dataset needs careful treatment. The collective-database guideline is not a blanket approval of every foreign-key relationship. Before production, review the precise linkage/export boundary; publish only the required place dataset, without account identifiers or device coordinates. [OSMF collective-database guideline](https://osmfoundation.org/wiki/Licence/Community_Guidelines/Collective_Database_Guideline_Guideline), [horizontal map layers](https://osmfoundation.org/wiki/Licence/Community_Guidelines/Horizontal_Map_Layers_-_Guideline).

## IDs and historical coexistence

Retain DiaperScout's opaque Location GUID as observation identity. Provider IDs are references with aliases, not permanent domain primary keys. Google documents replacement IDs and recommends refreshing IDs older than 12 months. Geoapify provides place IDs and OSM type/ID lookups, but no perpetual identity guarantee was established. OSM objects can be replaced or retagged. FSQ IDs are useful dataset references; TomTom/HERE IDs likewise need defensive handling rather than assumed immortality. [Google IDs](https://developers.google.com/maps/documentation/places/web-service/place-id), [Geoapify details](https://apidocs.geoapify.com/docs/place-details/).

Preserve existing native Locations and observation foreign keys unchanged. Future sourced places need provenance and permitted historical snapshots; a moved business must not silently move old discoveries. Do not bulk overwrite or fuzzy-merge historical native places. Independently acquired native records must not automatically be relabelled ODbL; mixed place enrichment is a licensing decision. No schema is proposed or changed here.

## Indicative cost, excluding taxes and existing hosting

| Provider / relevant service | Published allowance and growth price | Illustration: 10,000 nearby requests/month |
|---|---|---|
| Geoapify Places, at most 20 results | 3,000 credits/day Free; 1 credit per 20 places. Paid 10k/day $59/month, 25k/day $109, 50k/day $179 | $0 if daily shared credit/rate limits respected |
| Google Nearby Search Pro | 5,000 free/month, then $32/1,000 in first paid band | $160; selected details and any map/UI calls extra |
| Foursquare default-field Pro | 500 free/month, then $15/1,000 through 100k, then $12/1,000 through 500k | $142.50; selected details extra |
| TomTom Orbis Discover | Current page lists 5,000 free/month; paid rate not verified | First 5,000 free; remaining price unresolved |
| HERE Discover/Search | Current applicable rate/allowance unresolved | No defensible current total |

[Geoapify pricing](https://www.geoapify.com/pricing/), [Places credit specification](https://apidocs.geoapify.com/docs/places/), [Google pricing](https://developers.google.com/maps/billing-and-pricing/pricing), [Foursquare June 2026 change](https://docs.foursquare.com/developer/reference/upcoming-changes), [TomTom current pricing](https://docs.tomtom.com/pricing).

Geoapify daily quotas are not a monthly pool and are shared across APIs. Its limits are soft: implement an application request cap before production. Its pricing FAQ explicitly permits ongoing commercial production on Free, with attribution and usage limits. Foursquare pricing page still contains older 10,000-free promotional wording: use the explicit June 2026 500-free schedule pending dashboard confirmation. Do not quote TomTom's older 2,500/day allowance as its current monthly entitlement or HERE's old 250k/month freemium offer. Stored open-data places avoid paid lookups on every historical Atlas view; self-hosted FSQ data has no API licence fee but incurs ingestion/storage/search operations.

## Coverage evidence and bounded credentialed test

No authenticated provider search was performed. There are no measured provider hit rates, UK winner or US coverage pass. A modest public OSM query failed HTTP 406; a second was not executed because automatic approval review was unavailable (model capacity). Search-engine visibility is not nearby-API recall. Official business pages establish test ground truth only.

| Test business / area | Official evidence and important test |
|---|---|
| Boots North Walk, Yate | [Branch](https://www.boots.com/EStoreStoreDetailNonAjaxView?catalogId=28501&storeId=11352&storeLocatorStoreId=255&storeType=boots-store&urlRequestType=Base): distinguish pharmacy from Boots Opticians |
| Tesco Extra, Yate | [Branch](https://www.tesco.com/store-locator/bristol/tesco-stores-ltd-12-east-walk): distinguish main store from in-store pharmacy and Express branches |
| Bush Healthcare, Yate — obsolete tested unit | Original [shop reference](https://bushhealthcare.co.uk/our-stores/yate/); user correction on 5 October 2026: Bush has left the tested unit and Greggs now occupies it. Exclude this location from current-specialist scoring; Geoapify’s returned Bush identity is stale |
| Pearce Healthcare & Mobility, Yate | [Official site](https://www.pearcebrosmobility.co.uk/): independent specialist at Stover Road |
| Southgate Pharmacy, Gloucester | [Contact](https://southgatepharmacy.co.uk/contact-us/): 28 Southgate Street; naming aliases matter |
| Badham St George's Pharmacy, Cheltenham | [Branch](https://www.badhampharmacy.co.uk/pharmacies/st-georges-pharmacy): explicitly relocated from Regent Street; stale pin test |
| Mobility Store, Swindon and Wroughton | [Showrooms](https://thehearingandmobilitystore.co.uk/): two distinct branches, Clive Parade and Devizes Road |
| NRU, Bolton | [Official site](https://nru.co.uk/): specialist diaper retailer; verify current visit address before radius test |
| Ability Superstore, Nelson | [Contact](https://www.abilitysuperstore.com/pages/contact): explicitly no ordinary shop; prearranged collection, useful false-positive test |
| Medilink, Birmingham / Edinburgh / Plymouth | [Centres](https://www.saltsmedilink.co.uk/medilink-network/dispensing-care-centres): continence dispensing rather than general retail; don't promise walk-in pack availability |
| Incontinence Choice, Telford | [About](https://www.incontinencechoice.co.uk/about): online supplier, not evidence of a walk-in shelf |
| CVS, 2411 Landmark Drive, Raleigh, US | [Branch](https://www.cvs.com/store-locator/raleigh-nc-pharmacies/2411-landmark-dr-raleigh-nc-27607/storeid%3D10863): US chain sanity case |
| Raleigh Medical Supply, US | [Store](https://raleighmedicalsupply.com/): 2601 Blue Ridge Road, independent medical supplier |

These cases show why broad pharmacy/supermarket-only filters are insufficient and why office/warehouse results cannot be treated as walk-in retail. Category availability proves query capability, not actual recall. Geoapify supports commercial and healthcare categories with radius/proximity controls, names, structured address and coordinates. Test narrow categories and a bounded broader commercial query before deciding a specialist is missing. [Places documentation](https://apidocs.geoapify.com/docs/places/).

## Exact next action

Simon creates one Geoapify Free account/project (no card required) and stores its restricted test key locally without posting it in chat. Run one bounded read-only spike across the above cases: nearby 1 km then 3 km, first 20 results, narrow and broad category variants, recording correct branch/name/address/position, duplicate/stale records, latency, credits and datasource licences. Verify whether the response supplies enough durable address data without another service. Use public shop coordinates for the test, not Explorer movement histories. No schema or deployment yet.

If specialist coverage fails materially, test Foursquare next: first its free hosted Pro allowance for selection coverage, and separately OS Places records for durable storage. Request written confirmation that the intended hosted fields can be retained/displayed permanently after subscription termination, or source retained fields directly from the Apache dataset. Do not infer that permission from “powered by OS Places.” Google/HERE/TomTom keys are not worth obtaining first while historical-storage gates remain unresolved. Stop here pending the Geoapify spike and concrete ODbL boundary review.
