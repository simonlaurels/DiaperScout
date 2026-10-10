# Installed Scan and discovery implementation

4 October 2026. GitHub baseline: 616046e, fetched before implementation. This replaces the earlier boundary proposal with the approved architecture. No production changes.

## 1. Existing architecture

Scan uses InteractiveServer, the existing capture module and public GTIN lookup. A confident match returns canonical Product, ProductVariant, SizeVariant and exact PackType. Product and retailer destinations accept exact variant/pack context. Explorer proposals already use CatalogueSubmission, contributor recovery identifiers, private provenance, verification and moderator approval. IPlaceObservations owns confirmed commercial shops, exact-pack observations and qualifying Atlas projection. CatalogueSubmissionImage and its storage service own packaging images.

## 2. What changed

Added installed recognition presentation, stepped discovery, Explorer packaging evidence, catalogue candidate suggestions, pending shop evidence and transactional reconciliation into existing observations. Existing moderator image APIs, canonical models and Atlas qualification remain. Additive migration 20261004085541_ExplorerProposalEvidence adds nullable proposal identity/location/time/price/result links and private evidence metadata. Existing images default to non-Explorer evidence. No availability column.

## 3. Recognised product

Installed results show calm recognition, image-left/details-right information, primary Record a discovery, product details and Where to buy with exact variant/pack context. Only approved public images are used with contain sizing; missing imagery has an honest neutral fallback. A quiet correction panel offers rescan/manual search. Camera capture/cleanup remains unchanged. Ordinary browser presentation is preserved.

## 4. Record a discovery

Choose a confirmed shop, optional shelf price and local observed time, review/edit, then record through the existing exact-pack observation service. Product/variant/size/pack are fixed by the barcode. Completion links to the selected place on Atlas. Discovery means physically finding that exact pack at that place/time, not guaranteed present/future stock. No out-of-stock or shelf-label-only option.

## 5. Nearby-place capability

An explicit Find nearby shops action gets transient device coordinates and sends a no-store POST to the place read service. Confirmed known commercial shops within approximately 20 km are distance-ordered, returning up to 50. Shops can be selected before earning Atlas pins. Denied/unavailable location offers name/town/postcode search and confirmed manual shop entry. Selected public shop coordinates may be retained; raw device search coordinates never enter proposal, observation or browser recovery.

## 6. External Places

No provider or speculative adapter is added. Nearby reads are separate from contribution writes so a separately approved provider can later supply candidates. Coverage is existing DiaperScout shops; no external-provider decision is required for v1.

## 7. Unknown contribution

The installed flow reserves an owner-scoped Explorer CatalogueSubmission draft, requires a front photograph and supports up to four JPEG/PNG/WebP images, each at most 15 MB. It collects printed brand/name and optional type/notes. Existing storage retains community provenance and unknown permission status. Signature/size, ownership, draft state and upload retry identity are validated. Front evidence and valid identity are required before entering verification. Legacy ordinary-browser proposals preserve their existing route and validation.

## 8. Catalogue duplicate checking

After basic identity, existing public catalogue search finds current products matching entered product text, filtered by entered brand/manufacturer. Up to 12 plausible candidates appear with approved imagery where available. The Explorer suggests a match or explicitly proposes a new product. Review and moderation retain the choice.

## 9. Unknown barcode with existing product

SuggestedExistingProductId is strictly a moderation hint. Selection creates no ProductIdentifier or canonical product relationship. Moderators verify the evidence and choose the exact destination pack using existing conflict checks and audited resolution. Notes/type remain proposed facts. Canonical variant/size/pack modelling remains moderator work.

## 10. Pending shop evidence

A submitted proposal retains one owner-scoped confirmed Location, observed UTC time and optional price/currency. No normal Observation or Atlas exposure exists before resolution. Invalid shops/time/price/currency and mismatched retries fail.

New publication resolves the exact pack from the verified GTIN within the published product, requiring a unique match instead of an arbitrary first pack. Merge uses the moderator-selected canonical pack. Both reconcile in the same database transaction as moderation, serialized by a proposal-scoped PostgreSQL advisory lock. Original contributor/contribution provenance identifies the single observation; ResultingObservationId links it. Retries reuse the result. Conflicting pack/shop/time/price payloads fail. Rejection creates no observation. Explorer photographs are excluded from automatic publication to Product images; independent image authorisation remains existing editorial work.

## 11. Draft/recovery

Existing owner-scoped 30-minute contribution recovery is reused. Product recovery retains barcode, entered identity/notes/type, upload retry token, server proposal identity, selected suggestion and step. Photographs remain server evidence and reload through owner-only no-store HTTP image endpoints, never local-storage image blobs. Submitted recovery copies are excluded from unfinished Backpack drafts.

Discovery recovery retains selected public shop, time/price/currency, contribution identity and review step. Typed manual shop data and deliberately selected public shop coordinates are distinct from transient device-search coordinates. Reload restores context; completion clears the discovery draft. Server evidence follows existing submission retention; no separate cleanup scheduler. Connection errors preserve data and offer retry/reload.

## 12. Authentication

Existing Explorer authentication and signed API identity forwarding remain. Sign-in returns preserve barcode/exact pack/proposal. Explorer evidence operations are owner-scoped. Moderator access uses existing editorial APIs. Private image proxy requests forward the signed-in user and return no-store responses. Candidate selection and place search confer no editorial authority. Resolution retains PublishAtlas authorisation.

## 13. Tests/build

Final validation: 64 domain tests passed; all 111 affected integration/browser tests passed; three final migration/rejected-recovery checks passed. Release build: zero warnings/errors. The initial complete 300-test integration/browser run had 294 passed and six failures; four affected failures were corrected and rechecked, while two hydration timeouts reproduce on untouched baseline 616046e. See [exact results and commands](scan-discovery-evidence/validation.md). Tests cover additive migration application on isolated PostgreSQL; domain transition boundaries; private owner photos; missing-front prevention; invalid uploads/suggestions; pending evidence; no Atlas exposure; new publication; merge; rejection; conflicting payloads and retry idempotency. Browser coverage includes both installed flows, denied location, optional price, exact destination links, reload, private pending evidence and ordinary-browser regressions.

## 14. Visual validation

Screenshots at widths 390 and 430 in Chromium/WebKit cover recognition, place selection, details/review/completion, candidate selection and proposal review/completion. Screens use isolated fixture identities and synthetic uploaded test evidence, never production data. The missing-image fallback was inspected in earlier runs. Final WebKit coverage additionally seeds the existing prototype-tena1.png repository test image as approved fixture imagery and verifies contain sizing. Fixture product names and counts remain synthetic; this does not assert that the pack image describes those test facts. Assertions check overflow, installed header suppression, navigation/context and actionable destinations. Real iOS hardware/camera permission behaviour is not claimed from headless tests.

## 15. Storyboard differences

Availability choices are deliberately omitted per approval. Nearby results use known shops. The exact pencil-recording Guide artwork is absent from repository assets; completion uses an isolated map-icon slot instead of generating another Guide. Pixel-identical parity is not claimed. Real catalogue facts replace mockup sample product/manufacturer data.

## 16. Decisions/next actions

All required material domain/privacy decisions were resolved by the approval document. The user subsequently authorised commit and deployment. Implementation commit `3fd804d0d762a6e7a1186fe418dc343841036d76` was pushed to `fix/product-submission-recovery` and deployed on 4 October 2026. See [production deployment evidence](scan-discovery-evidence/deployment.md).
