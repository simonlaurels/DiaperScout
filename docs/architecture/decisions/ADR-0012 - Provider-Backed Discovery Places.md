# ADR-0012 — Provider-backed places for Explorer discoveries

## Status

Accepted — Geoapify selected for Year One on 5 October 2026. Implementation validation and release evidence are recorded separately.

## Decision

Use server-side Geoapify Places searches with the existing Leaflet/OpenStreetMap Atlas. Explorers select provider results; they cannot create or propose businesses. Preserve opaque DiaperScout Location IDs. A qualifying exact-pack observation publishes immediately; unknown-product place evidence remains private until transactional, idempotent catalogue resolution.

Accept imperfect specialist coverage and stale OSM-derived POIs in exchange for low cost, permitted licensed storage, historical meaning and low operating burden. Do not continue the provider bake-off. Hosted Google/Foursquare APIs are rejected for this phase principally because their historical-storage/licensing model is a worse fit. Foursquare improved specialist coverage in the tested subset; this decision does not claim otherwise.

## Evidence and correction

The credentialled [Geoapify report](../../implementation/geoapify-report.md) and [Foursquare comparison](../../implementation/foursquare-report.md) inform this decision. Bush Healthcare has left the tested Yate unit; Greggs moved into it from its previous smaller unit. Geoapify's Bush record is **stale**, not a successful current specialist hit. Foursquare's omission carries no coverage penalty. No extra provider calls were made merely to correct this evidence.

NRU aliases did not recover the Denton showroom in tested Geoapify searches; Foursquare aliases beyond NRU remain untested. Missing places are an expected limitation, not a reason for manual business submission. Operational upstream OSM improvement is preferable to creating an Explorer-submitted proprietary directory.

## Boundary and historical semantics

`PhysicalObservation → Location → provider snapshot/reference`. Provider IDs are provenance, never observation foreign keys. A selected OSM-backed snapshot contains sourced business name, address, coordinates, source identity, provider ID and licence/attribution. Snapshot content is immutable. Its content-derived identity reuses identical selections; moved/renamed/retagged records produce separate snapshots instead of rewriting history. Creation time belongs to Location; observations retain their own times.

Existing native Locations, names, coordinates and observation foreign keys coexist unchanged. No fuzzy matching, automatic identity attachment, bulk migration or historical coordinate overwrite.

The licensed place dataset consists of provider snapshots and their sourced presentation attributes. Products, packs, identifiers, observation claims/times/prices, accounts and moderation remain independently collected datasets. Observations reference opaque Location IDs; raw device geometry is never copied into observation/proposal records. This is an explainable dataset boundary, not an assertion that different SQL tables alone resolve licensing.

Public sourced snapshots are offered as machine-readable data under ODbL at `/places/open-data`, with source/licence attribution. The export excludes accounts, observations, prices, product relationships and privately pending proposal evidence. Native place data is not automatically reclassified as OSM-derived. Any future enrichment/mixing of the sourced dataset must retain this boundary and appropriate licence obligations. No application-source release is required by this decision.

## Operational consequences

- Five bounded category requests per search: chemist, healthcare pharmacy, medical supply, supermarket and broader commercial. Merge by OSM source identity, never solely by name/proximity. This avoids relying on the observed combined-category anomaly.
- Start at 1 km; explicit 3 km expansion. Incomplete/unsupported-source records are excluded. Result limits mean the list is not exhaustive.
- Device coordinates are transient POST-body search inputs. Provider key uses a server-side header, secure configuration and secret storage. Neither enters URLs, drafts, analytics or ordinary HTTP logs.
- Search results carry expiring server-signed selection evidence. Only selected places are persisted. This prevents free-text/arbitrary-client data becoming provider-backed canonical places.
- Attribution identifies OSM/Geoapify as place sources. Place existence or a positive observation never guarantees live stock.
- Provider outages prevent selecting new places with friendly UX; existing Atlas/catalogue observations remain database-backed and require no provider hydration.
- Preserve authenticated accounts, internal API ingress, VerifyFull TLS, shared Data Protection, affinity, scaling and production networking.
- Roll back app images if needed; retain the additive snapshot column and recorded data. Do not apply the migration's destructive Down operation to recover an app release.

## Related material

The [provider comparison](../../implementation/local-discovery-provider-comparison.md) records the historical Google assessment; Google is not the selected architecture. [Implementation and validation](../../implementation/geoapify-scan-discovery.md).
