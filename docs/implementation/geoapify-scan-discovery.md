# Geoapify Scan / Record a Discovery implementation

Work in progress, 5 October 2026. No production release is claimed by this document until explicit deployment evidence is appended.

Starting checkout: `/Users/sporter/Documents/Projects/DS`, branch `fix/product-submission-recovery`, HEAD `1bf717d099de160c98d784e0b78af1c9eff07583`. HTTPS read verified the branch matches GitHub; `main` is an older ancestor. Original untracked Google assessment/ADR preserved and ADR updated under this task. No unfinished importer work was present or included.

## Storyboard comparison and changes

The approved exact-pack result already has left-side image/right-side details, supporting Product Details/Where to Buy and primary Record a discovery; leave its recognition/camera code intact. Existing discovery forms lost the short nearby picker, exact-pack review imagery and bespoke completion scene, and exposed manual shop entry. Replace the picker with nearby provider results and a quiet missing-shop explanation/radius retry. Use exact-pack imagery, optional price, review and immediate success. No availability-status field or observation moderation added.

Unknown product's working evidence/photos/double-check/review/moderation architecture remains. Its optional discovery uses the same place picker. Bespoke completion artwork distinguishes public known-pack discovery from privately submitted packaging evidence. Fox is a stationary plush, not a live animal. Guide/van designs follow supplied character sheets. Functional forms remain uncluttered.

## Data and privacy

One additive nullable `ProviderSnapshotJson` JSONB column on Locations; existing rows/keys unchanged. Server-side provider mapping accepts OSM/ODbL provenance, stores immutable selected place snapshots, and does not retain raw device coordinates. Snapshot identity uses content hash; exact retries reuse it, changed historical meaning uses a new Location. Existing native places coexist and administrator maintenance remains available behind persisted editorial authorization.

Nearby provider requests use POST to a constant URL and `x-api-key`, avoiding secret/location query strings. Provider and contribution HTTP loggers are removed; request bodies are not logged. Selection payloads contain place metadata and expiry, authenticated with a key derived for this purpose from server-side provider configuration. Key rotation invalidates unsaved tokens; users reselect. Browser drafts contain selected place data only, never device positions. Tokens expire after 30 minutes.

Five single-category queries with limit 20 (five provider requests per search, nominally five credits at the researched Places rate; dashboard billing is not independently measured), overall 12-second deadline; 1 km initial radius and explicit 3 km expansion. Merge by datasource/source identity, preferring complete address, preserve distinct neighbouring places. Incomplete/unsupported-source records are excluded. Result caps, stale data and missing specialists are known limitations. Geoapify unavailable means friendly selection failure, not manual creation or historical-view failure.

Public licensed-place export at `/places/open-data` contains only sourced snapshots attached to qualifying public observations. Accounts, product data, observation details and pending private evidence are excluded. OSM/Geoapify attribution and ODbL source links appear alongside Atlas/picker. See ADR-0012 for boundary and rollback policy.

## Validation status

Initial API/Web compilation passed. Targeted original observation/native/proposal tests and five provider mapping/selection tests passed before the expanded provider-selected fixture run. Further automated/browser/live validation is in progress; no final success claim yet.

A direct integration-protocol check of the implemented POST/header shape succeeded for all five categories around the public Yate reference: HTTP 200, respectively 3, 2, 1, 5 and 20 results, all reported OSM source. This validates the protocol, not production UI or current retail identity.

Rollback: restore prior API/Web image revisions; retain added schema/data and existing secrets. Do not drop snapshots or alter networking, TLS, identity, affinity or Data Protection settings.
