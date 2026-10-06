# Geoapify credentialed coverage spike

> Historical provider-selection evidence. Geoapify was subsequently selected for Year One; the current decision is ADR-0012. Earlier conditional recommendations below remain as the research record.

5 October 2026. **Verdict: does not yet pass strongly enough to select as DiaperScout's sole discovery-place provider.** Chain/pharmacy performance is promising; specialist coverage and query consistency are insufficient in this sample. This is a bounded selection experiment, not a national benchmark. No application, repository, schema, account, provider data or production changes were made.

## Method and limits

16 reference cases; 64 baseline Places queries: narrow combined categories and broader `commercial,healthcare`, each at 1 km and 3 km, proximity ordering, limit 20. Address geocoding supplied public business/street/postcode centres, not device locations. This is a favourable at/near-shop test, not a physical iPhone test. The same provider geocodes centres, so positional correctness is not independently proven by agreement between its own endpoints. Official business addresses provide external branch checks.

Narrow combined categories: `commercial.supermarket,commercial.chemist,commercial.health_and_beauty.medical_supply,commercial.health_and_beauty.pharmacy,healthcare.pharmacy`.

Follow-ups: 32 individual relevant narrow-category queries at both radii; 15 name-filtered 3 km diagnostic searches across commercial/healthcare/office/production; one corrective street geocode; three category-consistency checks. One initial Places probe. **132 successful HTTP 200 requests total: 17 geocoding, 115 Places.** No automatic retries. Result-limit saturation means baseline absence is not proof of absence from the entire provider database. Diagnostic name matching returned loose matches too and was manually reviewed, not counted mechanically as success.

## Case results

N = single relevant narrow category; B = broader search. Findings below held at both 1 km and 3 km unless stated. An identified pharmacy branch is not a guarantee that it sells the target exact pack.

| Reference case | N | B | Branch/address assessment |
|---|---|---|---|
| Boots North Walk, Yate | Found | Found | Correct 15–17 North Walk, BS37 4AP; tagged chemist rather than healthcare pharmacy |
| Tesco Extra, Yate | Found | Found | Correct 12 East Walk, BS37 4AS. Tesco Express also returned at 3 km by narrow search; distinct branch, not a duplicate |
| Bush Healthcare, Yate | Found singly; omitted by combined N | Found | **Stale/outdated POI, not a current specialist hit.** User correction: Bush has left this unit and Greggs now occupies it; Greggs moved from its former smaller Yate unit. Raw shop=medical_supply reflects obsolete identity. |
| Pearce Healthcare & Mobility, Yate | Not found | Not found | **Inconclusive position:** full address and street geocoding both fell back to Yate city centre. Name-filtered 3 km query also empty; do not count as a definitive accurately centred miss |
| Southgate Pharmacy, Gloucester | Found as Allied Pharmacy | Found as Allied Pharmacy | Correct 28 Southgate Street; returned postcode GL1 1TG conflicts with official GL1 2DP and raw source/geocode |
| Badham St George's, Cheltenham | Found | Found | Correct current 84 Saint Georges Place, GL50 3QD. Combined N returns repeated same-ID row with degraded address; see below |
| Mobility Store, Swindon | Not found | Possible older identity | “Keep Able Mobility and Independant Living”, Cricklade Road/Clive Parade, SN2 1AJ; no house number. 320 m from imperfect geocode. Current-name/precise-premises identity not independently established |
| Mobility Store, Wroughton | Not found | Found | Correct 6 Devizes Road, SN4 0RZ; raw shop=car / category commercial.vehicle, so narrow medical-supply search misses it |
| NRU/Napus, current Denton showroom | Not found | Not found | Searches centred on M34 3SP postcode, not verified entrance; NRU/Napus/Nappies 3 km diagnostics empty |
| Ability Superstore, Nelson | Not found | Not found | Appointment/collection control, not an ordinary shop. Street-level centre; diagnostic “Ability” returned unrelated Adult Disability Day Service, not a hit |
| Medilink Aston | Not found | Not found | Building-level 8 Estone Drive centre; named diagnostic empty |
| Medilink Edinburgh | Not found | Not found | Building-level 97 McDonald Road centre; named diagnostic empty |
| Medilink Plymouth | Not found | Not found | Building-level 53 Valley Road centre; named diagnostic empty |
| Incontinence Choice, Telford | Not found | Not found | Online-supplier control, not verified walk-in retail. Stafford Park 18 street centre; named diagnostics returned unrelated shops |
| CVS Landmark Drive, Raleigh, US | Found | Found | Correct 2411 Landmark Drive, NC 27607, matching official branch address and source branch URL |
| Raleigh Medical Supply, US | Not found | Not found | Address geocoded to Wellness Pharmacy at the same street number; named diagnostic and single medical-supply searches empty |

Four intended UK supermarket/pharmacy cases returned identifiable branches. Of the eight originally tested UK specialist cases, only Wroughton returned a clear current identity; Bush was a confirmed stale identity, Swindon was a possible older identity, four cases were not found (NRU and three Medilinks), and Pearce remained inconclusive. Excluding the obsolete Bush reference leaves seven current specialist cases: one clear hit, one possible older identity, four misses and one inconclusive. Controls are excluded from that count. US is only two cases: chain found, independent supplier not found. Do not turn these small purposive counts into national recall estimates.

Official reference links are in the [prepared comparison](local-discovery-provider-comparison.md). Updated references: [NRU showroom](https://nru.co.uk/pages/nru-showroom), [Medilink Aston](https://www.saltsmedilink.co.uk/medilink-network/dispensing-care-centres/birmingham-aston), [Edinburgh](https://www.saltsmedilink.co.uk/medilink-network/dispensing-care-centres/edinburgh), [Plymouth](https://www.saltsmedilink.co.uk/medilink-network/dispensing-care-centres/plymouth). NRU's current official site specifies Denton, Manchester, and Napus signage; the earlier Bolton test reference was replaced, not used as current ground truth.

## Duplicates, stale entries and category consistency

Badham: two rows in each combined narrow-radius response shared the **exact same Place ID and OSM way 166126586**, with coordinates approximately 8 m apart. One formatted address omitted name/number and gave GL50 3QZ; the other correctly gave 84/GL50 3QD. This is repeated provider identity in a response, **not a finding that two real across-road premises are duplicates**. Simon notes nearby Badham premises can be distinct. Never merge shops solely by name/proximity. Other Badham branches at Swindon Road, Hewlett Road and Prestbury Road have different source identities and remain distinct. No separate obsolete Regent Street listing was observed in these tested results; this does not prove its global absence.

Swindon's “Keep Able” is a possible stale label, not a proven closed/wrong business. The Wroughton car-shop classification is an observable tagging/category problem. Source check dates are patchy and do not establish freshness.

Reproduction: medical_supply alone returned Bush; supermarket+medical_supply returned supermarkets but omitted Bush; medical_supply+healthcare.pharmacy returned pharmacies but omitted Bush. Neither result limit nor distance explains this tiny-result case. Individual narrow requests recover the stale Bush record; this remains a query-consistency finding, not evidence of current specialist coverage. Category-union correctness needs explanation/retest before integration; do not conclude medical-supply data is absent from the first combined query.

## Field completeness and source metadata

Baseline: 466 distinct Place IDs across 64 queries (deduplicating repeated IDs across searches). Name 450/466 (96.6%); formatted address, postcode, locality, country and finite coordinate fields 466/466; street 465/466; house number 242/466 (51.9%). Field presence is not address correctness: Southgate and the duplicate Badham presentation illustrate mismatches. Locality can be a nearby settlement rather than expected postal town. Positional accuracy was assessed by branch/address plausibility, not independent surveying; coordinates are POI/building representatives, not guaranteed entrances.

Every baseline unique record reports datasource `openstreetmap`, licence `Open Database License`, attribution `© OpenStreetMap contributors`, URL `https://www.openstreetmap.org/copyright`; raw source records expose OSM ID/type and tags. Store datasource provenance if eventually implemented. This confirms ODbL applies to this sample; it does not settle the future dataset/observation licensing boundary. Raw evidence is local, not a production place import.

## Latency and credits

Baseline Places (64): median 774 ms, p95 1,385 ms, maximum 2,398 ms. Baseline geocoding (16): median 907 ms, maximum 2,524 ms. All measured Places queries excluding initial probe (114), including follow-ups: median 915.5 ms, p95 9,156 ms, maximum 20,233 ms. Follow-ups reveal significant tail latency despite HTTP success. These include local network/TLS/server time and fresh urllib connections; no warmed connection pooling, load test or provider-only timing attribution. The initial successful probe took ~330 ms. A failed sandbox network attempt reached no verified API response and is not counted as a successful request.

**Expected credits: 132**, from the published one-credit geocoding/up-to-20-place request model; no selected-details/map calls. This is **not dashboard-confirmed consumption**: no usage-credit headers were returned, and account-dashboard access was unavailable. Check the project dashboard delta for the actual billed credits. The run fits the nominal 3,000/day Free allowance if no other project usage exhausts it. [Pricing](https://www.geoapify.com/pricing/), [Places specification](https://apidocs.geoapify.com/docs/places/).

## Missing-place corrections and saved discoveries

OSM supports editing missing places or leaving notes; Geoapify uses OSM, so upstream corrections can improve its data. No dedicated Geoapify place-write API or guaranteed propagation interval was verified. No corrections/submissions were made. [OSM correction guidance](https://www.openstreetmap.org/fixthemap), [Geoapify source](https://www.geoapify.com/tutorial/how-to-get-osm-places-by-category/).

Simon proposed privately holding a discovery and checking later for the place. This is a possible follow-up design, not implemented or a change to the gate in this spike. It would need a privacy-conscious pending-place identity, bounded occasional checks, and Explorer confirmation of the eventual recognised place; never automatically associate a similarly named business. It must explicitly reconcile with the no-manual-business-submission requirement, and must not keep raw precise device history merely for polling. It changes the unavailable-shop experience and does not make the missing place available immediately.

## Recommendation and next action

Keep Geoapify as a promising open-storage candidate, **not the selected sole provider yet**. Its strongest advantages are durable licensed data and low baseline cost. The negative specialist evidence is material for DiaperScout's domain and is stronger after excluding the stale Bush hit. Do not deploy an integration or add schema now.

Next selection test: obtain Foursquare test credentials and compare the **same specialist cases**, with its hosted-API retention rights separately clarified or durable fields sourced from Apache-licensed OS Places. Alternatively, if Simon explicitly accepts coverage gaps plus a saved-discovery/OSM-maintenance model, reassess Geoapify under that changed product requirement and resolve combined-category behaviour. No account or message was created/sent for either option.

## Safety and evidence files

Credential loaded from user-only temporary file, never embedded in scripts, source control, reports or printed request URLs. Artifact scan found no key value; temporary key file removed after requests finished. All test files are in this task workspace, outside DiaperScout's repository. Sanitised requests, cases and metrics support this report. No deploy, commit, provider-data mutation or application tests were appropriate for this read-only spike.

## Evidence correction — 5 October 2026

Simon supplied local ground truth: Bush Healthcare no longer occupies the tested Yate unit; Greggs moved into it from its previous smaller unit. This correction is accepted without additional provider/API calls. Raw responses, request counts, latency and field-presence metrics remain unchanged; current-branch interpretation and coverage conclusions above are corrected. No claim is made about Bush's current location elsewhere.

## NRU alias follow-up — 5 October 2026

Four additional Geoapify Places requests searched `Nappies R Us` and `Incontinence Choice`, each at 1 km and 3 km around the same Denton postcode centre (53.45236, -2.12839), using commercial/healthcare/office/production categories, limit 20 and proximity bias. All four returned HTTP 200 with zero results. This supplements earlier NRU/Napus/Nappies queries; the tested aliases did not recover the branch. It does not prove absence from the entire database, and the centre remains postcode-based. Estimated additional usage: four credits, not dashboard verified; cumulative Geoapify successful requests now 136 (17 geocoding, 119 Places). Original-run metrics above remain historical. Evidence: `nru-aliases.json`. No Foursquare requests were made. Temporary key deleted after calls.
