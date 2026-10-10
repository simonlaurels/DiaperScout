# Commerce plugin architecture

## Inspection and proposed design

The current public catalogue injects `IAffiliateLinkResolver`, but registration selects the concrete Awin resolver. That resolver checks preferred/configured programmes, network name, deep-link permission, and numeric advertiser/publisher IDs. Existing Awin programme discovery has its own HTTP client/options and a single-provider orchestration service. Its scheduler classes exist but are not registered as production jobs. Canonical listing upsert, verification, and exact-pack public eligibility already belong to DiaperScout services.

There are no persisted price/stock observations. The existing provider-neutral availability enum can be reused. PlatformSetting supports small non-secret operational controls without changing schema. Existing catalogue changes in this checkout are preserved.

Use four independent DI contracts in Application: retail discovery, pricing, availability, and affiliate resolution. An optional affiliate-programme discovery contract preserves the existing relationship-discovery capability without forcing it on every affiliate plugin. Plugins consume immutable canonical snapshots and emit normalised observations; they receive no DbContext or canonical writing service.

Application orchestration discovers installed implementations, orders them by configured priority then stable ID, checks enable/configuration state and support, validates normalised outputs, stamps trustworthy plugin provenance, isolates failures, and bounds calls with cancellation/timeouts. Outbound affiliate resolution falls back to the ordinary canonical URL. Infrastructure loads canonical contexts and records validated discovery results through the existing canonical upsert service, always subject to existing review/verification rules.

Awin implementation/configuration/HTTP details move to a separate plugin project referencing Application, not Infrastructure. Existing secure configuration keys and affiliate business rules remain compatible. Composition-root registration may name Awin; generic orchestration, catalogue queries, and management must not.

Persist only enabled booleans in existing PlatformSetting records. Configured priorities/timeouts and provider credentials remain in external application configuration. Operational last-success/failure data is process-local, explicitly labelled, and uses sanitised codes rather than provider exception messages. Typed observation/provenance results are returned to application callers; no generic JSON observation store or speculative price/stock schema is added.

Sanity check: capability contracts are independent; a provider can register several independently controlled plugins; canonical identity and writes stay in DiaperScout; no provider conditionals are needed to add implementations; DI avoids dynamic assembly loading; no new service or scheduler is needed; no migration or networking change is needed.

The following sections describe the completed implementation and its extension boundaries.

## Implemented architecture

### Contracts and ownership

`Application/CommercePlugins.cs` defines four independent capability interfaces:

| Contract | Input | Normalised output |
|---|---|---|
| `IRetailDiscoveryPlugin` | `CanonicalSellableItem`, including Product/Variant/Size/Pack IDs and pack identifiers | Retailer/listing evidence, identifiers, title, quantity, confidence, observed time |
| `IPricingPlugin` | Canonical listing and sellable-item snapshot | Amount, uppercase currency, observation/source/external ID, optional expiry |
| `IAvailabilityPlugin` | Same canonical listing snapshot | Existing provider-neutral availability enum, observation/source/external ID, optional expiry |
| `IAffiliatePlugin` | Listing, canonical retailer and relationship snapshots | Attributable URL, network, canonical programme ID, resolution time and evidence |

A provider can supply separate implementations and IDs for each capability. The registry rejects duplicate IDs, mismatched declared families and implementations combining the four primary families. `IAffiliateProgrammeDiscoveryPlugin` is an optional extension of the affiliate family for discovering retailer/network relationships. Ordinary affiliate plugins need not implement it.

Plugins expose stable ID, display name, family, version and safe configuration state. Snapshots contain no EF entities or writing services. The Awin project references Application only; Application references Domain. Plugin implementations are trusted compiled code, not dynamically sandboxed third-party DLLs. Architectural isolation is established by project dependencies and contracts.

DiaperScout retains Business → Brand → Product → Variant → Size → Pack → RetailerProductListing identity. No provider-specific product, retailer or listing models/tables were introduced.

### Orchestration and operations

`CommercePluginRegistry` discovers the DI implementations and validates metadata on construction. `CommercePluginOrchestrator` invokes only the relevant interface, ordered by ascending configured priority then ordinal stable ID. Retail discovery, pricing and availability return all valid observations in that deterministic order. Affiliate resolution stops at the first eligible valid result. An empty result allows the next plugin to try.

Each call checks runtime enablement, capability configuration and input support. Provider invocation runs within a bounded task with linked caller/deadline cancellation. Timeouts are constrained to 10 ms–120 seconds. A plugin that ignores cancellation cannot indefinitely block the request; it must still cooperate to release its own resources after the request stops waiting. Caller cancellation propagates rather than becoming a provider failure.

Failures, timeouts, missing configuration, unsupported inputs, disabled plugins and invalid results are distinct execution codes. One failing provider does not remove other providers' results. Optional programme discovery persists successful candidates even when another plugin fails; if every result is absent and a provider failed, the existing service reports a generic failure. Raw provider exception messages and HTTP error response bodies are never emitted by orchestration or management.

Validation covers HTTP(S) URLs without embedded credentials, lengths/counts, enum values, currency/amount, observation/expiry timestamps, positive quantity, confidence range and programme eligibility. Affiliate URLs require a matching preferred, configured canonical programme whose deep-link permission is not explicitly false. Unverified retailers/listings do not receive affiliate destinations. Without a usable provider, the canonical ordinary retailer URL is returned.

`ICanonicalCommerce` is the application-facing entry point for `DiscoverListingsAsync(packId)`, `RefreshPriceAsync(listingId)`, `RefreshAvailabilityAsync(listingId)` and `ResolveAffiliateAsync(listingId)`. It loads canonical data before calling plugins. Future workers can resolve it in a DI scope and pass cancellation; no new scheduler or background service is enabled.

For discovery, the application rejects conflicting identifiers or pack quantity and requires association with exactly one existing retailer by canonical website host, optionally narrowed by a supplied canonical retailer ID. Unknown/ambiguous retailers become review codes rather than provider-owned canonical entities. Results deduplicate by retailer and normalised listing URL for the requested pack. Existing verified listing provenance is preserved. Other candidates pass through the established `IRetailerDiscovery.UpsertAsync`; new listings remain Discovered and require existing verification before appearing publicly.

### Registration and extension recipe

Create a separate plugin project referencing `DiaperScout.Application`, implement exactly one primary interface, supply a unique stable ID, and register its configuration/HTTP dependencies in the composition root:

```csharp
// Example only: no speculative provider is installed by this task.
services.Configure<FeedOptions>(configuration.GetSection("RetailFeed"));
services.AddHttpClient<FeedClient>();
services.AddCommercePlugin<FeedRetailDiscoveryPlugin>();
```

The registry discovers that implementation through `ICommercePlugin`. No change to orchestration, catalogue queries or management UI is required. Registration of the same implementation is idempotent. DI implementations are scoped; health tracking is shared within the API process. Plugins should accept immutable inputs, use the provided cancellation token, return empty/null for a supported lookup with no data, and keep credential values out of exceptions, URLs and normalised results.

### Configuration and secrets

`CommercePlugins:Plugins:<stable-id>:Enabled`, `Priority` and `TimeoutSeconds` configure operational policy through existing .NET configuration sources. For example, an environment variable `CommercePlugins__Plugins__awin.affiliate__TimeoutSeconds` configures Awin's deadline. Provider configuration remains external to the application database. Production's existing secret/configuration mechanism is preserved; no real credentials were created or changed.

The existing `PlatformSetting` table stores only `commerce.plugin.<stable-id>.enabled` as `True`/`False`. A persisted operator setting applies on the next request/scope. Deployment policy disabling a plugin cannot be overridden by an operator. Unknown IDs are rejected. Credentials, full options and arbitrary JSON settings are never persisted by this feature or returned by management.

### Provenance and persistence boundaries

Every accepted result is wrapped in `SourcedCommerceObservation<T>` with the actual registered plugin ID/version, external listing ID, provider observation time, application refresh time and source URL. Providers cannot claim another plugin's identity through their normalised result. Price/availability use the canonical listing external ID when their own result omits it.

Discovery persists plugin ID, external ID and evidence URL through existing listing fields; canonical listing discovery/check timestamps retain their existing semantics. The returned observation also includes the original provider-observed timestamp and version. There is no new durable observation/history store. Price/stock observations and detailed batch provenance are typed return values for callers, not public prices or database history. Affiliate provenance accompanies the internal resolved destination, while the existing public offer DTO retains its established fields.

### Awin extraction and compatibility

`DiaperScout.Commerce.Plugins.Awin` contains `AwinAffiliatePlugin`, existing `AwinAffiliateProgrammeDiscoveryProvider` and its options. The old Infrastructure-only `AffiliateLinkResolver` was removed. Infrastructure's provider-aware composition root installs Awin, while generic orchestration and public catalogue code contain no Awin branch.

Existing `AwinAffiliateDiscovery` configuration keys are preserved. Deep-link resolution still requires only numeric publisher ID and a preferred Configured Awin programme with numeric advertiser ID and deep links not explicitly forbidden. Leading-zero numeric IDs retain the previous parsed-number URL behavior. Network comparison is case-insensitive and the canonical retailer URL is escaped into `ued`:

```text
https://www.awin1.com/cread.php?awinmid=<advertiser>&awinaffid=<publisher>&ued=<escaped-listing-url>
```

Resolution remains available without a programme-discovery API token and independently of the legacy discovery Enabled flag. API relationship discovery additionally requires discovery enablement, token, valid HTTPS base URL and country codes. Existing programme matching, membership status mapping and moderator-managed relationship lifecycle are preserved. Error bodies are no longer embedded in provider exceptions because they may contain sensitive provider details.

### Where to Buy and management UX

The exact verified listing query remains scoped to selected Product → Variant → Size → Pack. It builds a provider-neutral listing snapshot and asks orchestration for the outbound destination. DiscoveryProvider is not used to select an affiliate provider: a Manual listing can use any eligible affiliate plugin. The public offer DTO and ordinary URL fallback remain compatible.

`/admin/commerce-plugins`, linked from Admin, uses the existing visual language and shows all four capability sections. Installed plugin cards show name/version, enabled state, safe configuration state, health and last success/failure timestamps with sanitised failure code. Enable/disable uses protected `/api/v1/commerce-plugins/` GET and `/{id}/enabled` PUT endpoints. Both API policy and service-level authorization require Moderator or Administrator. Explorer/anonymous users cannot manage plugins. The UI exposes no credential fields or provider error payloads and explicitly labels execution history as application-instance-local.

## Files introduced or changed for this task

- Application: `CommercePlugins.cs`, `CommercePluginRegistry.cs`, `CommercePluginOrchestrator.cs`.
- Domain: append `Limited` to the existing `RetailerProductAvailability` enum, preserving existing numeric values.
- New plugin project: `DiaperScout.Commerce.Plugins.Awin.csproj`, `AwinAffiliatePlugin.cs`, moved `AwinAffiliateProgrammeDiscoveryProvider.cs`; solution/project references updated.
- Infrastructure: `CommercePluginRuntime.cs`, `CanonicalCommerce.cs`, `AwinPluginRegistration.cs`; composition/public catalogue integration in `ServiceCollectionExtensions.cs`; optional programme orchestration in `RetailerAffiliateProgrammeDiscoveryScheduler.cs`; remove `AffiliateLinkResolver.cs` and old-location Awin provider.
- API: `CommercePluginEndpoints.cs` and endpoint mapping in `Program.cs`.
- Web: `CommercePluginClient.cs`, `CommercePlugins.razor` and scoped CSS, Admin link and client registration.
- Tests: `CommercePluginTests.cs`, `CommercePluginApiTests.cs`, `CommercePluginBrowserTests.cs`; update Awin provider import and API/web test composition to use the real plugin framework.
- Report: this document. Earlier public-variant catalogue changes remain in the checkout and are documented separately in `public-variant-catalogue.md`.

## Schema and production safety

No EF mapping, migration, canonical identity or production record conversion was added. The availability enum is not mapped to a new persisted field. Existing settings support the boolean operational control. Deployment requires only API/web image revision updates. PostgreSQL VerifyFull, database connectivity, Azure/Container Apps networking, Private Link, NAT, TLS and Cloudflare settings remain unchanged.

## Validation and delivery

- Full suite: `dotnet test DiaperScout.slnx --no-restore -m:1 -nodeReuse:false`: **206 passed, 0 failed, 0 skipped** (168 integration/API/browser; 38 domain/application).
- New coverage adds **28 tests**: 20 domain/application cases and 8 integration/browser cases. Existing catalogue, manual listing, Awin programme and exact-pack variant tests pass through the new real plugin registration.
- Tests cover all four fake capability implementations, DI discovery/idempotent registration, stable IDs, duplicate/wrong-family/monolithic rejection, multi-provider ordering and results, disabled/missing/unsupported paths, invocation failure isolation, noncooperative timeout, active/pre-cancelled cancellation, malformed/expired/future observations, application-stamped provenance, programme-discovery isolation, preferred programme eligibility, Awin numeric/leading-zero/deep-link/encoding compatibility and ordinary URLs.
- API tests cover moderator/administrator permissions, anonymous/Explorer rejection, boolean-only persistence, unknown plugin and deployment-policy rejection, safe secret-free metadata, canonical discovery association/deduplication/review without automatic verification, and a fake affiliate provider monetising a manual listing then falling back safely when disabled/failing.
- Browser tests verify four-family operational UI, configuration display, API-backed enable/disable, no secret rendered in HTML, and no horizontal overflow at 1280/390 px. Screenshots were visually inspected at both widths; the focused visual-capture rerun also passed.
- Release build: `dotnet build DiaperScout.slnx -c Release --no-restore -m:1 -nodeReuse:false`: **succeeded, 0 errors**. One existing CS8600 warning in `ServiceCollectionExtensions.cs:472` remains outside this task's changes.
- Deployment completed successfully; both apps report `latestRevisionName == latestReadyRevisionName` for the new revisions. Production smoke finished with **31/31 checks passed**, using connection retries as described below.

## Known limitations and next extension

- Only Awin is a real installed provider. The other families are working contracts/orchestration/canonical entry points proven with test implementations, without fabricated provider data.
- Health history is process-local and resets on restart; multiple API replicas have independent histories. Durable distributed execution history would require a deliberate storage design.
- Operator enablement persists, but a running scoped operation uses the policy loaded for that scope. Disable affects subsequent scopes, not an already-running provider call.
- Price/availability and full provenance histories are not persisted or displayed publicly. Add durable typed observation storage when implementing a real provider with concrete retention/freshness requirements; do not use PlatformSetting as a JSON data store.
- Discovery requires an existing canonical retailer association and leaves candidates subject to existing review/verification. It does not create retailers or automatically promote confidence to verification.
- Schedulers remain inactive. The existing optional programme service and new canonical entry points can be used by a future explicitly configured worker.
- First recommended extension: an authorised retailer/feed discovery plugin supplying exact GTIN and pack-quantity evidence. It exercises canonical association and review with fewer moving parts than adding marketplace discovery, stock, pricing and attribution simultaneously. Pricing, availability and affiliate implementations for the same provider should remain separately registered plugins.

### Deployment

Used the existing `Dockerfile.api` / `Dockerfile.web` remote ACR build and image-only Container App update process. Source content fingerprint: `fe5e45522a`.

| Component | ACR run | Image tag | Production revision |
|---|---|---|---|
| API | `dbu` — Succeeded | `diaperscout-api:commerce-plugins-20261001-fe5e45522a` | `diaperscout-api-vnet--plugins-fe5e45522a` |
| Web | `dbv` — Succeeded | `diaperscout-web:commerce-plugins-20261001-fe5e45522a` | `diaperscout-web-vnet--plugins-fe5e45522a` |

Registry: `diaperscoutprod-dsg9bgg6dkgkcwbs.azurecr.io`. Resource group: `rg-diaperscout-prod`. Both revisions are Active, Healthy, Provisioned and Running. API was verified healthy before updating Web. Updates changed only image and revision suffix; environment/secrets, database settings and all networking/TLS configuration were preserved. No production catalogue data, programme configuration or plugin-control setting was edited for smoke testing.

### Production browser connection finding

Post-deployment browser tracing found intermittent SignalR handshake failures: `/_blazor/negotiate` completes but WebSocket/LongPolling returns HTTP 404, “connection ID not found.” Read-only Azure inspection shows a single active healthy Web revision with **three replicas**, `latestRevision: true` at 100% traffic, and **no configured sticky sessions**. The previous image revision also permitted up to ten replicas. The unchanged absence of affinity with multiple server-side Blazor instances is consistent with negotiation and transport reaching different replicas; this diagnosis is an inference from the browser trace and configuration.

No networking, scaling or sticky-session setting was changed to address this finding, in accordance with the task's guardrails. The smoke harness waits for loaded rows/selected state and reloads after a failed circuit connection. A successful retry demonstrates application behavior but does not remove this operational issue. Operators should investigate session affinity/distributed Blazor hosting separately under explicit infrastructure authorization. API/plugin tests and public SSR responses do not depend on that routing issue.


### Final production smoke result

**31/31 checks passed** against `https://diaperscout.app` after both new revisions were ready:

- Seven catalogue cards: Crinklz Original plus the six independently linked NorthShore MEGAMAX variants.
- Correct variant titles and variant-specific size counts, including White's seven sizes and four sizes for the other MEGAMAX variants.
- Non-Black Medium selections do not inherit the Black NRU offer.
- Black → Medium → 10 pieces · Bag selects canonical pack `a17f3fa3-a690-4224-9fad-8c84902ebe90` and exposes exactly `https://nru.co.uk/products/northshore-megamax-black?variant=50456472912205`.
- Extra Large changes the canonical pack and removes that Medium offer; changing variant also removes it.
- Variant URL selection, refresh, back/forward and malformed-variant handling remain correct.
- Anonymous users are redirected to sign in for the new plugin-management page; the existing retail-listing management page denies editorial access.

The initial post-promotion catalogue request yielded no rows; independent and subsequent loads rendered all seven cards. Earlier fixed-delay/click smoke runs were affected by the SignalR finding above. The final harness waits for rendered state and reloads failed connections, and all expected canonical IDs/destinations then matched the baseline. This does **not** claim the intermittent connection problem is resolved.

Production smoke exercised the ordinary outbound destination through the plugin-integrated Where to Buy path. No real credentials or programme relationships were created to force a positive live Awin lookup. Positive Awin resolution, programme preference and existing HTTP discovery behavior are covered by automated tests. Live authenticated Awin network access remains dependent on securely supplied production configuration and eligible relationships.

Changes remain in the working checkout; source fingerprint was rechecked after deployment and still matches the deployed images. `git diff --check` passes. No schema migration, production record conversion, network/TLS modification or production plugin toggle was performed.
